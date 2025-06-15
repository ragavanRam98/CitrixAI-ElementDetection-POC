using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitrixAI.Core.ML.Models
{
    /// <summary>
    /// Metadata about individual training samples.
    /// Tracks sample-level information and processing history.
    /// </summary>
    public class SampleMetadata
    {
        public string OriginalPath { get; set; }
        public DateTime CreatedDate { get; set; }
        public System.Drawing.Size OriginalSize { get; set; }
        public string ImageFormat { get; set; }
        public Dictionary<string, object> ProcessingHistory { get; set; }

        public SampleMetadata()
        {
            ProcessingHistory = new Dictionary<string, object>();
        }
    }
}
