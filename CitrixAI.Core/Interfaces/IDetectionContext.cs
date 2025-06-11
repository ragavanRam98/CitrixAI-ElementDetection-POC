using System;
using System.Collections.Generic;
using System.Drawing;

namespace CitrixAI.Core.Interfaces
{
    /// <summary>
    /// Provides context for element detection operations.
    /// Contains the image, search criteria, and environment information.
    /// </summary>
    public interface IDetectionContext
    {
        /// <summary>
        /// Gets the unique identifier for this detection context.
        /// </summary>
        Guid ContextId { get; }

        /// <summary>
        /// Gets the image to search for elements in.
        /// </summary>
        Bitmap SourceImage { get; }

        /// <summary>
        /// Gets the search criteria for element detection.
        /// </summary>
        IElementSearchCriteria SearchCriteria { get; }

        /// <summary>
        /// Gets the environment information (resolution, DPI, etc.).
        /// </summary>
        IEnvironmentInfo EnvironmentInfo { get; }

        /// <summary>
        /// Gets the region of interest for detection (null for entire image).
        /// </summary>
        Rectangle? RegionOfInterest { get; }

        /// <summary>
        /// Gets additional metadata for the detection context.
        /// </summary>
        IDictionary<string, object> Metadata { get; }

        /// <summary>
        /// Gets the timeout for detection operations.
        /// </summary>
        TimeSpan Timeout { get; }

        /// <summary>
        /// Gets the minimum confidence threshold for detection results.
        /// </summary>
        double MinimumConfidence { get; }

        /// <summary>
        /// Gets the maximum number of elements to detect.
        /// </summary>
        int MaxResults { get; }

        /// <summary>
        /// Gets a value indicating whether to perform OCR on detected elements.
        /// </summary>
        bool EnableOCR { get; }

        /// <summary>
        /// Gets a value indicating whether to classify detected elements.
        /// </summary>
        bool EnableClassification { get; }
    }
}