using System;
using System.Buffers;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.CloudPackages;

static class Extensions
{
    const int FILE_BUFFER_SIZE = 4096;
    const int COPYTO_BUFFER_SIZE = 81920;

    public static async Task DownloadFileAsync(this HttpClient httpClient, Uri uri, FileInfo fileInfo, IProgress<DownloadProgress> progress = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        
        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        long totalBytes = response.Content.Headers.ContentLength ?? -1;
        long totalDownloaded = 0;

        byte[] buffer = ArrayPool<byte>.Shared.Rent(COPYTO_BUFFER_SIZE);

        fileInfo.Directory.Create();
        using (var fileStream = new FileStream(fileInfo.FullName, FileMode.Create, FileAccess.Write, FileShare.Read, FILE_BUFFER_SIZE, true))
        {
            try
            {
                while (true)
                {
                    var read = await contentStream.ReadAsync(new Memory<byte>(buffer), cancellationToken).ConfigureAwait(false);
                    if (read > 0)
                        await fileStream.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, read), cancellationToken).ConfigureAwait(false);
                    if (progress != null)
                    {
                        totalDownloaded += read;
                        progress.Report(new DownloadProgress(totalDownloaded, totalBytes));
                    }

                    if (read <= 0)
                        break;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        fileInfo.Refresh();
    }
}
