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
    static HttpClient _client = null;

    static HttpClient PrivateHttpClient()
    {
        _client ??= new();
        return _client;
    }

    public static Task InstallAsync(Uri cloudUri, DirectoryInfo root, IProgress<InstallProgress> progress, bool deleteNonPackageFiles, CancellationToken cancellationToken = default) =>
        InstallAsync(PrivateHttpClient(), cloudUri, root, progress, deleteNonPackageFiles, cancellationToken);


    public static async Task InstallAsync(HttpClient client, Uri cloudUri, DirectoryInfo root, IProgress<InstallProgress> progress, bool deleteNonPackageFiles, CancellationToken cancellationToken = default)
    {
        progress?.Report(new InstallProgress("Loading package.json", 0, 0));
        Package package = await client.GetFromJsonAsync<Package>(cloudUri, cancellationToken).ConfigureAwait(false);

        double totalSize = package.Files.Sum(f => f.FileSize);
        double totalDownloaded = 0;

        if (deleteNonPackageFiles)
        {
            List<string> localPackageFiles = [.. package.Files.Select(f => Path.Combine(root.FullName, f.RelativePath.Replace('/', Path.DirectorySeparatorChar)))];
            foreach (FileInfo file in root.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!localPackageFiles.Any(f => f == file.FullName))
                    file.Delete();
            }
        }


        
        foreach (PackageFile file in package.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int lastTotalProgress = InstallPercent(totalDownloaded, totalSize);
            int lastFileProgress = 0;

            progress?.Report(new InstallProgress($"Scanning: {file.RelativePath}", lastFileProgress, lastTotalProgress));
            if (!file.Installed(root))
            {
                string fileUri = cloudUri.ToString();
                fileUri = fileUri[..(fileUri.LastIndexOf('/') + 1)] + $"v{package.Version}/" + file.RelativePath + Constants.PACKAGE_FILE_EXT;

                using HttpRequestMessage request = new (HttpMethod.Get, fileUri);
                using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                long fileSize = response.Content.Headers.ContentLength ?? -1;
                long fileDownloaded = 0;

                FileInfo dstFile = new (Path.Combine(root.FullName, file.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
                FileInfo tmpFile = new (dstFile.FullName + Constants.PACKAGE_FILE_EXT);
                tmpFile.Directory.Create();
                if (tmpFile.Exists)
                    tmpFile.Delete();


                //4096 is STILL the file stream default buffer size in .net 9 in 2024
                byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);

                await using (FileStream stream = new (tmpFile.FullName, FileMode.Create, FileAccess.Write, FileShare.None, buffer.Length, true))
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
                                int newTotalProgress = InstallPercent(totalDownloaded + fileDownloaded, totalSize);
                                int newFileProgress = InstallPercent(fileDownloaded, fileSize);
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
                progress?.Report(new InstallProgress($"Downloading: {file.RelativePath}", 100, InstallPercent(totalDownloaded + fileDownloaded, totalSize)));
            }

            totalDownloaded += file.FileSize;
        }

        progress?.Report(new InstallProgress("Done", 100, 100));
    }




    static int InstallPercent(double progress, double size)
    {
        try
        {
            var perc = progress / size * 100;
            return Convert.ToInt32(Math.Min(99, Math.Max(0, perc)));
        }
        catch { return -1; }
    }
}

