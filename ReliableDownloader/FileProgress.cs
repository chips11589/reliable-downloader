using System;

namespace ReliableDownloader
{
    public class FileProgress
    {
        public FileProgress(long? totalFileSize, long totalBytesDownloaded, TimeSpan? estimatedRemaining)
        {
            TotalFileSize = totalFileSize;
            TotalBytesDownloaded = totalBytesDownloaded;
            EstimatedRemaining = estimatedRemaining;
        }

        public long? TotalFileSize { get; }
        public long TotalBytesDownloaded { get; set; }
        public double? ProgressPercent => TotalBytesDownloaded * 100 / TotalFileSize;
        public TimeSpan? EstimatedRemaining { get; set; }
    }
}