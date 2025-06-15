using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitrixAI.Core.ML.Models
{
    /// <summary>
    /// Results of quality-based dataset filtering operations.
    /// Contains filtered samples and rejection statistics.
    /// </summary>
    public class FilteredDataset
    {
        public Dataset OriginalDataset { get; set; }
        public IList<TrainingSample> FilteredSamples { get; set; }
        public IList<TrainingSample> RejectedSamples { get; set; }
        public string FilterCriteria { get; set; }
        public int FilteredCount { get; set; }
        public int RejectedCount { get; set; }

        public FilteredDataset()
        {
            FilteredSamples = new List<TrainingSample>();
            RejectedSamples = new List<TrainingSample>();
        }
    }
}
