using System.Data;
using System.Threading.Tasks;
using CitrixAI.Core.ML.Models;

namespace CitrixAI.Core.ML.Interfaces
{
    /// <summary>
    /// Contract for loading and validating training datasets from various sources.
    /// Provides standardized interface for different data source implementations.
    /// </summary>
    public interface ITrainingDataSource
    {
        /// <summary>
        /// Loads a complete dataset from the specified source path or identifier.
        /// Source can be a file path, database connection, or cloud storage location.
        /// </summary>
        /// <param name="source">Source identifier (path, URL, connection string)</param>
        /// <returns>Complete dataset with samples and metadata</returns>
        Task<Dataset> LoadDatasetAsync(string source);

        /// <summary>
        /// Validates the integrity and completeness of a loaded dataset.
        /// Checks for missing files, corrupt annotations, and data consistency.
        /// </summary>
        /// <param name="dataset">Dataset to validate</param>
        /// <returns>True if dataset is valid and ready for training</returns>
        Task<bool> ValidateDatasetAsync(Dataset dataset);

        /// <summary>
        /// Generates comprehensive statistics about the dataset composition.
        /// Provides insights for training strategy and data balance assessment.
        /// </summary>
        /// <param name="dataset">Dataset to analyze</param>
        /// <returns>Statistical analysis of dataset composition</returns>
        Task<DatasetStatistics> GetStatisticsAsync(Dataset dataset);
    }
}