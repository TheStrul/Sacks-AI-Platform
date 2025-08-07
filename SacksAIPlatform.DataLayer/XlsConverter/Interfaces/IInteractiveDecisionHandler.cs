namespace SacksAIPlatform.DataLayer.XlsConverter
{
    using SacksAIPlatform.DataLayer.Entities;
    using SacksAIPlatform.DataLayer.Enums;
    using SacksAIPlatform.DataLayer.XlsConverter.Helpers;

    /// <summary>
    /// Interface for handling interactive user decisions during parsing
    /// Allows the parser to ask for user input when uncertain about parsing decisions
    /// </summary>
    public interface IInteractiveDecisionHandler
    {
        /// <summary>
        /// Asks the user to resolve brand ambiguity
        /// </summary>
        /// <param name="context">Context about the parsing decision</param>
        /// <param name="possibleBrands">List of possible brand matches</param>
        /// <returns>Selected brand ID or null to skip</returns>
        Task<int?> ResolveBrandAmbiguityAsync(InteractiveContext context, List<BrandOption> possibleBrands);

        /// <summary>
        /// Asks the user to resolve concentration ambiguity
        /// </summary>
        /// <param name="context">Context about the parsing decision</param>
        /// <param name="possibleConcentrations">List of possible concentration matches</param>
        /// <returns>Selected concentration or null to use default</returns>
        Task<Concentration?> ResolveConcentrationAmbiguityAsync(InteractiveContext context, List<ConcentrationOption> possibleConcentrations);

        /// <summary>
        /// Asks the user to resolve size parsing ambiguity
        /// </summary>
        /// <param name="context">Context about the parsing decision</param>
        /// <param name="possibleSizes">List of possible size interpretations</param>
        /// <returns>Selected size information or null to use default</returns>
        Task<SizeInfo?> ResolveSizeAmbiguityAsync(InteractiveContext context, List<SizeOption> possibleSizes);

        /// <summary>
        /// Asks the user to resolve general parsing ambiguity
        /// </summary>
        /// <param name="context">Context about the parsing decision</param>
        /// <param name="question">The question to ask the user</param>
        /// <param name="options">Available options</param>
        /// <returns>Selected option index or null to skip</returns>
        Task<int?> ResolveGeneralAmbiguityAsync(InteractiveContext context, string question, List<string> options);

        /// <summary>
        /// Asks if the user wants to learn from a parsing decision
        /// </summary>
        /// <param name="context">Context about the parsing decision</param>
        /// <param name="originalText">The original text that was parsed</param>
        /// <param name="detectedInfo">What the parser detected</param>
        /// <returns>True if the user wants to save this as a learning example</returns>
        Task<bool> ShouldLearnFromDecisionAsync(InteractiveContext context, string originalText, string detectedInfo);

        /// <summary>
        /// Allows the handler to access the runtime manager for learning
        /// </summary>
        /// <param name="runtimeManager">The runtime manager to update with learned patterns</param>
        void SetRuntimeManager(ProductParserRuntimeManager runtimeManager);
    }

    /// <summary>
    /// Context information for interactive decisions
    /// </summary>
    public class InteractiveContext
    {
        public int RowNumber { get; set; }
        public string OriginalText { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public Product CurrentProduct { get; set; } = new();
        public double ConfidenceLevel { get; set; }
        public string RawRowData { get; set; } = string.Empty;
    }

    /// <summary>
    /// Brand option for user selection
    /// </summary>
    public class BrandOption
    {
        public int BrandId { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public string ManufacturerName { get; set; } = string.Empty;
        public double MatchScore { get; set; }
        public string MatchReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Concentration option for user selection
    /// </summary>
    public class ConcentrationOption
    {
        public Concentration Concentration { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public double MatchScore { get; set; }
        public string MatchReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Size option for user selection
    /// </summary>
    public class SizeOption
    {
        public string Size { get; set; } = string.Empty;
        public Units Units { get; set; }
        public double MatchScore { get; set; }
        public string MatchReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Size information result
    /// </summary>
    public class SizeInfo
    {
        public string Size { get; set; } = string.Empty;
        public Units Units { get; set; }
    }
}