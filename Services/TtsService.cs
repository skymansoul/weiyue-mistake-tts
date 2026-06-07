using Dalamud.Plugin.Services;
using System.Speech.Synthesis;

namespace WeiyueMistakeTTS.Services;

public sealed class TtsService : IDisposable
{
    private readonly Configuration config;
    private readonly IChatGui chatGui;
    private SpeechSynthesizer? synthesizer;
    private bool ttsAvailable = true;

    public TtsService(Configuration config, IChatGui chatGui)
    {
        this.config = config;
        this.chatGui = chatGui;

        try
        {
            this.synthesizer = new SpeechSynthesizer();
            this.ApplySettings();
        }
        catch (Exception ex)
        {
            this.ttsAvailable = false;
            this.chatGui.PrintError($"[卫月犯错提醒] TTS 初始化失败，已回退到聊天提示：{ex.Message}");
        }
    }

    public void ApplySettings()
    {
        if (this.synthesizer == null)
            return;

        this.synthesizer.Volume = Math.Clamp(this.config.TtsVolume, 0, 100);
        this.synthesizer.Rate = Math.Clamp(this.config.TtsRate, -10, 10);
    }

    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (!this.config.TtsEnabled || !this.ttsAvailable || this.synthesizer == null)
        {
            this.chatGui.Print($"[卫月犯错提醒] {text}");
            return;
        }

        try
        {
            this.ApplySettings();
            this.synthesizer.SpeakAsyncCancelAll();
            this.synthesizer.SpeakAsync(text);
        }
        catch (Exception ex)
        {
            this.ttsAvailable = false;
            this.chatGui.PrintError($"[卫月犯错提醒] TTS 播报失败，已回退到聊天提示：{ex.Message}");
            this.chatGui.Print($"[卫月犯错提醒] {text}");
        }
    }

    public void Dispose()
    {
        this.synthesizer?.Dispose();
    }
}

