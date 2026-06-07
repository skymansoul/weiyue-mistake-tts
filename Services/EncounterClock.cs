namespace WeiyueMistakeTTS.Services;

public sealed class EncounterClock
{
    private DateTimeOffset? pullStartedAt;

    public long PullId { get; private set; }

    public bool IsRunning => this.pullStartedAt.HasValue;

    public double ElapsedSeconds => this.pullStartedAt.HasValue
        ? Math.Max(0, (DateTimeOffset.Now - this.pullStartedAt.Value).TotalSeconds)
        : 0;

    public void Start(double elapsedSeconds = 0)
    {
        this.pullStartedAt = DateTimeOffset.Now - TimeSpan.FromSeconds(Math.Max(0, elapsedSeconds));
        this.PullId++;
    }

    public void Stop()
    {
        this.pullStartedAt = null;
    }

    public void AdjustTo(double elapsedSeconds)
    {
        if (!this.pullStartedAt.HasValue)
        {
            this.Start(elapsedSeconds);
            return;
        }

        this.pullStartedAt = DateTimeOffset.Now - TimeSpan.FromSeconds(Math.Max(0, elapsedSeconds));
    }
}

