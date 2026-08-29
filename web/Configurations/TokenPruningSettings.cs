namespace PhiZoneApi.Configurations;

/// <summary>
///     Determines how expired OpenIddict tokens are pruned.
/// </summary>
public class TokenPruningSettings
{
    /// <summary>
    ///     Determines whether token pruning is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Determines how long to wait after startup before the first pruning run, in minutes.
    /// </summary>
    public int InitialDelayMinutes { get; set; } = 15;

    /// <summary>
    ///     Determines the interval between pruning runs, in hours.
    /// </summary>
    public int IntervalHours { get; set; } = 6;

    /// <summary>
    ///     Determines the maximum number of rows deleted by a single batch.
    /// </summary>
    public int BatchSize { get; set; } = 5000;

    /// <summary>
    ///     Determines the maximum number of batches deleted in a single run.
    /// </summary>
    public int MaxBatchesPerRun { get; set; } = 50;

    /// <summary>
    ///     Determines after how many consecutive batches without any expired row the run stops early.
    /// </summary>
    public int StopAfterCleanBatches { get; set; } = 10;

    /// <summary>
    ///     Determines how long to pause between batches, in seconds.
    /// </summary>
    public int PauseBetweenBatchesSeconds { get; set; } = 5;

    /// <summary>
    ///     Determines the database command timeout applied to each batch, in seconds.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 15;

    /// <summary>
    ///     Determines how long the Redis lock preventing concurrent pruning runs is held, in minutes.
    /// </summary>
    public int LockTtlMinutes { get; set; } = 30;
}
