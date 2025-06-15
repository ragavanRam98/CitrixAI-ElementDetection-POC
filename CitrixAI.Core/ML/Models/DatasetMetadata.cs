using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitrixAI.Core.ML.Models
{
    /// <summary>
    /// Metadata about dataset creation and management.
    /// Tracks dataset lifecycle and version information.
    /// </summary>
    public class DatasetMetadata
    {
        public DateTime CreatedDate { get; set; }
        public string SourcePath { get; set; }
        public string Version { get; set; }
        public string CreatedBy { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> CustomProperties { get; set; }

        public DatasetMetadata()
        {
            CustomProperties = new Dictionary<string, object>();
        }
    }
}
