using System.Collections.Generic;
using System.Drawing;

namespace CitrixAI.Core.Interfaces
{
    /// <summary>
    /// Provides information about the detection environment.
    /// </summary>
    public interface IEnvironmentInfo
    {
        /// <summary>
        /// Gets the screen resolution.
        /// </summary>
        Size ScreenResolution { get; }

        /// <summary>
        /// Gets the DPI setting for the X axis.
        /// </summary>
        float DpiX { get; }

        /// <summary>
        /// Gets the DPI setting for the Y axis.
        /// </summary>
        float DpiY { get; }

        /// <summary>
        /// Gets the platform identifier (e.g., "Citrix", "RDP", "Local").
        /// </summary>
        string Platform { get; }

        /// <summary>
        /// Gets the color depth of the display.
        /// </summary>
        int ColorDepth { get; }

        /// <summary>
        /// Gets additional environment properties.
        /// </summary>
        IDictionary<string, object> Properties { get; }
    }
}