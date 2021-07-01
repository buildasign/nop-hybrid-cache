using System.Text;
using System.Threading;

namespace BAS.Nop.Plugin.Misc.HybridCache.Common
{
    public interface IThreadMonitor
    {
        ThreadPoolStats GetThreadPoolStats();
        string GetThreadPoolStatsFormatted();
    }

    public class ThreadMonitor : IThreadMonitor
    {
        /// <summary>
        /// Returns the current thread pool usage statistics for the CURRENT AppDomain/Process
        /// </summary>
        public ThreadPoolStats GetThreadPoolStats()
        {
            //BusyThreads =  TP.GetMaxThreads() –TP.GetAVailable();
            //If BusyThreads >= TP.GetMinThreads(), then threadpool growth throttling is possible.

            int maxIoThreads, maxWorkerThreads;
            ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxIoThreads);

            int freeIoThreads, freeWorkerThreads;
            ThreadPool.GetAvailableThreads(out freeWorkerThreads, out freeIoThreads);

            int minIoThreads, minWorkerThreads;
            ThreadPool.GetMinThreads(out minWorkerThreads, out minIoThreads);

            int busyIoThreads = maxIoThreads - freeIoThreads;
            int busyWorkerThreads = maxWorkerThreads - freeWorkerThreads;

            return new ThreadPoolStats(
                    busyIoThreads,
                    freeIoThreads,
                    minIoThreads,
                    maxIoThreads,
                    busyWorkerThreads,
                    freeWorkerThreads,
                    minWorkerThreads,
                    maxWorkerThreads
                );
        }

        public string GetThreadPoolStatsFormatted()
        {
            return FormatStats(GetThreadPoolStats());
        }

        private string FormatStats(ThreadPoolStats stats)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"IOCP: (Busy={stats.BusyIoThreads},Free={stats.FreeIoThreads},Min={stats.MinIoThreads},Max={stats.MaxIoThreads})");
            sb.AppendLine($"WORKER: (Busy={stats.BusyWorkerThreads},Free={stats.FreeWorkerThreads},Min={stats.MinWorkerThreads},Max={stats.MaxWorkerThreads})");

            return sb.ToString();
        }

    }

    public class ThreadPoolStats
    {
        public ThreadPoolStats(int busyIoThreads, int freeIoThreads, int minIoThreads, int maxIoThreads, int busyWorkerThreads, int freeWorkerThreads, int minWorkerThreads, int maxWorkerThreads)
        {
            BusyIoThreads = busyIoThreads;
            FreeIoThreads = freeIoThreads;
            MinIoThreads = minIoThreads;
            MaxIoThreads = maxIoThreads;
            BusyWorkerThreads = busyWorkerThreads;
            FreeWorkerThreads = freeWorkerThreads;
            MinWorkerThreads = minWorkerThreads;
            MaxWorkerThreads = maxWorkerThreads;
        }

        public int BusyIoThreads { get; private set; }
        public int FreeIoThreads { get; private set; }
        public int MinIoThreads { get; private set; }
        public int MaxIoThreads { get; private set; }
        public int BusyWorkerThreads { get; private set; }
        public int FreeWorkerThreads { get; private set; }
        public int MinWorkerThreads { get; private set; }
        public int MaxWorkerThreads { get; private set; }
    }

}
