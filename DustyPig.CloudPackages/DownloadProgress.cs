namespace DustyPig.CloudPackages;

class DownloadProgress
{
    internal DownloadProgress(long downloaded, long total)
    {
        DownloadedBytes = downloaded;
        TotalBytes = total;
        Percent = downloaded > total ? -1 : downloaded / (double)total;
    }

    public long DownloadedBytes { get; }

    public long TotalBytes { get; }

    public double Percent { get; }
}
