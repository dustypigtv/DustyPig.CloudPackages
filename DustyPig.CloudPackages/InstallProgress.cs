namespace DustyPig.CloudPackages;

public class InstallProgress
{
    internal InstallProgress(string status, int progress, int totalProgress)
    {
        Status = status;
        Progress = progress;
        TotalProgress = totalProgress;
    }

    public string Status{ get; }

    public int Progress { get; }
    
    public int TotalProgress { get; }

    public override string ToString() => $"{Status}: {Progress}% (Total: {TotalProgress}%)";
}
