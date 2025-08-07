namespace SacksAIPlatform.DataLayer.XlsConverter
{

    using SacksAIPlatform.DataLayer.Entities;
    using System;

    public interface IFiletoProductConverter
    {
        /// <summary>
        /// Converts CSV file to a list of Product entities using flexible configuration
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <param name="configuration">CSV parsing configuration, null uses default</param>
        /// <returns>List of converted Product entities with validation results</returns>
        Task<FileConversionResult> ConvertFileToProductsAsync(string filePath, FileConfiguration configuration);

        /// <summary>
        /// Converts CSV file to a list of Product entities using flexible configuration with interactive decision support
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <param name="configuration">CSV parsing configuration, null uses default</param>
        /// <param name="interactiveHandler">Handler for interactive user decisions when parser is uncertain</param>
        /// <returns>List of converted Product entities with validation results</returns>
        Task<FileConversionResult> ConvertFileToProductsInteractiveAsync(string filePath, FileConfiguration configuration, IInteractiveDecisionHandler interactiveHandler);
    }

    public class FileConversionResult
    {
        public List<Product> ValidProducts { get; set; } = new();
        public List<FileValidationError> ValidationErrors { get; set; } = new();
        public int TotalLinesProcessed { get; set; }
        public int EmptyLines { get; internal set; }
        public int InteractiveDecisions { get; set; }
        public int LearnedExamples { get; set; }

        internal void Clear()
        {
            ValidProducts.Clear();
            ValidationErrors.Clear();
            TotalLinesProcessed = 0;
            EmptyLines = 0;
            InteractiveDecisions = 0;
            LearnedExamples = 0;
        }
    }

    public class FileValidationError
    {
        public int RowNumber { get; set; }
        public string Field { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string RawLine { get; set; } = string.Empty;
    }
}