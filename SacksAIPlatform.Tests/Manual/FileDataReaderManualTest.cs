using SacksAIPlatform.InfrastructuresLayer.FileProcessing.Implementations;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing.Interfaces;

namespace SacksAIPlatform.Tests.Manual;

/// <summary>
/// Manual test to verify FileDataReader works with all files in Inputs folder
/// Run this to validate the unified file reader implementation
/// </summary>
public static class FileDataReaderManualTest
{
    public static async Task RunTestAsync()
    {
        Console.WriteLine("=== FileDataReader Manual Test ===");
        Console.WriteLine("Testing unified file reader with real files from Inputs folder...\n");

        var fileDataReader = new FileDataReader();
        var inputsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Inputs");
        
        // Alternative path if first doesn't work
        if (!Directory.Exists(inputsPath))
        {
            inputsPath = Path.Combine("..", "..", "..", "..", "..", "Inputs");
        }
        
        // Try relative to solution root
        if (!Directory.Exists(inputsPath))
        {
            var solutionRoot = FindSolutionRoot();
            if (solutionRoot != null)
            {
                inputsPath = Path.Combine(solutionRoot, "Inputs");
            }
        }
        
        if (!Directory.Exists(inputsPath))
        {
            Console.WriteLine($"❌ Inputs folder not found at: {Path.GetFullPath(inputsPath)}");
            return;
        }

        Console.WriteLine($"📁 Inputs folder: {Path.GetFullPath(inputsPath)}\n");

        // Get all supported files
        var allFiles = Directory.GetFiles(inputsPath)
            .Where(f => fileDataReader.IsSupportedFile(f))
            .ToList();

        Console.WriteLine($"Found {allFiles.Count} supported files:");
        foreach (var file in allFiles)
        {
            Console.WriteLine($"  - {Path.GetFileName(file)}");
        }
        Console.WriteLine();

        // Test each file
        var results = new List<(string fileName, bool success, string details)>();

        foreach (var filePath in allFiles)
        {
            var fileName = Path.GetFileName(filePath);
            Console.Write($"Testing {fileName}... ");

            try
            {
                var fileData = await fileDataReader.ReadFileAsync(filePath);
                var details = $"✅ Cols: {fileData.ColumnCount}, Rows: {fileData.RowCount}";
                
                if (fileData.ColumnCount > 0)
                {
                    details += $", Headers: {string.Join(", ", fileData.Headers.Take(3))}";
                    if (fileData.Headers.Length > 3)
                        details += "...";
                }

                Console.WriteLine(details);
                results.Add((fileName, true, details));
            }
            catch (Exception ex)
            {
                var errorMsg = $"❌ Error: {ex.Message}";
                Console.WriteLine(errorMsg);
                results.Add((fileName, false, errorMsg));
            }
        }

        // Summary
        Console.WriteLine($"\n=== Summary ===");
        var successful = results.Count(r => r.success);
        Console.WriteLine($"Successfully read: {successful}/{results.Count} files");
        
        if (successful > 0)
        {
            Console.WriteLine("\n✅ Successful files:");
            foreach (var result in results.Where(r => r.success))
            {
                Console.WriteLine($"  {result.fileName}: {result.details}");
            }
        }

        if (results.Any(r => !r.success))
        {
            Console.WriteLine("\n❌ Failed files:");
            foreach (var result in results.Where(r => !r.success))
            {
                Console.WriteLine($"  {result.fileName}: {result.details}");
            }
        }

        // Test specific format comparison
        await TestFormatComparisonAsync(fileDataReader, inputsPath);
    }

    private static async Task TestFormatComparisonAsync(IFileDataReader fileDataReader, string inputsPath)
    {
        Console.WriteLine("\n=== Format Comparison Test ===");
        
        var csvPath = Path.Combine(inputsPath, "ComprehensiveStockAi.csv");
        var xlsxPath = Path.Combine(inputsPath, "ComprehensiveStockAi.xlsx");

        if (File.Exists(csvPath) && File.Exists(xlsxPath))
        {
            try
            {
                var csvData = await fileDataReader.ReadFileAsync(csvPath);
                var xlsxData = await fileDataReader.ReadFileAsync(xlsxPath);

                Console.WriteLine($"CSV:  {csvData.ColumnCount} columns, {csvData.RowCount} rows");
                Console.WriteLine($"XLSX: {xlsxData.ColumnCount} columns, {xlsxData.RowCount} rows");
                
                Console.WriteLine("\nCSV Headers:");
                Console.WriteLine($"  {string.Join(", ", csvData.Headers)}");
                
                Console.WriteLine("\nXLSX Headers:");
                Console.WriteLine($"  {string.Join(", ", xlsxData.Headers)}");

                // Show first row comparison if both have data
                if (csvData.RowCount > 0 && xlsxData.RowCount > 0)
                {
                    Console.WriteLine("\nFirst row comparison:");
                    var csvFirstRow = csvData.GetRow(0);
                    var xlsxFirstRow = xlsxData.GetRow(0);
                    
                    for (int i = 0; i < Math.Min(3, Math.Min(csvFirstRow.Length, xlsxFirstRow.Length)); i++)
                    {
                        Console.WriteLine($"  {csvData.Headers[i]}: CSV='{csvFirstRow[i]}' vs XLSX='{xlsxFirstRow[i]}'");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error comparing formats: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("⚠️ Cannot compare - ComprehensiveStockAi files not found in both formats");
        }
    }

    private static string? FindSolutionRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        while (currentDir != null)
        {
            if (Directory.GetFiles(currentDir, "*.sln").Any())
            {
                return currentDir;
            }
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }
        return null;
    }
}
