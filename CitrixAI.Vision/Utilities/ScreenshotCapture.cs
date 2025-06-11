using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CitrixAI.Vision.Utilities
{
    /// <summary>
    /// Provides screenshot capture functionality with various options.
    /// Implements the Single Responsibility Principle for screen capture operations.
    /// </summary>
    public sealed class ScreenshotCapture : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Captures a screenshot of the entire primary screen.
        /// </summary>
        /// <returns>Bitmap containing the screenshot.</returns>
        public Bitmap CaptureScreen()
        {
            return CaptureScreen(Screen.PrimaryScreen.Bounds);
        }

        /// <summary>
        /// Captures a screenshot of the specified screen region.
        /// </summary>
        /// <param name="bounds">The bounds of the region to capture.</param>
        /// <returns>Bitmap containing the screenshot.</returns>
        public Bitmap CaptureScreen(Rectangle bounds)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                throw new ArgumentException("Bounds must have positive width and height.", nameof(bounds));

            try
            {
                var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to capture screenshot: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Captures a screenshot of the specified window.
        /// </summary>
        /// <param name="windowHandle">Handle to the window to capture.</param>
        /// <returns>Bitmap containing the window screenshot.</returns>
        public Bitmap CaptureWindow(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
                throw new ArgumentException("Invalid window handle.", nameof(windowHandle));

            try
            {
                var windowRect = GetWindowRect(windowHandle);
                if (windowRect.IsEmpty)
                    throw new InvalidOperationException("Could not get window bounds.");

                return CaptureScreen(windowRect);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to capture window: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Captures a screenshot with metadata about the capture environment.
        /// </summary>
        /// <param name="bounds">The bounds of the region to capture.</param>
        /// <returns>Screenshot with capture metadata.</returns>
        public ScreenshotData CaptureWithMetadata(Rectangle? bounds = null)
        {
            var captureBounds = bounds ?? Screen.PrimaryScreen.Bounds;
            var timestamp = DateTime.UtcNow;

            var bitmap = CaptureScreen(captureBounds);

            var metadata = new ScreenshotMetadata
            {
                CaptureTime = timestamp,
                Bounds = captureBounds,
                ScreenResolution = Screen.PrimaryScreen.Bounds.Size,
                DpiX = GetSystemDpiX(),
                DpiY = GetSystemDpiY(),
                ColorDepth = Screen.PrimaryScreen.BitsPerPixel,
                Platform = Environment.OSVersion.Platform.ToString()
            };

            return new ScreenshotData(bitmap, metadata);
        }

        /// <summary>
        /// Gets the bounds of the specified window.
        /// </summary>
        /// <param name="windowHandle">Handle to the window.</param>
        /// <returns>Rectangle representing the window bounds.</returns>
        private Rectangle GetWindowRect(IntPtr windowHandle)
        {
            if (GetWindowRect(windowHandle, out RECT rect))
            {
                return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            }
            return Rectangle.Empty;
        }

        /// <summary>
        /// Gets the system DPI for X axis.
        /// </summary>
        /// <returns>DPI value for X axis.</returns>
        private float GetSystemDpiX()
        {
            using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                return graphics.DpiX;
            }
        }

        /// <summary>
        /// Gets the system DPI for Y axis.
        /// </summary>
        /// <returns>DPI value for Y axis.</returns>
        private float GetSystemDpiY()
        {
            using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                return graphics.DpiY;
            }
        }

        /// <summary>
        /// Releases resources used by the ScreenshotCapture.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        #region Windows API

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        #endregion
    }

    /// <summary>
    /// Contains screenshot data and metadata.
    /// </summary>
    public sealed class ScreenshotData : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the ScreenshotData class.
        /// </summary>
        /// <param name="image">The captured image.</param>
        /// <param name="metadata">The capture metadata.</param>
        public ScreenshotData(Bitmap image, ScreenshotMetadata metadata)
        {
            Image = image ?? throw new ArgumentNullException(nameof(image));
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }

        /// <summary>
        /// Gets the captured image.
        /// </summary>
        public Bitmap Image { get; }

        /// <summary>
        /// Gets the capture metadata.
        /// </summary>
        public ScreenshotMetadata Metadata { get; }

        /// <summary>
        /// Releases resources used by the ScreenshotData.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Image?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Contains metadata about a screenshot capture.
    /// </summary>
    public sealed class ScreenshotMetadata
    {
        /// <summary>
        /// Gets or sets the time when the screenshot was captured.
        /// </summary>
        public DateTime CaptureTime { get; set; }

        /// <summary>
        /// Gets or sets the bounds of the captured region.
        /// </summary>
        public Rectangle Bounds { get; set; }

        /// <summary>
        /// Gets or sets the screen resolution.
        /// </summary>
        public Size ScreenResolution { get; set; }

        /// <summary>
        /// Gets or sets the DPI for X axis.
        /// </summary>
        public float DpiX { get; set; }

        /// <summary>
        /// Gets or sets the DPI for Y axis.
        /// </summary>
        public float DpiY { get; set; }

        /// <summary>
        /// Gets or sets the color depth.
        /// </summary>
        public int ColorDepth { get; set; }

        /// <summary>
        /// Gets or sets the platform identifier.
        /// </summary>
        public string Platform { get; set; }
    }
}