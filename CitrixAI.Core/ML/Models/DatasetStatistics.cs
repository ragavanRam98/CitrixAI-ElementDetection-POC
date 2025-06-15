using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitrixAI.Core.Interfaces;

namespace CitrixAI.Core.ML.Models
{
    /// <summary>
    /// Comprehensive statistical analysis of dataset composition.
    /// Provides insights for training strategy optimization.
    /// </summary>
    public class DatasetStatistics
    {
        public int TotalSamples { get; set; }
        public int TotalAnnotations { get; set; }
        public Dictionary<ElementType, int> ElementTypeDistribution { get; set; }
        public Dictionary<string, int> SizeDistribution { get; set; }
        public double AverageQualityScore { get; set; }
        public QualityRange QualityRange { get; set; }

        public DatasetStatistics()
        {
            ElementTypeDistribution = new Dictionary<ElementType, int>();
            SizeDistribution = new Dictionary<string, int>();
        }
    }
}
