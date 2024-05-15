namespace MatrixEngine.WorkerService;

public class CronScheduleOptions
{
    public const string Cron = "Cron";
    /// <summary>
    /// Cron Expression
    /// Support library document
    /// https://github.com/HangfireIO/Cronos 
    /// </summary>
    public string Schedule { get; set; } = string.Empty;
}