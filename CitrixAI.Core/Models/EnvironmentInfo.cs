using CitrixAI.Core.Interfaces;
using System.Collections.Generic;
using System.Drawing;

namespace CitrixAI.Core.Models
{
    /// <summary>
    /// Implementation of IEnvironmentInfo providing environment details.
    /// </summary>
    public class EnvironmentInfo : IEnvironmentInfo
    {
        private readonly Dictionary<string, object> _properties;

        /// <summary>
        /// Initializes a new instance of the EnvironmentInfo class.
        /// </summary>
        public EnvironmentInfo()
        {
            _properties = new Dictionary<string, object>();
            ColorDepth = 32;
            Platform = "Unknown";
        }

        /// <inheritdoc />
        public Size ScreenResolution { get; set; }

        /// <inheritdoc />
        public float DpiX { get; set; }

        /// <inheritdoc />
        public float DpiY { get; set; }

        /// <inheritdoc />
        public string Platform { get; set; }

        /// <inheritdoc />
        public int ColorDepth { get; set; }

        /// <inheritdoc />
        public IDictionary<string, object> Properties => new Dictionary<string, object>(_properties);

        /// <summary>
        /// Sets an environment property.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="value">The property value.</param>
        public void SetProperty(string key, object value)
        {
            _properties[key] = value;
        }
    }
}