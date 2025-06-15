using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitrixAI.Core.ML.Models
{
    public class Dataset
    {
        public string DatasetId { get; set; }
        public IList<TrainingSample> Samples { get; set; }
        public AnnotationData Annotations { get; set; }
        public DatasetMetadata Metadata { get; set; }
        public DatasetStatistics Statistics { get; set; }

        public Dataset()
        {
            Samples = new List<TrainingSample>();
        }
    }
}
