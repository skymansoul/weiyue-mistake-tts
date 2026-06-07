using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using WeiyueMistakeTTS.Services;
using WeiyueMistakeTTS.Windows;

namespace WeiyueMistakeTTS;

public sealed class Plugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICommandManager commandManager;
    private readonly IFramework framework;
    private readonly IClientState clientState;
    private readonly ICondition condition;
    private readonly WindowSystem windowSystem = new("WeiyueMistakeTTS");

    private readonly Configuration config;
    private readonly DataStore dataStore;
    private readonly TimelineService timelineService;
    private readonly EncounterClock clock;
    private readonly TtsService ttsService;
    private readonly ReminderService reminderService;
    private readonly PartyService partyService;
    private readonly CastTriggerService castTriggerService;
    private readonly MainWindow mainWindow;

    private bool wasInCombat;
    private IReadOnlySet<string> currentPartyMemberIds = new HashSet<string>();

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IFramework framework,
        IChatGui chatGui,
        IClientState clientState,
        ICondition condition,
        IPartyList partyList,
        IObjectTable objectTable,
        IDataManager dataManager)
    {
        this.pluginInterface = pluginInterface;
        this.commandManager = commandManager;
        this.framework = framework;
        this.clientState = clientState;
        this.condition = condition;

        this.config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        this.dataStore = new DataStore(pluginInterface.ConfigDirectory.FullName);
        this.timelineService = new TimelineService(this.dataStore, dataManager);
        this.clock = new EncounterClock();
        this.ttsService = new TtsService(this.config, chatGui);
        this.reminderService = new ReminderService(this.config, this.dataStore, this.clock, this.ttsService);
        this.partyService = new PartyService(partyList, this.dataStore, objectTable);
        this.castTriggerService = new CastTriggerService(objectTable, this.clock, this.reminderService);

        this.mainWindow = new MainWindow(
            this.config,
            this.dataStore,
            this.timelineService,
            this.clock,
            this.ttsService,
            this.partyService,
            this.clientState,
            this.SaveConfig);
        this.mainWindow.IsOpen = this.config.MainWindowOpen;
        this.windowSystem.AddWindow(this.mainWindow);

        this.commandManager.AddHandler("/wym", new CommandInfo(this.OnCommand)
        {
            HelpMessage = "打开 Team Mistake。用法：/wym、/wym pull、/wym stop、/wym test",
        });

        framework.Update += this.OnFrameworkUpdate;
        pluginInterface.UiBuilder.Draw += this.DrawUi;
        pluginInterface.UiBuilder.OpenConfigUi += this.OpenConfigUi;
    }

    public void Dispose()
    {
        this.config.MainWindowOpen = this.mainWindow.IsOpen;
        this.SaveConfig();

        this.pluginInterface.UiBuilder.Draw -= this.DrawUi;
        this.pluginInterface.UiBuilder.OpenConfigUi -= this.OpenConfigUi;
        this.framework.Update -= this.OnFrameworkUpdate;
        this.windowSystem.RemoveAllWindows();
        this.commandManager.RemoveHandler("/wym");
        this.ttsService.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        var normalized = args.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Equals("open", StringComparison.OrdinalIgnoreCase))
        {
            this.mainWindow.Toggle();
            return;
        }

        if (normalized.Equals("pull", StringComparison.OrdinalIgnoreCase))
        {
            this.StartPull();
            return;
        }

        if (normalized.Equals("stop", StringComparison.OrdinalIgnoreCase))
        {
            this.StopPull();
            return;
        }

        if (normalized.Equals("test", StringComparison.OrdinalIgnoreCase))
        {
            this.ttsService.Speak("Team Mistake 测试");
            return;
        }

        if (normalized.StartsWith("sync ", StringComparison.OrdinalIgnoreCase))
        {
            var valueText = normalized[5..].Trim();
            if (double.TryParse(valueText, out var seconds))
            {
                this.clock.AdjustTo(seconds);
                return;
            }
        }

        this.mainWindow.IsOpen = true;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        this.currentPartyMemberIds = this.partyService.SyncCurrentParty();

        var inCombat = this.condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat];
        if (this.config.AutoPullDetection && inCombat && !this.wasInCombat)
        {
            this.timelineService.GetOrCreateEncounterForTerritory(this.clientState.TerritoryType);
            this.StartPull();
        }

        if (!inCombat && this.wasInCombat && this.config.AutoPullDetection)
            this.StopPull();

        this.wasInCombat = inCombat;

        var encounter = this.timelineService.CurrentEncounter(this.clientState.TerritoryType);
        this.castTriggerService.Tick(encounter, this.currentPartyMemberIds);
        this.reminderService.Tick(encounter, this.currentPartyMemberIds);
    }

    private void StartPull()
    {
        this.clock.Start();
        this.reminderService.Reset();
        this.castTriggerService.Reset();
    }

    private void StopPull()
    {
        this.clock.Stop();
        this.reminderService.Reset();
        this.castTriggerService.Reset();
    }

    private void DrawUi()
    {
        this.windowSystem.Draw();
    }

    private void OpenConfigUi()
    {
        this.mainWindow.IsOpen = true;
    }

    private void SaveConfig()
    {
        this.pluginInterface.SavePluginConfig(this.config);
    }
}
