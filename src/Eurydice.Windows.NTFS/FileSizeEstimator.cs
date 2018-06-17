using System;
using System.IO;
using System.Runtime.InteropServices;
using Eurydice.Core;

namespace Eurydice.Windows.NTFS
{
    /// <summary>
    ///     File size estimator implementation.
    /// </summary>
    public sealed class FileSizeEstimator : IFileSizeEstimator
    {
        public long Estimate(string path)
        {
            var file = new FileInfo(path);

            if ((file.Attributes & FileAttributes.Compressed) > 0) return GetCompressedFileSize(file.FullName);

            long clusterSize = GetClusterSize(file.Directory.Root.FullName);

            var logicalSize = file.Length;
            var clusters = logicalSize / clusterSize;

            if (logicalSize % clusterSize > 0) clusters++;

            return clusters * clusterSize;
        }


        private static long GetCompressedFileSize(string fileName)
        {
            var lo = GetCompressedFileSize(fileName, out var hi);

            if (Marshal.GetLastWin32Error() != 0) throw new InvalidOperationException();

            return (long) (((ulong) hi << 32) + lo);
        }

        private static int GetClusterSize(string driveName)
        {
            if (!GetDiskFreeSpace(driveName, out var sectorsPerCluster, out var bytesPerSector, out _, out _))
                throw new InvalidOperationException();

            return (int) sectorsPerCluster * (int) bytesPerSector;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint GetCompressedFileSize(
            string lpFileName, out uint lpFileSizeHigh);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool GetDiskFreeSpace(
            string lpRootPathName,
            out uint lpSectorsPerCluster,
            out uint lpBytesPerSector,
            out uint lpNumberOfFreeClusters,
            out uint lpTotalNumberOfClusters);
    }
}