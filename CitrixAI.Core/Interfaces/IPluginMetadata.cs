using System;
using System.Collections.Generic;

namespace CitrixAI.Core.Interfaces
{
    /// <summary>
    /// Provides metadata information about a plugin.
    /// Contains version, capabilities, and configuration details.
    /// </summary>
    public interface IPluginMetadata
    {
        /// <summary>
        /// Gets the unique identifier for the plugin.
        /// </summary>
        string PluginId { get; }

        /// <summary>
        /// Gets the human-readable name of the plugin.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the plugin version.
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// Gets the plugin description.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the plugin author/vendor information.
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Gets the list of supported capabilities.
        /// </summary>
        IReadOnlyList<string> Capabilities { get; }

        /// <summary>
        /// Gets the minimum required platform version.
        /// </summary>
        Version MinimumPlatformVersion { get; }

        /// <summary>
        /// Gets configuration parameters required by the plugin.
        /// </summary>
        IDictionary<string, object> ConfigurationParameters { get; }

        /// <summary>
        /// Gets the plugin file path or location.
        /// </summary>
        string Location { get; }

        /// <summary>
        /// Gets a value indicating whether the plugin is enabled.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets additional metadata properties.
        /// </summary>
        IDictionary<string, object> AdditionalProperties { get; }
    }
}