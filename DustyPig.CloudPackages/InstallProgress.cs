namespace DustyPig.CloudPackages;

/// <summary>
/// Reports package installation progress
/// </summary>
public class InstallProgress
{
    internal InstallProgress(string status, int progress, int totalProgress)
    {
        Status = status;
        Progress = progress;
        TotalProgress = totalProgress;
    }

    /// <summary>
    /// The current operation
    /// </summary>
    public string Status{ get; }

    /// <summary>
    /// Percent of current file progress expressed as an integer between 0 and 100
    /// </summary>
    public int Progress { get; }
    
    /// <summary>
    /// Percent of total progress expressed as an integer between 0 and 100
    /// </summary>
    public int TotalProgress { get; }

    public override string ToString() => $"{Status}: {Progress}% (Total: {TotalProgress}%)";
}
