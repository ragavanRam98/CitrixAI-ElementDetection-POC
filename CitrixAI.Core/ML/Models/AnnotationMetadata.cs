using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitrixAI.Core.ML.Models
{
    /// <summary>
    /// Metadata about annotation source and format.
    /// Tracks annotation file information and parsing details.
    /// </summary>
    public class AnnotationMetadata
    {
        public DateTime CreatedDate { get; set; }
        public DateTime LastModified { get; set; }
        public string AnnotationTool { get; set; }
        public string Version { get; set; }
        public Dictionary<string, object> FormatSpecificData { get; set; }

        public AnnotationMetadata()
        {
            FormatSpecificData = new Dictionary<string, object>();
        }
    }
}
