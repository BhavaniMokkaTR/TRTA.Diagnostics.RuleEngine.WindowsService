using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace TRTA.Diagnostics.RuleEngine.WindowsService.Metrics
{
    /// <summary>
    /// Helps in collecting various performance metrics for the current computer
    /// </summary>
    public class PerformanceMetricsCollector
    {
        private const int SleepTime = 1000; // mseconds
        private const int MovingWindowTime = 8; // seconds
        private const int MovingWindowSize = (MovingWindowTime * 1000) / SleepTime;

        private const string MetricNameCpu = "Cpu";
        private const string MetricNameAverageCpu = "AverageCpu";
        private const string MetricNameAvailableMBytes = "AvailableMBytes";
        private const string MetricNamePercentCommitCharge = "PercentCommitCharge";

        private Thread _collectorThread;

        private PerformanceCounter _cpuCounter;
        private PerformanceCounter _memoryCounter;
        private PerformanceCounter _memoryCounter2;

        private Queue<float> _cpuMovingWindow;

        /// <summary>
        /// Contains metrics named above in constants
        /// </summary>
        private ConcurrentDictionary<string, float> _metrics = new ConcurrentDictionary<string, float>();

        public PerformanceMetricsCollector()
        {
            _metrics[MetricNameCpu] = 0;
            _metrics[MetricNameAverageCpu] = 0;
            _metrics[MetricNameAvailableMBytes] = 0;
            _metrics[MetricNamePercentCommitCharge] = 0;

            _collectorThread = new Thread(CollectorThreadFunc);
            _collectorThread.Start();
        }

        /// <summary>
        /// Stops the collector internal thread
        /// </summary>
        public void Stop()
        {
            _collectorThread.Interrupt();
            _collectorThread.Join();
        }

        /// <summary>
        /// Instantaneous CPU usage
        /// </summary>
        public float Cpu
        {
            get { return _metrics[MetricNameCpu]; }
        }

        /// <summary>
        /// Average CPU usage over last MovingWindowTime number of seconds
        /// </summary>
        public float AverageCpu
        {
            get { return _metrics[MetricNameAverageCpu]; }
        }

        /// <summary>
        /// Available physical MBytes
        /// </summary>
        public float AvailableMBytes
        {
            get { return _metrics[MetricNameAvailableMBytes]; }
        }

        public float PercentCommitCharge
        {
            get { return _metrics[MetricNamePercentCommitCharge]; }
        }

        /// <summary>
        /// Total number of logical processors available (Procs x Cores x Hyp-threads/Core)
        /// </summary>
        public int ProcessorCount
        {
            get { return Environment.ProcessorCount; }
        }

        //---------------------------------------------------------------------

        private void Init()
        {
            _cpuCounter = new PerformanceCounter();
            _cpuCounter.CategoryName = "Processor";
            _cpuCounter.CounterName = "% Processor Time";
            _cpuCounter.InstanceName = "_Total";

            _memoryCounter = new PerformanceCounter();
            _memoryCounter.CategoryName = "Memory";
            _memoryCounter.CounterName = "Available MBytes";

            _memoryCounter2 = new PerformanceCounter();
            _memoryCounter2.CategoryName = "Memory";
            _memoryCounter2.CounterName = "% Committed Bytes In Use";

            _cpuMovingWindow = new Queue<float>();
        }

        /// <summary>
        /// Collector thread function sits in a loop collecting metrics
        /// </summary>
        /// <param name="o"></param>
        private void CollectorThreadFunc(object o)
        {
            Init();

            while (true)
            {
                try
                {
                    Collect();
                    Thread.Sleep(SleepTime);
                }
                catch (ThreadInterruptedException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Performs the various calls to collect metrics
        /// </summary>
        private void Collect()
        {
            // cpu
            float val = _cpuCounter.NextValue();
            _metrics[MetricNameCpu] = val;

            // available MBytes
            _metrics[MetricNameAvailableMBytes] = _memoryCounter.NextValue();

            // commit charge
            _metrics[MetricNamePercentCommitCharge] = _memoryCounter2.NextValue();

            // average cpu
            UpdateAverageCpu(val);
        }

        private void UpdateAverageCpu(float v)
        {
            _cpuMovingWindow.Enqueue(v);
            if (_cpuMovingWindow.Count > MovingWindowSize)
                _cpuMovingWindow.Dequeue();

            float avgCpu = _cpuMovingWindow.Sum() / _cpuMovingWindow.Count;
            _metrics[MetricNameAverageCpu] = avgCpu;
        }
    }
}
