using CitrixAI.Core.Caching;
using CitrixAI.Core.Utilities;
using CitrixAI.Demo.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows;

namespace CitrixAI.Demo.ViewModels
{
    /// <summary>
    /// View model for the performance monitoring dashboard.
    /// Provides real-time metrics and analytics for system optimization.
    /// </summary>
    public class PerformanceMonitorViewModel : INotifyPropertyChanged, IDisposable
    {
        private DispatcherTimer _updateTimer;
        private readonly AdvancedDetectionCache _cache;
        private bool _isMonitoring;
        private DateTime _monitoringStartTime;
        private bool _disposed;

        #region Private Fields

        private int _totalDetections;
        private double _averageDetectionTime;
        private double _cacheHitRatio;
        private double _similarityHitRatio;
        private long _currentMemoryUsage;
        private long _peakMemoryUsage;
        private int _totalCacheEntries;
        private DateTime _lastUpdateTime;
        private string _systemStatus;

        #endregion

        /// <summary>
        /// Initializes a new instance of the PerformanceMonitorViewModel class.
        /// </summary>
        /// <param name="cache">The advanced detection cache to monitor.</param>
        public PerformanceMonitorViewModel(AdvancedDetectionCache cache = null)
        {
            _cache = cache;
            _monitoringStartTime = DateTime.UtcNow;
            _systemStatus = "Initializing...";

            InitializeCollections();
            InitializeCommands();
            InitializeTimer();

            SystemStatus = "Ready";
        }

        #region Properties

        public bool IsMonitoring
        {
            get => _isMonitoring;
            set => SetProperty(ref _isMonitoring, value);
        }

        public int TotalDetections
        {
            get => _totalDetections;
            set => SetProperty(ref _totalDetections, value);
        }

        public double AverageDetectionTime
        {
            get => _averageDetectionTime;
            set => SetProperty(ref _averageDetectionTime, value);
        }

        public double CacheHitRatio
        {
            get => _cacheHitRatio;
            set => SetProperty(ref _cacheHitRatio, value);
        }

        public double SimilarityHitRatio
        {
            get => _similarityHitRatio;
            set => SetProperty(ref _similarityHitRatio, value);
        }

        public long CurrentMemoryUsage
        {
            get => _currentMemoryUsage;
            set => SetProperty(ref _currentMemoryUsage, value);
        }

        public long PeakMemoryUsage
        {
            get => _peakMemoryUsage;
            set
            {
                SetProperty(ref _peakMemoryUsage, value);
                OnPropertyChanged(nameof(MemoryEfficiencyRatio));
            }
        }

        public int TotalCacheEntries
        {
            get => _totalCacheEntries;
            set => SetProperty(ref _totalCacheEntries, value);
        }

        public DateTime LastUpdateTime
        {
            get => _lastUpdateTime;
            set => SetProperty(ref _lastUpdateTime, value);
        }

        public string SystemStatus
        {
            get => _systemStatus;
            set => SetProperty(ref _systemStatus, value);
        }

        public TimeSpan MonitoringDuration => DateTime.UtcNow - _monitoringStartTime;

        public double MemoryEfficiencyRatio
        {
            get
            {
                if (PeakMemoryUsage == 0) return 1.0;
                return Math.Max(0.0, 1.0 - ((double)CurrentMemoryUsage / PeakMemoryUsage));
            }
        }

        public string PerformanceGrade
        {
            get
            {
                var score = CalculatePerformanceScore();
                if (score >= 90) return "A+";
                if (score >= 80) return "A";
                if (score >= 70) return "B";
                if (score >= 60) return "C";
                return "D";
            }
        }

        #endregion

        #region Collections

        public ObservableCollection<CacheMetric> CacheMetrics { get; private set; }
        public ObservableCollection<MemoryUsagePoint> MemoryHistory { get; private set; }
        public ObservableCollection<StrategyPerformance> StrategyMetrics { get; private set; }
        public ObservableCollection<PerformanceAlert> ActiveAlerts { get; private set; }

        #endregion

        #region Commands

        public ICommand StartMonitoringCommand { get; private set; }
        public ICommand StopMonitoringCommand { get; private set; }
        public ICommand ClearHistoryCommand { get; private set; }
        public ICommand ExportReportCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCollections()
        {
            CacheMetrics = new ObservableCollection<CacheMetric>();
            MemoryHistory = new ObservableCollection<MemoryUsagePoint>();
            StrategyMetrics = new ObservableCollection<StrategyPerformance>();
            ActiveAlerts = new ObservableCollection<PerformanceAlert>();
        }

        private void InitializeCommands()
        {
            StartMonitoringCommand = new RelayCommand(StartMonitoring, () => !IsMonitoring);
            StopMonitoringCommand = new RelayCommand(StopMonitoring, () => IsMonitoring);
            ClearHistoryCommand = new RelayCommand(ClearHistory);
            ExportReportCommand = new RelayCommand(async () => await ExportPerformanceReportAsync());
            RefreshCommand = new RelayCommand(async () => await UpdateMetricsAsync());
        }

