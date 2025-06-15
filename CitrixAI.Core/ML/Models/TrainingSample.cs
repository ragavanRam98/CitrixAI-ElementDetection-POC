using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitrixAI.Core.ML.Models
{
    /// <summary>
    /// Individual training sample containing image path and associated annotations.
    /// Represents a single data point in the training dataset.
    /// </summary>
    public class TrainingSample
    {
        public string SampleId { get; set; }
        public string ImagePath { get; set; }
        public IList<ElementAnnotation> Annotations { get; set; }
        public double QualityScore { get; set; }
        public SampleMetadata Metadata { get; set; }

        public TrainingSample()
        {
            Annotations = new List<ElementAnnotation>();
        }
    }
}
