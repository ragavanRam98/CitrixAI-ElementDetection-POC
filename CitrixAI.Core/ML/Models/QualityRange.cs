using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitrixAI.Core.ML.Models
{
    /// <summary>
    /// Quality score range information with statistical measures.
    /// Provides quality distribution insights for dataset assessment.
    /// </summary>
    public class QualityRange
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public double StandardDeviation { get; set; }
    }
}
