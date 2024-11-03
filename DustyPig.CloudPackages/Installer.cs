using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.CloudPackages;

static class Installer
{
    static readonly HttpClient client = new();

    public static async Task InstallAsync(Uri cloudUri, DirectoryInfo root, IProgress<InstallProgress> progress, bool deleteNonPackageFiles, CancellationToken cancellationToken)
    {
        progress?.Report(new InstallProgress("Loading package.json", 0, 0));
        var package = await client.GetFromJsonAsync<Package>(cloudUri, cancellationToken).ConfigureAwait(false);

        double totalSize = package.Files.Sum(f => f.FileSize);
        double totalProgress = 0;

        if(deleteNonPackageFiles)
        {
            var localPackageFiles = package.Files.Select(f => Path.Combine(root.FullName, f.RelativePath.Replace('/', Path.DirectorySeparatorChar))).ToList();
            foreach (var file in root.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!localPackageFiles.Any(f => f == file.FullName))
                    file.Delete();
            }
        }

        foreach (var file in package.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int lastTotalProgress = InstallPercent(totalProgress, totalSize);
            int lastFileProgress = 0;
            progress?.Report(new InstallProgress($"File: {file.RelativePath}", lastFileProgress, lastTotalProgress));
            
            if (!file.Installed(root))
            {
                var dlprogress = new Progress<DownloadProgress>(dp =>
                {
                    int newTotalProgress = InstallPercent(totalProgress + dp.DownloadedBytes, totalSize);
                    int newFileProgress = InstallPercent(dp.DownloadedBytes, dp.TotalBytes);
                    if (newTotalProgress != lastTotalProgress || newFileProgress != lastFileProgress)
                    {
                        lastTotalProgress = newTotalProgress;
                        lastFileProgress = newFileProgress;
                        progress?.Report(new InstallProgress(
                            $"File: {file.RelativePath}",
                            lastFileProgress,
                            lastTotalProgress
                        ));
                    }
                });

                string fileUri = cloudUri.ToString();
                fileUri = fileUri[..(fileUri.LastIndexOf('/') + 1)] + $"v{package.Version}/" + file.RelativePath + ".dpcp";
                
                var tmpFile = new FileInfo(Path.GetTempFileName());
                tmpFile.Delete();
           
                await client.DownloadFileAsync(new Uri(fileUri), tmpFile, dlprogress, cancellationToken).ConfigureAwait(false);

                var dstFile = new FileInfo(Path.Combine(root.FullName, file.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
                dstFile.Directory.Create();
                tmpFile.MoveTo(dstFile.FullName, true);
            }

            totalProgress += file.FileSize;
        }

        progress?.Report(new InstallProgress("Done", 100, 100));
    }

    static int InstallPercent(double progress, double size) =>
        Convert.ToInt32(Math.Min(100, Math.Max(0, progress / size * 100)));
}