        private void InitializeTimer()
        {
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2) // Update every 2 seconds
            };
            _updateTimer.Tick += async (s, e) => await UpdateMetricsAsync();
        }

        #endregion

        #region Command Implementations

        private void StartMonitoring()
        {
            IsMonitoring = true;
            _monitoringStartTime = DateTime.UtcNow;
            _updateTimer.Start();
            SystemStatus = "Monitoring Active";

            AddAlert(PerformanceAlertType.Info, "Performance monitoring started");
        }

        private void StopMonitoring()
        {
            IsMonitoring = false;
            _updateTimer.Stop();
            SystemStatus = "Monitoring Stopped";

            AddAlert(PerformanceAlertType.Info, "Performance monitoring stopped");
        }

        private void ClearHistory()
        {
            CacheMetrics.Clear();
            MemoryHistory.Clear();
            StrategyMetrics.Clear();

            // Keep active alerts but add info about clearing
            AddAlert(PerformanceAlertType.Info, "Performance history cleared");
        }

        private async Task ExportPerformanceReportAsync()
        {
            try
            {
                SystemStatus = "Exporting report...";

                // Simulate report generation
                await Task.Delay(1000);

                var report = GeneratePerformanceReport();

                // In a real implementation, this would save to file or display save dialog
                SystemStatus = "Report exported successfully";
                AddAlert(PerformanceAlertType.Success, "Performance report exported");
            }
            catch (Exception ex)
            {
                SystemStatus = "Export failed";
                AddAlert(PerformanceAlertType.Error, $"Export failed: {ex.Message}");
            }
        }

        #endregion

        #region Performance Monitoring

        public async Task UpdateMetricsAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    UpdateMemoryMetrics();
                    UpdateCacheMetrics();
                    UpdateStrategyMetrics();
                    CheckPerformanceAlerts();
                });

                LastUpdateTime = DateTime.UtcNow;
                OnPropertyChanged(nameof(MonitoringDuration));
                OnPropertyChanged(nameof(PerformanceGrade));
            }
            catch (Exception ex)
            {
                AddAlert(PerformanceAlertType.Error, $"Metrics update failed: {ex.Message}");
            }
        }

        private void UpdateMemoryMetrics()
        {
            CurrentMemoryUsage = MemoryManager.GetCurrentMemoryUsage();

            if (CurrentMemoryUsage > PeakMemoryUsage)
            {
                PeakMemoryUsage = CurrentMemoryUsage;
            }

            // Add to history
            Application.Current?.Dispatcher.Invoke(() =>
            {
                MemoryHistory.Add(new MemoryUsagePoint
                {
                    Timestamp = DateTime.UtcNow,
                    MemoryUsage = CurrentMemoryUsage,
                    MemoryDelta = MemoryHistory.Any() ?
                        CurrentMemoryUsage - MemoryHistory.Last().MemoryUsage : 0
                });

                // Keep only last 100 points
                while (MemoryHistory.Count > 100)
                {
                    MemoryHistory.RemoveAt(0);
                }
            });
        }

        private void UpdateCacheMetrics()
        {
            if (_cache == null) return;

            try
            {
                var stats = _cache.GetStatistics();

                CacheHitRatio = stats.HitRatio;
                SimilarityHitRatio = stats.SimilarityRatio;
                TotalCacheEntries = stats.TotalEntries;

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    CacheMetrics.Add(new CacheMetric
                    {
                        Timestamp = DateTime.UtcNow,
                        HitRatio = stats.HitRatio,
                        SimilarityRatio = stats.SimilarityRatio,
                        TotalEntries = stats.TotalEntries,
                        TotalHits = stats.TotalHits,
                        TotalMisses = stats.TotalMisses,
                        SimilarityHits = stats.SimilarityHits,
                        ExactHits = stats.ExactHits
                    });

                    // Keep only last 50 metrics
                    while (CacheMetrics.Count > 50)
                    {
                        CacheMetrics.RemoveAt(0);
                    }
                });
            }
            catch (Exception ex)
            {
                AddAlert(PerformanceAlertType.Warning, $"Cache metrics update failed: {ex.Message}");
            }
        }

        private void UpdateStrategyMetrics()
        {
            // In a real implementation, this would query the DetectionOrchestrator
            // for strategy performance metrics
            Application.Current?.Dispatcher.Invoke(() =>
            {
                // Simulate strategy metrics
                if (StrategyMetrics.Count == 0)
                {
                    StrategyMetrics.Add(new StrategyPerformance
                    {
                        StrategyName = "AI Detection",
                        ExecutionCount = 0,
                        AverageExecutionTime = 0,
                        SuccessRate = 0,
                        LastExecutionTime = DateTime.MinValue
                    });

                    StrategyMetrics.Add(new StrategyPerformance
                    {
                        StrategyName = "Template Matching",
                        ExecutionCount = 0,
                        AverageExecutionTime = 0,
                        SuccessRate = 0,
                        LastExecutionTime = DateTime.MinValue
                    });
                }
            });
        }

        private void CheckPerformanceAlerts()
        {
            // Memory pressure alert
            if (MemoryManager.IsMemoryPressureHigh() &&
                !ActiveAlerts.Any(a => a.Type == PerformanceAlertType.Warning && a.Message.Contains("Memory pressure")))
            {
                AddAlert(PerformanceAlertType.Warning, "High memory pressure detected");
            }

            // Cache effectiveness alert
            if (CacheHitRatio < 0.3 && TotalCacheEntries > 10 &&
                !ActiveAlerts.Any(a => a.Type == PerformanceAlertType.Warning && a.Message.Contains("Cache effectiveness")))
            {
                AddAlert(PerformanceAlertType.Warning, "Low cache effectiveness detected");
            }

            // Performance degradation alert
            var performanceScore = CalculatePerformanceScore();
            if (performanceScore < 60 &&
                !ActiveAlerts.Any(a => a.Type == PerformanceAlertType.Error && a.Message.Contains("Performance degradation")))
            {
                AddAlert(PerformanceAlertType.Error, "Performance degradation detected");
            }

            // Clean up old alerts (keep only last 10)
            Application.Current?.Dispatcher.Invoke(() =>
            {
                while (ActiveAlerts.Count > 10)
                {
                    ActiveAlerts.RemoveAt(0);
                }
            });
        }

        #endregion

        #region Helper Methods

        private double CalculatePerformanceScore()
        {
            double score = 100.0;

            // Memory efficiency (30% weight)
            var memoryScore = Math.Max(0, 100 - (CurrentMemoryUsage / 10.0)); // Penalize high memory usage
            score = score * 0.7 + memoryScore * 0.3;

            // Cache effectiveness (40% weight)
            var cacheScore = CacheHitRatio * 100;
            score = score * 0.6 + cacheScore * 0.4;

            // System stability (30% weight)
            var stabilityScore = ActiveAlerts.Count(a => a.Type == PerformanceAlertType.Error) == 0 ? 100 : 50;
            score = score * 0.7 + stabilityScore * 0.3;

            return Math.Max(0, Math.Min(100, score));
        }

        private void AddAlert(PerformanceAlertType type, string message)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                ActiveAlerts.Add(new PerformanceAlert
                {
                    Type = type,
                    Message = message,
                    Timestamp = DateTime.UtcNow
                });
            });
        }

        private string GeneratePerformanceReport()
        {
            var report = $@"
Performance Report - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}
===============================================

