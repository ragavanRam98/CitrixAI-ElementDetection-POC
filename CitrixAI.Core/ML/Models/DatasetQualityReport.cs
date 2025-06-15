using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitrixAI.Core.Interfaces;

namespace CitrixAI.Core.ML.Models
{
    /// <summary>
    /// Comprehensive quality analysis report for dataset assessment.
    /// Provides insights into data quality and improvement recommendations.
    /// </summary>
    public class DatasetQualityReport
    {
        public int TotalSamples { get; set; }
        public double AverageQuality { get; set; }
        public double MinQuality { get; set; }
        public double MaxQuality { get; set; }
        public Dictionary<ElementType, int> ElementTypeDistribution { get; set; }
        public Dictionary<ElementType, double> QualityByElementType { get; set; }
        public IList<string> Recommendations { get; set; }

        public DatasetQualityReport()
        {
            ElementTypeDistribution = new Dictionary<ElementType, int>();
            QualityByElementType = new Dictionary<ElementType, double>();
            Recommendations = new List<string>();
        }
    }
}
