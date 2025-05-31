using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.CloudPackages;

static class Installer
{
    public static async Task Install(HttpClient client, Uri cloudUri, DirectoryInfo installDirectory, IProgress<InstallProgress> progress, bool deleteNonPackageFiles, CancellationToken cancellationToken)
    {
        progress?.Report(new InstallProgress("Loading package.json", 0, 0));
        Package package = await client.GetFromJsonAsync<Package>(cloudUri, cancellationToken).ConfigureAwait(false);

        double totalSize = package.Files.Sum(f => f.FileSize);
        double totalDownloaded = 0;

        if (deleteNonPackageFiles)
        {
            List<string> localPackageFiles = [.. package.Files.Select(f => Path.Combine(installDirectory.FullName, f.RelativePath.Replace('/', Path.DirectorySeparatorChar)))];
            foreach (FileInfo file in installDirectory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!localPackageFiles.Any(f => f == file.FullName))
                    file.Delete();
            }
        }


        
        foreach (PackageFile file in package.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int lastTotalProgress = CalcPercent(totalDownloaded, totalSize);
            int lastFileProgress = 0;

            progress?.Report(new InstallProgress($"Scanning: {file.RelativePath}", lastFileProgress, lastTotalProgress));
            if (!file.Installed(installDirectory))
            {
                string fileUri = cloudUri.ToString();
                fileUri = fileUri[..(fileUri.LastIndexOf('/') + 1)] + $"v{package.Version}/" + file.RelativePath + Constants.PACKAGE_FILE_EXT;

                using HttpRequestMessage request = new (HttpMethod.Get, fileUri);
                using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                long fileSize = response.Content.Headers.ContentLength ?? -1;
                long fileDownloaded = 0;

                FileInfo dstFile = new (Path.Combine(installDirectory.FullName, file.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
                FileInfo tmpFile = new (dstFile.FullName + Constants.PACKAGE_FILE_EXT);
                tmpFile.Directory.Create();
                if (tmpFile.Exists)
                    tmpFile.Delete();


                byte[] buffer = ArrayPool<byte>.Shared.Rent(Constants.FILE_BUFFER_SIZE);
                await using (FileStream stream = new (tmpFile.FullName, FileMode.Create, FileAccess.Write, FileShare.None, Constants.FILE_BUFFER_SIZE, true))
                {
                    try
                    {
                        while (true)
                        {
                            int read = await contentStream.ReadAsync(new Memory<byte>(buffer), cancellationToken).ConfigureAwait(false);
                            if (read <= 0)
                                break; 
                            
                            await stream.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, read), cancellationToken).ConfigureAwait(false);

                            if (progress != null)
                            {
                                fileDownloaded += read;
                                int newTotalProgress = CalcPercent(totalDownloaded + fileDownloaded, totalSize);
                                int newFileProgress = CalcPercent(fileDownloaded, fileSize);
                                if (newTotalProgress > lastTotalProgress || newFileProgress > lastFileProgress)
                                {
                                    progress.Report(new InstallProgress(
                                         $"Downloading: {file.RelativePath}",
                                         newFileProgress,
                                         newTotalProgress
                                    ));

                                    lastTotalProgress = newTotalProgress;
                                    lastFileProgress = newFileProgress;
                                }
                            }
                        }

                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }

                tmpFile.Refresh();
                tmpFile.MoveTo(Path.Combine(tmpFile.Directory.FullName, Path.GetFileNameWithoutExtension(tmpFile.Name)), true);


                totalDownloaded += file.FileSize; 
                progress?.Report(new InstallProgress($"Downloading: {file.RelativePath}", 100, CalcPercent(totalDownloaded + fileDownloaded, totalSize)));
            }

            totalDownloaded += file.FileSize;
        }

        progress?.Report(new InstallProgress("Done", 100, 100));
    }


    public static async Task UnInstall(HttpClient client, Uri cloudUri, DirectoryInfo installDirectory, IProgress<InstallProgress> progress, CancellationToken cancellationToken)
    {
        progress?.Report(new InstallProgress("Loading package.json", 0, 0));
        Package package = await client.GetFromJsonAsync<Package>(cloudUri, cancellationToken).ConfigureAwait(false);

        int prevProgress = -1;
        for(int i = 0; i < package.Files.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            PackageFile file = package.Files[i];

            if (progress != null)
            {
                int newProgress = CalcPercent(i, package.Files.Count);
                if (newProgress > prevProgress)
                {
                    prevProgress = newProgress;
                    progress?.Report(new InstallProgress("Deleting: " + file.RelativePath, newProgress, newProgress));
                }
            }

            FileInfo dstFile = new(Path.Combine(installDirectory.FullName, file.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (dstFile.Exists)
                dstFile.Delete();

            //If the files parent directory is empty, delete it
            try { dstFile.Directory.Delete(false); }
            catch { }
        }
    }


    static int CalcPercent(double progress, double size)
    {
        try
        {
            var perc = progress / size * 100;
            return Convert.ToInt32(Math.Min(99, Math.Max(0, perc)));
        }
        catch { return -1; }
    }
}