Monitoring Duration: {MonitoringDuration:hh\:mm\:ss}
Performance Grade: {PerformanceGrade}
Performance Score: {CalculatePerformanceScore():F1}%

Memory Metrics:
- Current Usage: {CurrentMemoryUsage} MB
- Peak Usage: {PeakMemoryUsage} MB
- Efficiency Ratio: {MemoryEfficiencyRatio:P1}

Cache Performance:
- Hit Ratio: {CacheHitRatio:P1}
- Similarity Hit Ratio: {SimilarityHitRatio:P1}
- Total Entries: {TotalCacheEntries}

Active Alerts: {ActiveAlerts.Count}
Recent Memory Points: {MemoryHistory.Count}
Cache Metrics Recorded: {CacheMetrics.Count}

System Status: {SystemStatus}
";
            return report;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                _updateTimer?.Stop();
                _disposed = true;
            }
        }

        #endregion
    }

    #region Data Models

    /// <summary>
    /// Represents a cache performance metric point.
    /// </summary>
    public class CacheMetric
    {
        public DateTime Timestamp { get; set; }
        public double HitRatio { get; set; }
        public double SimilarityRatio { get; set; }
        public int TotalEntries { get; set; }
        public int TotalHits { get; set; }
        public int TotalMisses { get; set; }
        public int SimilarityHits { get; set; }
        public int ExactHits { get; set; }
    }

    /// <summary>
    /// Represents a memory usage data point.
    /// </summary>
    public class MemoryUsagePoint
    {
        public DateTime Timestamp { get; set; }
        public long MemoryUsage { get; set; }
        public long MemoryDelta { get; set; }
    }

    /// <summary>
    /// Represents performance metrics for a detection strategy.
    /// </summary>
    public class StrategyPerformance
    {
        public string StrategyName { get; set; }
        public int ExecutionCount { get; set; }
        public double AverageExecutionTime { get; set; }
        public double SuccessRate { get; set; }
        public DateTime LastExecutionTime { get; set; }
    }

    /// <summary>
    /// Represents a performance alert.
    /// </summary>
    public class PerformanceAlert
    {
        public PerformanceAlertType Type { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Types of performance alerts.
    /// </summary>
    public enum PerformanceAlertType
    {
        Info,
        Success,
        Warning,
        Error
    }

    #endregion
}