using CitrixAI.Core.Interfaces;
using CitrixAI.Core.Models;
using CitrixAI.Detection.Orchestrator;
using CitrixAI.Detection.Strategies;
using CitrixAI.Vision.Utilities;
using CitrixAI.Core.Caching;
using CitrixAI.Core.Classification;
using CitrixAI.Demo.Views;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CitrixAI.Core.Utilities;
using System.Collections.Generic;

namespace CitrixAI.Demo.ViewModels
{
    /// <summary>
    /// Main view model for the CitrixAI Demo application.
    /// Implements MVVM pattern with proper separation of concerns.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        #region Private Fields

        private DetectionOrchestrator _detectionOrchestrator;
        private ScreenshotCapture _screenshotCapture;
        private BitmapImage _currentImage;
        private string _currentImagePath;
        private string _statusMessage;
        private string _logOutput;
        private bool _isProcessing;
        private double _confidenceThreshold = 0.8;
        private int _maxResults = 20;
        private bool _useTemplateMatching = true;
        private bool _useFeatureDetection = true;
        private bool _useAIDetection = true;
        private bool _useOCR = true;
        private double _detectionTime;
        private int _elementsFound;
        private double _imageQuality = 1.0;
        private DetectionResultViewModel _selectedResult;
        private AdvancedDetectionCache _advancedCache;
        private BasicElementClassifier _elementClassifier;
        private ParallelDetectionOrchestrator _parallelOrchestrator;
        private PerformanceMonitorViewModel _performanceMonitor;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            LogOutput = "CitrixAI Element Detection POC\n" +
           "================================\n";

            InitializeCommands();
            InitializeDetectionSystem(); // Now log messages will be preserved
            InitializeCollections();

