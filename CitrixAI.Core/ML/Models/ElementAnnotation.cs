using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitrixAI.Core.Interfaces;

namespace CitrixAI.Core.ML.Models
{
    /// <summary>
    /// Individual element annotation with bounding box and classification data.
    /// Represents a single labeled UI element in training data.
    /// </summary>
    public class ElementAnnotation
    {
        public string AnnotationId { get; set; }
        public Rectangle BoundingBox { get; set; }
        public ElementType ElementType { get; set; }
        public string Text { get; set; }
        public double? Confidence { get; set; }
        public string SourceImage { get; set; }
        public Dictionary<string, object> Properties { get; set; }

        public ElementAnnotation()
        {
            Properties = new Dictionary<string, object>();
        }
    }

}
