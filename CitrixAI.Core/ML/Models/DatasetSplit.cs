using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitrixAI.Core.ML.Models
{
    /// <summary>
    /// Results of train/validation dataset splitting operations.
    /// Maintains element type distribution across splits.
    /// </summary>
    public class DatasetSplit
    {
        public Dataset OriginalDataset { get; set; }
        public IList<TrainingSample> TrainingSamples { get; set; }
        public IList<TrainingSample> ValidationSamples { get; set; }
        public double SplitRatio { get; set; }
        public int TrainingCount { get; set; }
        public int ValidationCount { get; set; }

        public DatasetSplit()
        {
            TrainingSamples = new List<TrainingSample>();
            ValidationSamples = new List<TrainingSample>();
        }
    }
}