            StatusMessage = "Ready - Load an image or capture screenshot to begin";
            LogMessage("System initialized and ready for detection.");
        }

        #endregion

        #region Properties

        public BitmapImage CurrentImage
        {
            get => _currentImage;
            set => SetProperty(ref _currentImage, value);
        }

        public string CurrentImagePath
        {
            get => _currentImagePath;
            set => SetProperty(ref _currentImagePath, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string LogOutput
        {
            get => _logOutput;
            set => SetProperty(ref _logOutput, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public double ConfidenceThreshold
        {
            get => _confidenceThreshold;
            set => SetProperty(ref _confidenceThreshold, value);
        }

        public int MaxResults
        {
            get => _maxResults;
            set => SetProperty(ref _maxResults, value);
        }

        public bool UseTemplateMatching
        {
            get => _useTemplateMatching;
            set => SetProperty(ref _useTemplateMatching, value);
        }

        public bool UseFeatureDetection
        {
            get => _useFeatureDetection;
            set => SetProperty(ref _useFeatureDetection, value);
        }

        public bool UseAIDetection
        {
            get => _useAIDetection;
            set => SetProperty(ref _useAIDetection, value);
        }

        public bool UseOCR
        {
            get => _useOCR;
            set => SetProperty(ref _useOCR, value);
        }

        public double DetectionTime
        {
            get => _detectionTime;
            set => SetProperty(ref _detectionTime, value);
        }

        public int ElementsFound
        {
            get => _elementsFound;
            set => SetProperty(ref _elementsFound, value);
        }

        public double ImageQuality
        {
            get => _imageQuality;
            set => SetProperty(ref _imageQuality, value);
        }

        public DetectionResultViewModel SelectedResult
        {
            get => _selectedResult;
            set => SetProperty(ref _selectedResult, value);
        }
        public double CacheHitRatio
        {
            get => _cacheHitRatio;
            set => SetProperty(ref _cacheHitRatio, value);
        }

        public int CacheEntries
        {
            get => _cacheEntries;
            set => SetProperty(ref _cacheEntries, value);
        }

        public double ClassificationAccuracy
        {
            get => _classificationAccuracy;
            set => SetProperty(ref _classificationAccuracy, value);
        }

        private double _cacheHitRatio;
        private int _cacheEntries;
        private double _classificationAccuracy;

        public string SystemResourceStatus
        {
            get
            {
                try
                {
                    var info = ResourceMonitor.GetSystemResourceInfo();
                    return $"CPU: {info.CurrentCpuUsage:F1}% | Memory: {info.ProcessMemoryMB}MB | Cores: {info.CpuCores}";
                }
                catch
                {
                    return "Resource info unavailable";
                }
            }
        }

        public string PerformanceGrade
        {
            get
            {
                try
                {
                    if (_performanceMonitor != null)
                        return _performanceMonitor.PerformanceGrade;
                    return "N/A";
                }
                catch
                {
                    return "N/A";
                }
            }
        }


        public ObservableCollection<DetectionResultViewModel> DetectionResults { get; private set; }
        public ObservableCollection<DetectedElementViewModel> DetectedElements { get; private set; }

        #endregion

        #region Commands

        public ICommand OpenImageCommand { get; private set; }
        public ICommand CaptureScreenshotCommand { get; private set; }
        public ICommand RunDetectionCommand { get; private set; }
        public ICommand RunTemplateMatchingCommand { get; private set; }
        public ICommand RunAIDetectionCommand { get; private set; }
        public ICommand RunAllStrategiesCommand { get; private set; }
        public ICommand ClearResultsCommand { get; private set; }
        public ICommand OpenMockGeneratorCommand { get; private set; }
        public ICommand OpenAnnotationToolCommand { get; private set; }
        public ICommand OpenPerformanceMonitorCommand { get; private set; }
        public ICommand AboutCommand { get; private set; }
        public ICommand ExitCommand { get; private set; }

        public ICommand RunSegment1TestCommand { get; private set; }

        #endregion

        #region Command Implementations

        private void InitializeCommands()
        {
            OpenImageCommand = new RelayCommand(async () => await OpenImageAsync());
            CaptureScreenshotCommand = new RelayCommand(async () => await CaptureScreenshotAsync());
            RunDetectionCommand = new RelayCommand(async () => await RunDetectionAsync(), CanRunDetection);
            RunTemplateMatchingCommand = new RelayCommand(async () => await RunTemplateMatchingAsync(), CanRunDetection);
            RunAIDetectionCommand = new RelayCommand(async () => await RunAIDetectionAsync(), CanRunDetection);
            RunAllStrategiesCommand = new RelayCommand(async () => await RunAllStrategiesAsync(), CanRunDetection);
            ClearResultsCommand = new RelayCommand(ClearResults, () => DetectionResults?.Count > 0);
            OpenMockGeneratorCommand = new RelayCommand(OpenMockGenerator);
            OpenAnnotationToolCommand = new RelayCommand(OpenAnnotationTool);
            OpenPerformanceMonitorCommand = new RelayCommand(OpenPerformanceMonitor);
            AboutCommand = new RelayCommand(ShowAbout);
            ExitCommand = new RelayCommand(() => Application.Current.Shutdown());
            RunSegment1TestCommand = new RelayCommand(async () => await RunSegment1TestAsync());
        }

        private async Task RunSegment1TestAsync()
        {
            try
            {
                IsProcessing = true;
                StatusMessage = "Running Segment 1 Integration Tests...";
                LogMessage("=== SEGMENT 1 INTEGRATION TEST START ===");

                await Task.Run(() =>
                {
                    try
                    {
                        var success = CitrixAI.Vision.Tests.Segment1IntegrationTest.RunTests(
                            message => Application.Current.Dispatcher.Invoke(() => LogMessage(message))
                        );

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            LogMessage($"=== SEGMENT 1 TEST RESULT: {(success ? "PASSED" : "FAILED")} ===");
                            StatusMessage = success ?
                                "Segment 1 tests completed successfully" :
                                "Segment 1 tests failed - check log for details";
                        });
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            LogError($"Segment 1 test execution failed: {ex.Message}");
                            StatusMessage = "Segment 1 test execution failed";
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                LogError($"Failed to start Segment 1 tests: {ex.Message}");
                StatusMessage = "Failed to start tests";
            }
            finally
            {
                IsProcessing = false;
            }
        }
        private async Task OpenImageAsync()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Select Image File",
                    Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All Files|*.*",
                    CheckFileExists = true
                };

                if (dialog.ShowDialog() == true)
                {
                    await LoadImageAsync(dialog.FileName);
                    LogMessage($"Image loaded: {dialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to open image: {ex.Message}");
            }
        }

        private async Task CaptureScreenshotAsync()
        {
            try
            {
                StatusMessage = "Capturing screenshot...";
                IsProcessing = true;

                // Hide window temporarily
                Application.Current.MainWindow.WindowState = WindowState.Minimized;
                await Task.Delay(500); // Give time for window to minimize

                using (var screenshotData = _screenshotCapture.CaptureWithMetadata())
                {
                    var tempPath = Path.Combine(Path.GetTempPath(), $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                    screenshotData.Image.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);

                    await LoadImageAsync(tempPath);
                    CurrentImagePath = "Screenshot";

                    LogMessage($"Screenshot captured: {screenshotData.Metadata.ScreenResolution.Width}x{screenshotData.Metadata.ScreenResolution.Height}");
                }

                // Restore window
                Application.Current.MainWindow.WindowState = WindowState.Normal;
                Application.Current.MainWindow.Activate();
            }
            catch (Exception ex)
            {
                LogError($"Failed to capture screenshot: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
                StatusMessage = "Screenshot captured";
            }
        }

        private async Task RunDetectionAsync()
        {
            if (CurrentImage == null)
            {
                LogMessage("No image loaded. Please load an image or capture a screenshot first.");
                return;
            }

            try
            {
                IsProcessing = true;
                StatusMessage = "Running advanced element detection...";

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Convert BitmapImage to Bitmap for detection
                var bitmap = ConvertBitmapImageToBitmap(CurrentImage);

                // Create detection context
                var context = DetectionContext.ForAIDetection(
                    bitmap,
                    new[] { ElementType.Button, ElementType.TextBox, ElementType.Label, ElementType.Dropdown },
                    ConfidenceThreshold);

                // Choose orchestrator based on image size and complexity
                var useParallelProcessing = ShouldUseParallelProcessing(bitmap);
                IDetectionResult result;

                if (useParallelProcessing && _parallelOrchestrator != null)
                {
                    LogMessage("Using parallel detection orchestrator");
                    result = await _parallelOrchestrator.DetectElementsAsync(context);
                }
                else
                {
                    LogMessage("Using standard detection orchestrator");
                    result = await _detectionOrchestrator.DetectElementsAsync(context);
                }

                stopwatch.Stop();
                DetectionTime = stopwatch.Elapsed.TotalMilliseconds;

                // Enhance results with classification if available
                var enhancedResult = await EnhanceWithClassification(result, context);

                // Process results
                ProcessDetectionResult(enhancedResult);

                // Update cache statistics
                UpdateCacheStatistics();

                LogMessage($"Advanced detection completed in {DetectionTime:F0}ms");
                LogMessage($"Found {enhancedResult.DetectedElements.Count} elements with {enhancedResult.OverallConfidence:F2} confidence");

                if (_advancedCache != null)
                {
                    LogMessage($"Cache hit ratio: {CacheHitRatio:P1}, Classification accuracy: {ClassificationAccuracy:P1}");
                }

                StatusMessage = $"Detection completed - {enhancedResult.DetectedElements.Count} elements found";
            }
            catch (Exception ex)
            {
                LogError($"Advanced detection failed: {ex.Message}");
                StatusMessage = "Detection failed";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private bool ShouldUseParallelProcessing(Bitmap image)
        {
            if (_parallelOrchestrator == null) return false;

            var imageArea = image.Width * image.Height;
            var availableStrategies = _parallelOrchestrator.GetRegisteredStrategies().Count;

            // Use parallel processing for larger images or when multiple strategies are available
            return availableStrategies >= 2 &&
                   (imageArea > 500000 || availableStrategies >= 3) &&
                   !ResourceMonitor.IsSystemUnderLoad();
        }

        private async Task<IDetectionResult> EnhanceWithClassification(IDetectionResult result, IDetectionContext context)
        {
            if (!result.IsSuccessful || result.DetectedElements.Count == 0 || _elementClassifier == null)
                return result;

            try
            {
                var classifiedElements = new List<IElementInfo>();
                var classificationSuccessCount = 0;

                foreach (var element in result.DetectedElements)
                {
                    try
                    {
                        var classificationResult = await _elementClassifier.ClassifyAsync(element, context);

                        if (classificationResult.IsSuccessful && classificationResult.Confidence >= 0.6)
                        {
                            // Create enhanced element with classification
                            var enhancedElement = new ElementInfo(
                                element.BoundingBox,
                                classificationResult.ClassifiedType,
                                element.Confidence,
                                element.Text,
                                CombineProperties(element.Properties, classificationResult.Properties),
                                element.Features,
                                classificationResult.Confidence,
                                element.Parent,
                                element.Children);

                            classifiedElements.Add(enhancedElement);
                            classificationSuccessCount++;
                        }
                        else
                        {
                            classifiedElements.Add(element); // Keep original if classification failed
                        }
                    }
                    catch
                    {
                        classifiedElements.Add(element); // Keep original on error
                    }
                }

                // Update classification accuracy
                ClassificationAccuracy = result.DetectedElements.Count > 0 ?
                    (double)classificationSuccessCount / result.DetectedElements.Count : 0.0;

                // Create enhanced result
                var enhancedMetadata = new Dictionary<string, object>(result.Metadata)
                {
                    ["ClassificationEnabled"] = true,
                    ["ClassificationAccuracy"] = ClassificationAccuracy,
                    ["ClassifiedElements"] = classificationSuccessCount
                };

                return new DetectionResult(
                    result.StrategyId + "_Enhanced",
                    classifiedElements,
                    result.OverallConfidence,
                    result.ProcessingTime,
                    enhancedMetadata,
                    result.Warnings,
                    result.ImageQuality);
            }
            catch (Exception ex)
            {
                LogMessage($"Classification enhancement failed: {ex.Message}");
                return result; // Return original result if enhancement fails
            }
        }

        public async Task UpdateAdvancedMetricsAsync()
        {
            try
            {
                UpdateCacheStatistics();
                OnPropertyChanged(nameof(SystemResourceStatus));
                OnPropertyChanged(nameof(PerformanceGrade));

                if (_performanceMonitor != null)
                    await _performanceMonitor.UpdateMetricsAsync();
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to update advanced metrics: {ex.Message}");
            }
        }

        private Dictionary<string, object> CombineProperties(IDictionary<string, object> original,
    IDictionary<string, object> classification)
        {
            var combined = new Dictionary<string, object>(original);
            foreach (var prop in classification)
            {
                combined[$"Classification_{prop.Key}"] = prop.Value;
            }
            return combined;
        }

        private void UpdateCacheStatistics()
        {
            try
            {
                if (_advancedCache != null)
                {
                    var stats = _advancedCache.GetStatistics();
                    CacheHitRatio = stats.HitRatio;
                    CacheEntries = stats.TotalEntries;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to update cache statistics: {ex.Message}");
            }
        }

        private async Task RunTemplateMatchingAsync()
        {
            LogMessage("Template matching strategy executed (basic implementation for Day 1)");
            await RunDetectionAsync();
        }

        private async Task RunAIDetectionAsync()
        {
            if (CurrentImage == null)
            {
                LogMessage("No image loaded. Please load an image or capture a screenshot first.");
                return;
            }

            try
            {
                IsProcessing = true;
                StatusMessage = "Running AI detection...";

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Get AI strategy specifically
                var aiStrategy = _detectionOrchestrator.GetRegisteredStrategies()
                    .FirstOrDefault(s => s.StrategyId == "AI_Detection");

                if (aiStrategy == null)
                {
                    LogMessage("AI Detection strategy not available");
                    StatusMessage = "AI Detection not available";
                    return;
                }

                // Convert BitmapImage to Bitmap for detection
                var bitmap = ConvertBitmapImageToBitmap(CurrentImage);

                // Create AI-specific detection context
                var context = DetectionContext.ForAIDetection(
                    bitmap,
                    new[] { ElementType.Button, ElementType.TextBox, ElementType.Label, ElementType.Dropdown },
                    ConfidenceThreshold);

                // Run AI detection only
                var result = await aiStrategy.DetectAsync(context);

                stopwatch.Stop();
                DetectionTime = stopwatch.Elapsed.TotalMilliseconds;

                // Process results
                ProcessDetectionResult(result);

                LogMessage($"AI Detection completed in {DetectionTime:F0}ms");
                LogMessage($"Found {result.DetectedElements.Count} elements with confidence {result.OverallConfidence:F2}");

                StatusMessage = $"AI Detection completed - {result.DetectedElements.Count} elements found";
            }
            catch (Exception ex)
            {
                LogError($"AI Detection failed: {ex.Message}");
                StatusMessage = "AI Detection failed";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task RunAllStrategiesAsync()
        {
            await RunDetectionAsync();
        }

        private void ClearResults()
        {
            DetectionResults.Clear();
            DetectedElements.Clear();
            LogMessage("Results cleared");
            StatusMessage = "Results cleared";
        }

        private void OpenMockGenerator()
        {
            try
            {
                var mockWindow = new MockCitrixWindow();
                mockWindow.Show();
                LogMessage("Mock Citrix generator opened");
            }
            catch (Exception ex)
            {
                LogError($"Failed to open mock generator: {ex.Message}");
            }
        }

        private void OpenAnnotationTool()
        {
            if (CurrentImage == null)
            {
                MessageBox.Show("Please load an image first.", "No Image", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var annotationWindow = new AnnotationWindow(CurrentImage);
                annotationWindow.Show();
                LogMessage("Annotation tool opened");
            }
            catch (Exception ex)
            {
                LogError($"Failed to open annotation tool: {ex.Message}");
            }
        }

        private void OpenPerformanceMonitor()
        {
            try
            {
                LogMessage("Performance monitor will be implemented in the next iteration");
                // TODO: Implement PerformanceMonitorWindow
                // var performanceWindow = new PerformanceMonitorWindow
                // {
                //     DataContext = _performanceMonitor
                // };
                // performanceWindow.Show();
                // LogMessage("Performance monitor opened");
            }
            catch (Exception ex)
            {
                LogError($"Failed to open performance monitor: {ex.Message}");
            }
        }

        private void ShowAbout()
        {
            var aboutText = $"CitrixAI Element Detection POC\n" +
                           $"Version 1.0.0\n" +
                           $"Day 1 Implementation\n\n" +
                           $"Features:\n" +
                           $"• Screenshot capture\n" +
                           $"• Mock Citrix UI generator\n" +
                           $"• Basic template matching\n" +
                           $"• Extensible architecture\n\n" +
                           $"Built with .NET Framework 4.8 and OpenCV";

            MessageBox.Show(aboutText, "About CitrixAI POC", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool CanRunDetection()
        {
            return CurrentImage != null && !IsProcessing;
        }

        #endregion

        #region Public Methods

        public void HandleImageClick(double x, double y)
        {
            LogMessage($"Image clicked at position: ({x:F0}, {y:F0})");

            // Find if click is on any detected element
            var clickedElement = DetectedElements.FirstOrDefault(e =>
                x >= e.BoundingBox.X && x <= e.BoundingBox.X + e.BoundingBox.Width &&
                y >= e.BoundingBox.Y && y <= e.BoundingBox.Y + e.BoundingBox.Height);

            if (clickedElement != null)
            {
                LogMessage($"Clicked on element: {clickedElement.ElementType} - {clickedElement.Text}");
                SelectedResult = DetectionResults.FirstOrDefault(r => r.ElementId == clickedElement.ElementId);
            }
        }

        #endregion

        #region Private Methods

        private void InitializeDetectionSystem()
        {
            LogMessage("Starting advanced detection system initialization...");
            try
            {
                // Initialize advanced cache
                _advancedCache = new AdvancedDetectionCache(maxEntries: 100, similarityThreshold: 0.85);
                LogMessage("Advanced detection cache initialized with perceptual hashing");

                // Initialize element classifier
                _elementClassifier = new BasicElementClassifier(confidenceThreshold: 0.6);
                LogMessage("Basic element classifier initialized");

                // Initialize parallel orchestrator with advanced cache
                _parallelOrchestrator = new ParallelDetectionOrchestrator(
                    resultAggregator: new WeightedConsensusAggregator(),
                    cache: _advancedCache,
                    maxParallelism: 0 // Auto-detect optimal parallelism
                );

                // Also keep the basic orchestrator for comparison
                _detectionOrchestrator = new DetectionOrchestrator(cache: _advancedCache);
                _screenshotCapture = new ScreenshotCapture();

                // Initialize performance monitoring
                _performanceMonitor = new PerformanceMonitorViewModel(_advancedCache);

                // Register strategies with both orchestrators
                RegisterDetectionStrategies();

                LogMessage($"Advanced detection system initialized with {_parallelOrchestrator.MaxParallelism} max parallelism");
                LogMessage("Resource utilization monitoring enabled");
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize advanced detection system: {ex.Message}");
                // Fallback to basic system
                InitializeBasicDetectionSystemFallback();
            }
        }

        private void RegisterDetectionStrategies()
        {
            LogMessage("Registering detection strategies...");
            try
            {
                // Register AI Detection Strategy
                var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "ui_detection.onnx");
                var aiStrategy = new AIDetectionStrategy(modelPath, 0.5, 0.4);

                _detectionOrchestrator.RegisterStrategy(aiStrategy);
                _parallelOrchestrator.RegisterStrategy(aiStrategy);
                LogMessage("AI Detection Strategy registered");

                // Register Template Matching Strategy
                var templateStrategy = new TemplateMatchingStrategy();
                _detectionOrchestrator.RegisterStrategy(templateStrategy);
                _parallelOrchestrator.RegisterStrategy(templateStrategy);
                LogMessage("Template Matching Strategy registered");

                LogMessage($"Total strategies registered: {_detectionOrchestrator.StrategyCount}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to register strategies: {ex.Message}");
            }
        }

        private void InitializeBasicDetectionSystemFallback()
        {
            try
            {
                _detectionOrchestrator = new DetectionOrchestrator();
                var fallbackStrategy = new TemplateMatchingStrategy();
                _detectionOrchestrator.RegisterStrategy(fallbackStrategy);
                LogMessage("Fallback: Initialized with template matching only");
            }
            catch (Exception fallbackEx)
            {
                LogError($"Fallback initialization failed: {fallbackEx.Message}");
            }
        }

        private void InitializeCollections()
        {
            DetectionResults = new ObservableCollection<DetectionResultViewModel>();
            DetectedElements = new ObservableCollection<DetectedElementViewModel>();
        }

        private async Task LoadImageAsync(string imagePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;

                // Scale large images to fit better in the UI (max 1000x700)
                using (var tempImage = new Bitmap(imagePath))
                {
                    if (tempImage.Width > 1000 || tempImage.Height > 700)
                    {
                        // Calculate scale to fit in reasonable display size
                        double scaleX = 1000.0 / tempImage.Width;
                        double scaleY = 700.0 / tempImage.Height;
                        double scale = Math.Min(scaleX, scaleY);

                        bitmap.DecodePixelWidth = (int)(tempImage.Width * scale);
                        bitmap.DecodePixelHeight = (int)(tempImage.Height * scale);

                        LogMessage($"Scaled large image from {tempImage.Width}x{tempImage.Height} to {bitmap.DecodePixelWidth}x{bitmap.DecodePixelHeight}");
                    }
                }

                bitmap.EndInit();
                bitmap.Freeze();

                CurrentImage = bitmap;
                CurrentImagePath = Path.GetFileName(imagePath);

                // Calculate image quality using original size
                using (var systemBitmap = new Bitmap(imagePath))
                {
                    ImageQuality = Math.Min(1.0, (systemBitmap.Width * systemBitmap.Height) / (800.0 * 600.0));
                }

                ClearResults();
                StatusMessage = $"Image loaded: {CurrentImagePath}";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load image: {ex.Message}", ex);
            }
        }

        private void ProcessDetectionResult(IDetectionResult result)
        {
            DetectionResults.Clear();
            DetectedElements.Clear();

            if (result == null || !result.IsSuccessful)
            {
                LogMessage("Detection failed or returned no results");
                return;
            }

            foreach (var element in result.DetectedElements)
            {
                var resultViewModel = new DetectionResultViewModel(element);
                DetectionResults.Add(resultViewModel);

                var elementViewModel = new DetectedElementViewModel(element);
                DetectedElements.Add(elementViewModel);
            }

            ElementsFound = result.DetectedElements.Count;
            ImageQuality = result.ImageQuality;

            // Log warnings if any
            foreach (var warning in result.Warnings)
            {
                LogMessage($"Warning: {warning}");
            }
        }

        private Bitmap ConvertBitmapImageToBitmap(BitmapImage bitmapImage)
        {
            using (var outStream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                encoder.Save(outStream);

                var bitmap = new Bitmap(outStream);
                return new Bitmap(bitmap); // Create a copy to avoid stream disposal issues
            }
        }

        private void LogMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var memoryUsage = MemoryManager.GetCurrentMemoryUsage();
            LogOutput += $"[{timestamp}] {message} (Mem: {memoryUsage}MB)\n";
            OnPropertyChanged(nameof(LogOutput));
        }

        private void LogError(string error)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            LogOutput += $"[{timestamp}] ERROR: {error}\n";
            OnPropertyChanged(nameof(LogOutput));
        }

        #endregion

        #region INotifyPropertyChanged Implementation

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

        #region IDisposable Implementation

        public void Dispose()
        {
            _detectionOrchestrator?.Dispose();
            _parallelOrchestrator?.Dispose();
            _screenshotCapture?.Dispose();
            _advancedCache?.Dispose();
            _performanceMonitor?.Dispose();
        }

        #endregion
    }
}