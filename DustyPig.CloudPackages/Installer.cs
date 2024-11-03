using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.CloudPackages;

static class Installer
{
    //1 kb
    const int BASE_BUFFER_SIZE = 1024;

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
        var package = await client.GetFromJsonAsync<Package>(cloudUri, cancellationToken).ConfigureAwait(false);

        double totalSize = package.Files.Sum(f => f.FileSize);
        double totalDownloaded = 0;

        if (deleteNonPackageFiles)
        {
            var localPackageFiles = package.Files.Select(f => Path.Combine(root.FullName, f.RelativePath.Replace('/', Path.DirectorySeparatorChar))).ToList();
            foreach (var file in root.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!localPackageFiles.Any(f => f == file.FullName))
                    file.Delete();
            }
        }


        int multiplier = 1;
        try
        {
            var free = MemoryMetrics.Get().Free;
            var max = Math.Min(int.MaxValue, free * 0.01);

            while (true)
            {
                var tst = multiplier * 2;
                if (BASE_BUFFER_SIZE * tst < max)
                    multiplier = tst;
                else
                    break;

                //Max out at 1 MB
                if (multiplier == BASE_BUFFER_SIZE)
                    break;
            }
        }
        catch { }



        foreach (var file in package.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int lastTotalProgress = InstallPercent(totalDownloaded, totalSize);
            int lastFileProgress = 0;

            progress?.Report(new InstallProgress($"Scanning: {file.RelativePath}", lastFileProgress, lastTotalProgress));
            if (!file.Installed(root))
            {
                string fileUri = cloudUri.ToString();
                fileUri = fileUri[..(fileUri.LastIndexOf('/') + 1)] + $"v{package.Version}/" + file.RelativePath + ".dpcp";

                using var request = new HttpRequestMessage(HttpMethod.Get, fileUri);
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                long fileSize = response.Content.Headers.ContentLength ?? -1;
                long fileDownloaded = 0;

                var dstFile = new FileInfo(Path.Combine(root.FullName, file.RelativePath.Replace('/', Path.DirectorySeparatorChar)) + ".dpcp");
                dstFile.Directory.Create();
                if (dstFile.Exists)
                    dstFile.Delete();


                
                int bufferSize = multiplier * BASE_BUFFER_SIZE;
                byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

                bool memoryStream = fileSize > 0 && fileSize <= bufferSize;

                Stream stream = memoryStream ?
                    new MemoryStream() :
                    new FileStream(dstFile.FullName, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true);

                try
                {
                    while (true)
                    {
                        var read = await contentStream.ReadAsync(new Memory<byte>(buffer), cancellationToken).ConfigureAwait(false);
                        if (read > 0)
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

                        if (read <= 0)
                            break;
                    }

                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }


                if (memoryStream)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    File.WriteAllBytes(dstFile.FullName, ((MemoryStream)stream).ToArray());
                }
                stream.Dispose();

                dstFile.Refresh();
                dstFile.MoveTo(Path.Combine(dstFile.Directory.FullName, Path.GetFileNameWithoutExtension(dstFile.Name)), true);


                totalDownloaded += file.FileSize; 
                int newTotalProgress = InstallPercent(totalDownloaded + fileDownloaded, totalSize);
                progress?.Report(new InstallProgress($"Downloading: {file.RelativePath}", 100, newTotalProgress));
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

