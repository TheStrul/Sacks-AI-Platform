namespace SacksAIPlatform.DataLayer.XlsConverter.Helpers
{
    using SacksAIPlatform.DataLayer.Enums;
    using SacksAIPlatform.DataLayer.XlsConverter;

    /// <summary>
    /// Console-based implementation of interactive decision handler
    /// Provides user prompts and input handling for parsing decisions
    /// </summary>
    public class ConsoleInteractiveDecisionHandler : IInteractiveDecisionHandler
    {
        private readonly bool _enableInteraction;
        private readonly double _confidenceThreshold;
        private ProductParserRuntimeManager? _runtimeManager;

        /// <summary>
        /// Initializes the console interactive decision handler
        /// </summary>
        /// <param name="enableInteraction">Whether to enable interactive prompts</param>
        /// <param name="confidenceThreshold">Confidence threshold below which to ask for user input (0.0 to 1.0)</param>
        public ConsoleInteractiveDecisionHandler(bool enableInteraction = true, double confidenceThreshold = 0.7)
        {
            _enableInteraction = enableInteraction;
            _confidenceThreshold = confidenceThreshold;
        }

        /// <summary>
        /// Sets the runtime manager for applying learned patterns
        /// </summary>
        public void SetRuntimeManager(ProductParserRuntimeManager runtimeManager)
        {
            _runtimeManager = runtimeManager;
        }

        public async Task<int?> ResolveBrandAmbiguityAsync(InteractiveContext context, List<BrandOption> possibleBrands)
        {
            if (!_enableInteraction || context.ConfidenceLevel >= _confidenceThreshold)
                return null;

            Console.WriteLine();
            Console.WriteLine("?? Brand Recognition Uncertainty");
            Console.WriteLine("=" + new string('=', 40));
            Console.WriteLine($"?? Row {context.RowNumber}: {context.OriginalText}");
            Console.WriteLine($"?? Field: {context.FieldName}");
            Console.WriteLine($"?? Confidence: {context.ConfidenceLevel:P1}");
            Console.WriteLine();

            if (possibleBrands.Count == 0)
            {
                Console.WriteLine("? No brand matches found. What should I do?");
                Console.WriteLine("1. Skip brand assignment (leave as 0)");
                Console.WriteLine("2. Enter brand ID manually");
                Console.WriteLine("0. Use default behavior");
                Console.Write("Choice (0-2): ");

                var choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        return 0;
                    case "2":
                        Console.Write("Enter brand ID: ");
                        if (int.TryParse(Console.ReadLine(), out var brandId))
                            return brandId;
                        break;
                }
                return null;
            }

            Console.WriteLine("??? Possible brand matches:");
            Console.WriteLine("0. Skip brand assignment");
            
            for (int i = 0; i < possibleBrands.Count; i++)
            {
                var brand = possibleBrands[i];
                Console.WriteLine($"{i + 1}. {brand.BrandName} (ID: {brand.BrandId}) - {brand.ManufacturerName}");
                Console.WriteLine($"    Score: {brand.MatchScore:P1} - {brand.MatchReason}");
            }

            Console.WriteLine($"{possibleBrands.Count + 1}. Enter different brand ID manually");
            Console.Write($"Select brand (0-{possibleBrands.Count + 1}): ");

            var selection = Console.ReadLine();
            if (int.TryParse(selection, out var index))
            {
                if (index == 0)
                    return 0;
                else if (index >= 1 && index <= possibleBrands.Count)
                    return possibleBrands[index - 1].BrandId;
                else if (index == possibleBrands.Count + 1)
                {
                    Console.Write("Enter brand ID: ");
                    if (int.TryParse(Console.ReadLine(), out var manualBrandId))
                        return manualBrandId;
                }
            }

            return null;
        }

        public async Task<Concentration?> ResolveConcentrationAmbiguityAsync(InteractiveContext context, List<ConcentrationOption> possibleConcentrations)
        {
            if (!_enableInteraction || context.ConfidenceLevel >= _confidenceThreshold)
                return null;

            Console.WriteLine();
            Console.WriteLine("?? Concentration Recognition Uncertainty");
            Console.WriteLine("=" + new string('=', 45));
            Console.WriteLine($"?? Row {context.RowNumber}: {context.OriginalText}");
            Console.WriteLine($"?? Field: {context.FieldName}");
            Console.WriteLine($"?? Confidence: {context.ConfidenceLevel:P1}");
            Console.WriteLine();

            if (possibleConcentrations.Count == 0)
            {
                Console.WriteLine("? No concentration matches found. Use default?");
                Console.WriteLine("1. Use EDT (default)");
                Console.WriteLine("2. Use EDP");
                Console.WriteLine("3. Use Parfum");
                Console.WriteLine("0. Skip (leave unknown)");
                Console.Write("Choice (0-3): ");

                var choice = Console.ReadLine();
                return choice switch
                {
                    "1" => Concentration.EDT,
                    "2" => Concentration.EDP,
                    "3" => Concentration.Parfum,
                    _ => null
                };
            }

            Console.WriteLine("?? Possible concentration matches:");
            Console.WriteLine("0. Skip (use default)");
            
            for (int i = 0; i < possibleConcentrations.Count; i++)
            {
                var conc = possibleConcentrations[i];
                Console.WriteLine($"{i + 1}. {conc.DisplayName}");
                Console.WriteLine($"    Score: {conc.MatchScore:P1} - {conc.MatchReason}");
            }

            Console.Write($"Select concentration (0-{possibleConcentrations.Count}): ");

            var selection = Console.ReadLine();
            if (int.TryParse(selection, out var index))
            {
                if (index == 0)
                    return null;
                else if (index >= 1 && index <= possibleConcentrations.Count)
                    return possibleConcentrations[index - 1].Concentration;
            }

            return null;
        }

        public async Task<SizeInfo?> ResolveSizeAmbiguityAsync(InteractiveContext context, List<SizeOption> possibleSizes)
        {
            if (!_enableInteraction || context.ConfidenceLevel >= _confidenceThreshold)
                return null;

            Console.WriteLine();
            Console.WriteLine("?? Size Recognition Uncertainty");
            Console.WriteLine("=" + new string('=', 35));
            Console.WriteLine($"?? Row {context.RowNumber}: {context.OriginalText}");
            Console.WriteLine($"?? Field: {context.FieldName}");
            Console.WriteLine($"?? Confidence: {context.ConfidenceLevel:P1}");
            Console.WriteLine();

            if (possibleSizes.Count == 0)
            {
                Console.WriteLine("? No size matches found. Enter manually?");
                Console.Write("Enter size (or press Enter to skip): ");
                var sizeInput = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(sizeInput))
                {
                    Console.WriteLine("Select units:");
                    Console.WriteLine("1. ml");
                    Console.WriteLine("2. oz");
                    Console.WriteLine("3. g");
                    Console.Write("Choice (1-3): ");
                    
                    var unitsChoice = Console.ReadLine();
                    var units = unitsChoice switch
                    {
                        "2" => Units.oz,
                        "3" => Units.g,
                        _ => Units.ml
                    };

                    return new SizeInfo { Size = sizeInput, Units = units };
                }
                return null;
            }

            Console.WriteLine("?? Possible size interpretations:");
            Console.WriteLine("0. Skip size assignment");
            
            for (int i = 0; i < possibleSizes.Count; i++)
            {
                var size = possibleSizes[i];
                Console.WriteLine($"{i + 1}. {size.Size} {size.Units}");
                Console.WriteLine($"    Score: {size.MatchScore:P1} - {size.MatchReason}");
            }

            Console.WriteLine($"{possibleSizes.Count + 1}. Enter size manually");
            Console.Write($"Select size (0-{possibleSizes.Count + 1}): ");

            var selection = Console.ReadLine();
            if (int.TryParse(selection, out var index))
            {
                if (index == 0)
                    return null;
                else if (index >= 1 && index <= possibleSizes.Count)
                {
                    var selected = possibleSizes[index - 1];
                    return new SizeInfo { Size = selected.Size, Units = selected.Units };
                }
                else if (index == possibleSizes.Count + 1)
                {
                    Console.Write("Enter size: ");
                    var sizeInput = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(sizeInput))
                    {
                        Console.WriteLine("Select units:");
                        Console.WriteLine("1. ml");
                        Console.WriteLine("2. oz");
                        Console.WriteLine("3. g");
                        Console.Write("Choice (1-3): ");
                        
                        var unitsChoice = Console.ReadLine();
                        var units = unitsChoice switch
                        {
                            "2" => Units.oz,
                            "3" => Units.g,
                            _ => Units.ml
                        };

                        return new SizeInfo { Size = sizeInput, Units = units };
                    }
                }
            }

            return null;
        }

        public async Task<int?> ResolveGeneralAmbiguityAsync(InteractiveContext context, string question, List<string> options)
        {
            if (!_enableInteraction || context.ConfidenceLevel >= _confidenceThreshold)
                return null;

            Console.WriteLine();
            Console.WriteLine("? Parser Decision Required");
            Console.WriteLine("=" + new string('=', 30));
            Console.WriteLine($"?? Row {context.RowNumber}: {context.OriginalText}");
            Console.WriteLine($"?? Field: {context.FieldName}");
            Console.WriteLine($"?? Confidence: {context.ConfidenceLevel:P1}");
            Console.WriteLine();
            Console.WriteLine(question);
            Console.WriteLine();

            for (int i = 0; i < options.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {options[i]}");
            }
            Console.WriteLine("0. Skip/Use default");

            Console.Write($"Select option (0-{options.Count}): ");

            var selection = Console.ReadLine();
            if (int.TryParse(selection, out var index))
            {
                if (index >= 0 && index <= options.Count)
                    return index;
            }

            return null;
        }

        public async Task<bool> ShouldLearnFromDecisionAsync(InteractiveContext context, string originalText, string detectedInfo)
        {
            if (!_enableInteraction)
                return false;

            // Show comprehensive parsing context in the requested format
            Console.WriteLine();
            Console.WriteLine("?? Parser Learning Opportunity");
            Console.WriteLine("=" + new string('=', 40));
            
            // Parse the raw row to show individual cells
            var cells = context.RawRowData.Split(',').Select(c => c.Trim().Trim('"')).ToArray();
            Console.WriteLine($"Original row ({context.RowNumber}): {string.Join(" | ", cells)}");
            Console.WriteLine($"Unsure about: {context.OriginalText}");
            
            // Show already detected properties
            var detectedProperties = new List<string>();
            var undetectedProperties = new List<string>();
            
            var product = context.CurrentProduct;
            
            // Check what's already detected
            if (product.BrandID != 0)
                detectedProperties.Add($"Brand = {product.BrandID}");
            else
                undetectedProperties.Add("Brand");
                
            if (product.Concentration != Concentration.Unknown)
                detectedProperties.Add($"Concentration = {product.Concentration}");
            else
                undetectedProperties.Add("Concentration");
                
            if (product.Type != PerfumeType.Spray)
                detectedProperties.Add($"Type = {product.Type}");
            else if (!context.OriginalText.ToLowerInvariant().Contains("spray"))
                undetectedProperties.Add("Type");
                
            if (product.Gender != Gender.Unisex)
                detectedProperties.Add($"Gender = {product.Gender}");
            else
                undetectedProperties.Add("Gender");
                
            if (!string.IsNullOrEmpty(product.Size) && product.Size != "0")
                detectedProperties.Add($"Size = {product.Size} {product.Units}");
            else
                undetectedProperties.Add("Size");
                
            if (!string.IsNullOrEmpty(product.Name))
                detectedProperties.Add($"Name = {product.Name}");
            else
                undetectedProperties.Add("Name");
                
            if (!string.IsNullOrEmpty(product.Code))
                detectedProperties.Add($"Code = {product.Code}");
            else
                undetectedProperties.Add("Code");

            Console.WriteLine($"Already detected: {(detectedProperties.Count > 0 ? string.Join(", ", detectedProperties) : "None")}<wbr>");
            Console.WriteLine($"Undetected properties: {(undetectedProperties.Count > 0 ? string.Join(", ", undetectedProperties) : "All detected")}");
            Console.WriteLine();
            
            Console.Write("Would you like to teach the parser what to learn from this text? (y/N): ");
            var response = Console.ReadLine()?.ToLowerInvariant();
            
            if (response != "y" && response != "yes")
                return false;

            // Now ask the user to explain what to learn
            return await CaptureUserLearningExplanation(context, originalText);
        }

        /// <summary>
        /// Captures detailed learning explanation from the user
        /// </summary>
        private async Task<bool> CaptureUserLearningExplanation(InteractiveContext context, string originalText)
        {
            Console.WriteLine();
            Console.WriteLine("?? Teaching the Parser");
            Console.WriteLine("=" + new string('=', 25));
            Console.WriteLine($"Text to analyze: \"{originalText}\"");
            Console.WriteLine();
            Console.WriteLine("Please explain what the parser should learn from this text.");
            Console.WriteLine("Examples:");
            Console.WriteLine("• \"Each time you see '(M)' you can treat this as Gender=Male\"");
            Console.WriteLine("• \"When you see 'INTENSE' it usually means Concentration=Parfum\"");
            Console.WriteLine("• \"'ATOMIZER' should be recognized as Type=Spray\"");
            Console.WriteLine("• \"Brand names like 'CH' or 'C.H.' refer to Brand=1 (Chanel)\"");
            Console.WriteLine();
            
            var learningRules = new List<string>();
            
            while (true)
            {
                Console.Write("Enter learning rule (or press Enter to finish): ");
                var rule = Console.ReadLine()?.Trim();
                
                if (string.IsNullOrEmpty(rule))
                    break;
                    
                learningRules.Add(rule);
                Console.WriteLine($"? Added rule: {rule}");
            }
            
            if (learningRules.Count == 0)
            {
                Console.WriteLine("? No learning rules provided.");
                return false;
            }
            
            // Parse and apply the learning rules
            Console.WriteLine($"\n?? Processing {learningRules.Count} learning rules...");
            
            foreach (var rule in learningRules)
            {
                var success = await ProcessLearningRule(rule, originalText, context);
                if (success)
                {
                    Console.WriteLine($"? Successfully processed: {rule}");
                }
                else
                {
                    Console.WriteLine($"?? Could not process: {rule}");
                }
            }
            
            Console.WriteLine("?? Learning session completed!");
            return true;
        }

        /// <summary>
        /// Processes a single learning rule provided by the user
        /// </summary>
        private async Task<bool> ProcessLearningRule(string rule, string originalText, InteractiveContext context)
        {
            try
            {
                // Parse the learning rule to extract pattern and mapping
                // Examples:
                // "Each time you see '(M)' you can treat this as Gender=Male"
                // "When you see 'INTENSE' it usually means Concentration=Parfum"
                
                var ruleUpper = rule.ToUpperInvariant();
                
                // Extract the pattern (text between quotes)
                var patternMatch = System.Text.RegularExpressions.Regex.Match(rule, @"['""]([^'""]+)['""]");
                if (!patternMatch.Success)
                {
                    Console.WriteLine($"?? Could not find pattern in quotes for rule: {rule}");
                    return false;
                }
                
                var pattern = patternMatch.Groups[1].Value;
                
                // Extract the property and value (Property=Value)
                var mappingMatch = System.Text.RegularExpressions.Regex.Match(ruleUpper, @"(\w+)\s*=\s*(\w+)");
                if (!mappingMatch.Success)
                {
                    Console.WriteLine($"?? Could not find Property=Value mapping for rule: {rule}");
                    return false;
                }
                
                var property = mappingMatch.Groups[1].Value;
                var value = mappingMatch.Groups[2].Value;
                
                // Apply the learning based on property type
                return property switch
                {
                    "GENDER" => ApplyGenderLearning(pattern, value),
                    "CONCENTRATION" => ApplyConcentrationLearning(pattern, value),
                    "TYPE" => ApplyTypeLearning(pattern, value),
                    "BRAND" => ApplyBrandLearning(pattern, value),
                    "SIZE" => ApplySizeLearning(pattern, value, originalText),
                    _ => false
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error processing rule '{rule}': {ex.Message}");
                return false;
            }
        }

        private bool ApplyGenderLearning(string pattern, string value)
        {
            var gender = value.ToUpperInvariant() switch
            {
                "MALE" or "MAN" or "MEN" or "M" => Gender.Male,
                "FEMALE" or "WOMAN" or "WOMEN" or "F" => Gender.Female,
                "UNISEX" or "U" => Gender.Unisex,
                _ => (Gender?)null
            };
            
            if (gender.HasValue && _runtimeManager != null)
            {
                _runtimeManager.AddGenderMapping(pattern.ToUpperInvariant(), gender.Value);
                Console.WriteLine($"? Added gender mapping: '{pattern}' ? {gender.Value}");
                return true;
            }
            
            Console.WriteLine($"? Could not parse gender value: {value}");
            return false;
        }

        private bool ApplyConcentrationLearning(string pattern, string value)
        {
            var concentration = value.ToUpperInvariant() switch
            {
                "EDT" or "TOILETTE" => Concentration.EDT,
                "EDP" => Concentration.EDP,
                "PARFUM" or "INTENSE" or "ELIXIR" => Concentration.Parfum,
                "EDC" or "COLOGNE" => Concentration.EDC,
                "EDF" or "FRAICHE" => Concentration.EDF,
                _ => (Concentration?)null
            };
            
            if (concentration.HasValue && _runtimeManager != null)
            {
                _runtimeManager.AddConcentrationMapping(pattern.ToUpperInvariant(), concentration.Value);
                Console.WriteLine($"? Added concentration mapping: '{pattern}' ? {concentration.Value}");
                return true;
            }
            
            Console.WriteLine($"? Could not parse concentration value: {value}");
            return false;
        }

        private bool ApplyTypeLearning(string pattern, string value)
        {
            var type = value.ToUpperInvariant() switch
            {
                "SPRAY" or "ATOMIZER" or "VAPORISATEUR" => PerfumeType.Spray,
                "SPLASH" => PerfumeType.Splash,
                "OIL" => PerfumeType.Oil,
                "SOLID" => PerfumeType.Solid,
                "ROLLETTE" => PerfumeType.Rollette,
                "COLOGNE" => PerfumeType.Cologne,
                _ => (PerfumeType?)null
            };
            
            if (type.HasValue && _runtimeManager != null)
            {
                _runtimeManager.AddTypeMapping(pattern.ToUpperInvariant(), type.Value);
                Console.WriteLine($"? Added type mapping: '{pattern}' ? {type.Value}");
                return true;
            }
            
            Console.WriteLine($"? Could not parse type value: {value}");
            return false;
        }

        private bool ApplyBrandLearning(string pattern, string value)
        {
            if (int.TryParse(value, out var brandId) && _runtimeManager != null)
            {
                _runtimeManager.AddBrandMapping(pattern.ToUpperInvariant(), brandId);
                Console.WriteLine($"? Added brand mapping: '{pattern}' ? Brand ID {brandId}");
                return true;
            }
            
            Console.WriteLine($"? Could not parse brand ID: {value}");
            return false;
        }

        private bool ApplySizeLearning(string pattern, string value, string originalText)
        {
            if (_runtimeManager != null)
            {
                // For size learning, we can add a parsing rule to extract size patterns
                _runtimeManager.AddParsingRule(
                    name: $"UserLearnedSize_{pattern.Replace(" ", "_")}",
                    pattern: $@"\b{System.Text.RegularExpressions.Regex.Escape(pattern)}\b",
                    propertyType: PropertyType.Size,
                    priority: 10
                );
                Console.WriteLine($"? Added size pattern rule: '{pattern}' for size detection");
                return true;
            }
            
            Console.WriteLine($"? Runtime manager not available for size learning");
            return false;
        }
    }
}