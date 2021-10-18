using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace ReliableDownloader
{
    public class FileDownloader : IFileDownloader
    {
        private CancellationTokenSource _cancellationSource;

        public async Task<bool> DownloadFile(string contentFileUrl, string localFilePath, Action<FileProgress> onProgressChanged)
        {
            _cancellationSource = new CancellationTokenSource();
            string responseContentHash;

            try
            {
                var systemCalls = new WebSystemCalls();
                var cancellationToken = _cancellationSource.Token;

                var headerResponse = await systemCalls.GetHeadersAsync(contentFileUrl, cancellationToken);
                var isPartialContentSupported = headerResponse.Headers.AcceptRanges.Any(r => r == "bytes");
                var contentResponse = await systemCalls.DownloadContent(contentFileUrl, cancellationToken);
                var stopWatch = new Stopwatch();

                using (var stream = await contentResponse.Content.ReadAsStreamAsync())
                using (var fileStream = File.Create(localFilePath)) // TODO: Split to file writer interface for testing
                {
                    var buffer = new byte[8192];

                    // TODO: Split to stream reader interface for unit tests
                    var readBytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                    var totalBytesToDownload = contentResponse.Content.Headers.ContentLength.GetValueOrDefault();
                    var fileProgress = new FileProgress(totalBytesToDownload, 0, TimeSpan.FromMinutes(10));
                    double bytesDownloadedPerMilleseconds = 0;
                    responseContentHash = Convert.ToBase64String(contentResponse.Content.Headers.ContentMD5);

                    stopWatch.Start();

                    while (readBytes > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, readBytes);
                        fileProgress.TotalBytesDownloaded += readBytes;

                        // TODO: separate to method
                        bytesDownloadedPerMilleseconds = fileProgress.TotalBytesDownloaded / stopWatch.ElapsedMilliseconds;
                        fileProgress.EstimatedRemaining = TimeSpan.FromMilliseconds((totalBytesToDownload - fileProgress.TotalBytesDownloaded) / bytesDownloadedPerMilleseconds);

                        onProgressChanged(fileProgress);

                        readBytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                    }
                    stopWatch.Stop();
                }
            }
            catch (Exception exception)
            {
                // TODO: Split out, compute hash to ensure file integrity - Do partial download
                var downloadedBytes = File.ReadAllBytes(localFilePath);
                var md5 = MD5.Create();
                var contentHash = md5.ComputeHash(downloadedBytes);
                var hash = Convert.ToBase64String(contentHash);
            }

            return true;
        }

        public void CancelDownloads()
        {
            _cancellationSource.Cancel();
        }
    }
}