using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitrixAI.Core.ML.Models
{
    /// <summary>
    /// Container for annotation data parsed from various annotation formats.
    /// Holds all element annotations for a dataset with format metadata.
    /// </summary>
    public class AnnotationData
    {
        public string SourceFile { get; set; }
        public string Format { get; set; }
        public IList<ElementAnnotation> Elements { get; set; }
        public AnnotationMetadata Metadata { get; set; }

        public AnnotationData()
        {
            Elements = new List<ElementAnnotation>();
        }
    }
}
