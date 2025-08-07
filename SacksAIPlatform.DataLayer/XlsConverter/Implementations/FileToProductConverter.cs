namespace SacksAIPlatform.DataLayer.XlsConverter
{
    using SacksAIPlatform.DataLayer.Entities;
    using SacksAIPlatform.DataLayer.Enums;
    using SacksAIPlatform.DataLayer.XlsConverter.Helpers;
    using SacksAIPlatform.InfrastructuresLayer.FileProcessing;


    /*
     I need you help in creating a kind of Dictionary helper and a simple parser to be used by class FiletoProductConverter on the proccess of converting raw data into a real Product object.
the raw data will always be string, somtitimes it will be axactly in the needed format and syntaxs, e.g property "Code" is expected to be like "12365478987" and this what we get, but sometime, we will get somthing like "ADP BLU MEDITERRANEO MIRTO DI PANAREA  30ML EDT SPRAY 29.6ml" where in this case we needs to understand that:
* "ADP" is Concentration.Parfum, 
* "30ML" is size of 30 and Units.ml
* SPRAY is PerfumeType.Spray
* "29.6ml" should be ignored
the parser and the dictionary should be configurable on runTime, meaning, the user can:
* add the dictionary new words that can help the parser understand which propert is described
* add new "rules" to the parser to help him with the parsing
* the parser will also have additional 2 seperated Dictionaties:
* BrandName to BarandI
 * ProductName to BarandI
 
    */
    public class FiletoProductConverter : IFiletoProductConverter
    {
        private FileDataReader _fileDataReader = new FileDataReader();
        private FileConfiguration _configuration;
        private FileConversionResult _result = new FileConversionResult();
        private readonly ProductDescriptionParser _parser;
        private readonly ProductParserConfigurationManager _configManager;



        /// <summary>
        /// Initializes the converter with a specific parser configuration manager
        /// </summary>
        public FiletoProductConverter(ProductParserConfigurationManager configManager)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _parser = new ProductDescriptionParser(_configManager.CurrentConfiguration);
        }

        /// <summary>
        /// Initializes the converter with a specific parser configuration manager and file configuration
        /// </summary>
        public FiletoProductConverter(ProductParserConfigurationManager configManager, FileConfiguration configuration)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _parser = new ProductDescriptionParser(_configManager.CurrentConfiguration);
            _configuration = configuration ?? FileConfiguration.CreateDefaultConfiguration();
        }

        /// <summary>
        /// Gets the parser configuration manager for runtime updates
        /// </summary>
        public ProductParserConfigurationManager ConfigurationManager => _configManager;

        /// <summary>
        /// Gets the current parser instance
        /// </summary>
        public ProductDescriptionParser Parser => _parser;


        public async Task<FileConversionResult> ConvertFileToProductsAsync(string fullPath, FileConfiguration configuration)
        {
            _result.Clear();

            _configuration = configuration ?? FileConfiguration.CreateDefaultConfiguration();
            try
            {
                var fileData = await _fileDataReader.ReadFileAsync(fullPath);
                var endRow = _configuration.EndAtRow == -1 ? fileData.RowCount - 1 : Math.Min(_configuration.EndAtRow, fileData.RowCount - 1);

                // _configuration.StartFromRow and _configuration.EndAtRow are 1-based indices
                for (int i = _configuration.StartFromRow; i <= endRow; i++)
                {
                    _result.TotalLinesProcessed++;
                    RowData? r = fileData.GetRow(i - 1);
                    if (r == null || r.Cells.Count == 0)
                    {
                        // Skip empty rows and add validation error
                        _result.ValidationErrors.Add(new FileValidationError
                        {
                            RowNumber = i,
                            Field = "General",
                            Value = string.Empty,
                            ErrorMessage = "Empty row",
                            RawLine = string.Empty
                        });
                        continue;
                    }
                    // Skip inner titles if configured
                    if (_configuration.HasInnerTitles && IsLikelyTitleRow(r))
                    {
                        continue;
                    }

                    try
                    {
                        var product = ParseRowToProduct(r);
                        if (product != null)
                        {
                            if (product.Validate())
                            {
                                _result.ValidProducts.Add(product);
                            }
                            else
                            {
                                // Add detailed validation errors
                                var validationErrors = product.GetValidationErrors();
                                foreach (var validationError in validationErrors)
                                {
                                    _result.ValidationErrors.Add(new FileValidationError
                                    {
                                        RowNumber = i,
                                        Field = "Product Validation",
                                        Value = product.Code ?? string.Empty,
                                        ErrorMessage = validationError,
                                        RawLine = string.Join(",", r.Cells.Select(c => c.Value))
                                    });
                                }
                            }
                        }
                        else
                        {
                            _result.EmptyLines++;
                        }
                    }
                    catch (Exception ex)
                    {
                        var rowData = string.Join(",", r.Cells);
                        _result.ValidationErrors.Add(new FileValidationError
                        {
                            RowNumber = i,
                            Field = "General",
                            Value = string.Empty,
                            ErrorMessage = ex.Message,
                            RawLine = rowData
                        });
                    }
                }

                return _result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to process CSV file: {ex.Message}", ex);
            }
        }


        private bool IsLikelyTitleRow(RowData row)
        {
            if ((row == null) || row.Cells.Count != _configuration.ValidNumOfColumns)
            {
                return true;
            }
            return false;
        }
        private Product? ParseRowToProduct(RowData fields)
        {
            if (fields.HasData)
            {
                var product = new Product
                {
                    OriginalSource = fields.ToString(),
                };

                foreach (KeyValuePair<int, PropertyType> keyValue in _configuration.ColumnMapping)
                {
                    MapFieldToProduct(product, keyValue.Value, fields.Cells[keyValue.Key - 1].Value, fields.Index);
                }

                foreach (int i in _configuration.DescriptionColumns)
                {
                    AnalyzeDescripotionFields(product, fields.Cells[i - 1]);
                }

                return product;
            }
            else
            {
                // If no data is present, return null
                return null;
            }
        }

        private void MapFieldToProduct(Product product, PropertyType propertyType, string fieldValue, int rowNumber)
        {
            try
            {
                switch (propertyType)
                {
                    case PropertyType.Code:
                        DoUpdateCode(product, fieldValue, rowNumber);
                        if (!string.IsNullOrWhiteSpace(fieldValue))
                            product.Code = fieldValue;
                        break;

                    case PropertyType.Name:
                        DoUpdateName(product, fieldValue, rowNumber);
                        if (!string.IsNullOrWhiteSpace(fieldValue))
                            product.Name = CleanProductName(fieldValue);
                        break;

                    case PropertyType.Brand:
                        DoUpdateBrand(product, fieldValue, rowNumber);
                        if (int.TryParse(fieldValue, out var brandId))
                            product.BrandID = brandId;
                        break;

                    case PropertyType.Concentration:
                        DoUpdateConcentration(product, fieldValue, rowNumber);
                        break;

                    case PropertyType.Type:
                        DoUpdateType(product, fieldValue, rowNumber);
                        break;

                    case PropertyType.Gender:
                        DoUpdateGender(product, fieldValue, rowNumber);
                        break;

                    case PropertyType.Size:
                        DoUpdateSize(product, fieldValue, rowNumber);
                        break;

                    case PropertyType.LilFree:
                        DoUpdateLilFree(product, fieldValue, rowNumber);
                        break;

                    case PropertyType.CountryOfOrigin:
                        DoUpdateCountryOfOrigin(product, fieldValue, rowNumber);
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to map field '{fieldValue}' to property '{propertyType}' at row {rowNumber}: {ex.Message}");
            }
        }

        private void AnalyzeDescripotionFields(Product product, CellData cellData)
        {
            if (cellData == null || string.IsNullOrWhiteSpace(cellData.Value))
                return;

            // Use the parser to analyze the description and update the product
            _parser.ParseAndUpdateProduct(product, cellData.Value, overwriteExisting: false);
        }

        private void DoUpdateConcentration(Product product, string fieldValue, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(fieldValue))
                return;

            // Try direct parsing first
            var concentration = ParseConcentration(fieldValue);
            if (concentration != Concentration.Unknown)
            {
                product.Concentration = concentration;
                return;
            }

            // If direct parsing fails, use the parser for complex descriptions
            var parsed = _parser.ParseDescription(fieldValue);
            if (parsed.Concentration.HasValue)
            {
                product.Concentration = parsed.Concentration.Value;
            }
        }

        private void DoUpdateType(Product product, string fieldValue, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(fieldValue))
                return;

            // Try direct parsing first
            var type = ParseType(fieldValue);
            if (type != PerfumeType.Spray || fieldValue.ToLowerInvariant().Contains("spray"))
            {
                product.Type = type;
                return;
            }

            // If direct parsing fails, use the parser for complex descriptions
            var parsed = _parser.ParseDescription(fieldValue);
            if (parsed.Type.HasValue)
            {
                product.Type = parsed.Type.Value;
            }
        }

        private void DoUpdateGender(Product product, string fieldValue, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(fieldValue))
                return;

            // Try direct parsing first
            var gender = ParseGender(fieldValue);
            if (gender != Gender.Unisex || fieldValue.ToLowerInvariant().Contains("unisex"))
            {
                product.Gender = gender;
                return;
            }

            // If direct parsing fails, use the parser for complex descriptions
            var parsed = _parser.ParseDescription(fieldValue);
            if (parsed.Gender.HasValue)
            {
                product.Gender = parsed.Gender.Value;
            }
        }

        private void DoUpdateSize(Product product, string fieldValue, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(fieldValue))
                return;

            // Try direct parsing first
            var size = ParseSize(fieldValue);
            var units = ParseUnits(fieldValue);

            if (size != "0")
            {
                product.Size = size;
                product.Units = units;
                return;
            }

            // If direct parsing fails, use the parser for complex descriptions
            var parsed = _parser.ParseDescription(fieldValue);
            if (!string.IsNullOrEmpty(parsed.Size))
            {
                product.Size = parsed.Size;
                if (parsed.Units.HasValue)
                {
                    product.Units = parsed.Units.Value;
                }
            }
        }

        private void DoUpdateBrand(Product product, string fieldValue, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(fieldValue))
                return;

            // Use the parser to look up brand by name
            var parsed = _parser.ParseDescription(fieldValue);
            if (parsed.BrandId.HasValue)
            {
                product.BrandID = parsed.BrandId.Value;
            }
        }

        private void DoUpdateLilFree(Product product, string fieldValue, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(fieldValue))
                return;

            product.LilFree = ParseLiFree(fieldValue);
        }

        private void DoUpdateCountryOfOrigin(Product product, string fieldValue, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(fieldValue))
                return;

            product.CountryOfOrigin = CleanCountryName(fieldValue);
        }

        private void DoUpdateName(Product product, string fieldValue, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(fieldValue))
                return;

            // Clean the product name
            var cleanedName = CleanProductName(fieldValue);
            product.Name = cleanedName;

            // Try to extract brand information from the product name
            var parsed = _parser.ParseDescription(fieldValue);
            if (parsed.BrandId.HasValue && product.BrandID == 0)
            {
                product.BrandID = parsed.BrandId.Value;
            }

            // Use extracted product name if available and current name seems to contain extra info
            if (!string.IsNullOrEmpty(parsed.ExtractedProductName) &&
                parsed.ExtractedProductName.Length < cleanedName.Length)
            {
                product.Name = parsed.ExtractedProductName;
            }
        }

        private void DoUpdateCode(Product product, string fieldValue, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(fieldValue))
                return;

            // For code, we typically expect a direct value, but let's clean it
            var cleanedCode = fieldValue.Trim().Replace("\"", "");

            // If the code looks like it might contain extra information, try to parse it
            if (cleanedCode.Contains(" ") && cleanedCode.Length > 20)
            {
                // This might be a complex description rather than a simple code
                var parsed = _parser.ParseDescription(fieldValue);

                // Extract potential code from the beginning or look for numeric patterns
                var words = cleanedCode.Split(' ');
                var potentialCode = words.FirstOrDefault(w => w.All(char.IsDigit) && w.Length > 5);

                if (!string.IsNullOrEmpty(potentialCode))
                {
                    product.Code = potentialCode;
                }
                else
                {
                    // Use the first part as code if no clear numeric code is found
                    product.Code = words[0];
                }

                // Update other properties if found in the description
                _parser.ParseAndUpdateProduct(product, fieldValue, overwriteExisting: false);
            }
            else
            {
                // Direct assignment for simple codes
                product.Code = cleanedCode;
            }
        }

        private Units ParseUnitsFromText(string unitsField)
        {
            if (string.IsNullOrWhiteSpace(unitsField))
                return Units.ml;

            var lower = unitsField.ToLowerInvariant();

            return lower switch
            {
                "oz" or "fl oz" or "fluid ounce" => Units.oz,
                "ml" or "milliliter" => Units.ml,
                "g" or "gram" or "grams" => Units.g,
                _ => Units.ml
            };
        }

        private string CleanProductName(string productName)
        {
            return productName
                .Replace("\"", "")
                .Replace("  ", " ")
                .Trim();
        }

        private string ParseSize(string sizeField)
        {
            if (string.IsNullOrWhiteSpace(sizeField) || sizeField.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                return "0";

            // Extract numeric part
            var cleanSize = System.Text.RegularExpressions.Regex.Match(sizeField, @"[\d.]+").Value;
            return string.IsNullOrEmpty(cleanSize) ? "0" : cleanSize;
        }

        private Units ParseUnits(string sizeField)
        {
            if (string.IsNullOrWhiteSpace(sizeField))
                return Units.ml;

            var lowerSize = sizeField.ToLowerInvariant();

            if (lowerSize.Contains("oz") || lowerSize.Contains("fl"))
                return Units.oz;
            else if (lowerSize.Contains("ml"))
                return Units.ml;
            else if (lowerSize.Contains("g") || lowerSize.Contains("gram"))
                return Units.g;
            else
                return Units.ml; // Default to ml
        }

        private Concentration ParseConcentration(string concentrationField)
        {
            if (string.IsNullOrWhiteSpace(concentrationField))
                return Concentration.Unknown;

            var lower = concentrationField.ToLowerInvariant().Trim();

            return lower switch
            {
                "eau de toilette" or "edt" => Concentration.EDT,
                "eau de parfum" or "edp" => Concentration.EDP,
                "parfum" or "parfum intense" or "elixir" => Concentration.Parfum,
                "eau de cologne" or "edc" or "cologne" => Concentration.EDC,
                "eau de fraiche" or "edf" => Concentration.EDF,
                _ => Concentration.Unknown
            };
        }

        private PerfumeType ParseType(string typeField)
        {
            if (string.IsNullOrWhiteSpace(typeField) || typeField.Equals("NA", StringComparison.OrdinalIgnoreCase))
                return PerfumeType.Spray;

            var lower = typeField.ToLowerInvariant().Trim();

            return lower switch
            {
                "sp" or "spray" => PerfumeType.Spray,
                "cologne" => PerfumeType.Cologne,
                "fl" or "splash" => PerfumeType.Splash,
                "oil" => PerfumeType.Oil,
                "solid" => PerfumeType.Solid,
                "rollette" => PerfumeType.Rollette,
                _ => PerfumeType.Spray // Default to spray
            };
        }

        private Gender ParseGender(string genderField)
        {
            if (string.IsNullOrWhiteSpace(genderField))
                return Gender.Unisex;

            var lower = genderField.ToLowerInvariant().Trim();

            return lower switch
            {
                "m" or "male" or "men" => Gender.Male,
                "w" or "f" or "female" or "women" => Gender.Female,
                "u" or "unisex" => Gender.Unisex,
                _ => Gender.Unisex // Default to unisex
            };
        }

        private bool ParseLiFree(string liFreeField)
        {
            if (string.IsNullOrWhiteSpace(liFreeField))
                return false;

            var lower = liFreeField.ToLowerInvariant().Trim();
            return lower.Contains("free") || lower.Equals("none", StringComparison.OrdinalIgnoreCase);
        }

        private string CleanCountryName(string countryField)
        {
            if (string.IsNullOrWhiteSpace(countryField))
                return "";

            return countryField.Trim();
        }

        /// <summary>
        /// Cleans a CSV field by removing quotes and trimming whitespace
        /// </summary>
        private string CleanField(string? field)
        {
            if (string.IsNullOrEmpty(field))
            {
                return string.Empty;
            }

            // First trim whitespace, then remove surrounding quotes, then handle escaped quotes
            return field
                .Trim() // Remove leading/trailing whitespace
                .Trim('"') // Remove surrounding quotes
                .Replace("\"\"", "\"") // Handle escaped quotes (convert "" to ")
                .Trim(); // Final trim after processing
        }
    }
}