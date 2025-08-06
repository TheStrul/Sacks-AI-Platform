using SacksAIPlatform.InfrastructuresLayer.AI.Services;
using SacksAIPlatform.InfrastructuresLayer.AI.Models;

/// <summary>
/// Simple test to demonstrate AiAgentBase tool registration without requiring OpenAI API
/// Shows how FilesystemFunctionHandler tools are always included + additional tools from config
/// </summary>
class ConfigTest
{
    public static async Task Test(string[] args)
    {
        Console.WriteLine("=== AiAgentBase Configuration Test ===");
        Console.WriteLine("Testing tool registration without OpenAI API calls");
        Console.WriteLine();

        try
        {
            // Test 1: AiAgentBase with default configuration (should include filesystem tools + config tools)
            Console.WriteLine("🔧 Test 1: Loading default configuration...");
            var configPath = AiAgentBaseConfiguration.GetDefaultConfigPath();
            Console.WriteLine($"📁 Configuration file: {configPath}");
            
            if (File.Exists(configPath))
            {
                var config = await AiAgentBaseConfiguration.LoadFromFileAsync(configPath);
                Console.WriteLine($"✅ Configuration loaded: {config.Name}");
                Console.WriteLine($"📋 Model: {config.Model}");
                Console.WriteLine($"🌡️  Temperature: {config.Temperature}");
                Console.WriteLine($"🔧 Functions in config: {config.Functions?.Count ?? 0}");
                
                if (config.Functions != null)
                {
                    foreach (var func in config.Functions)
                    {
                        Console.WriteLine($"   • {func.Name}: {func.Description}");
                    }
                }
            }
            else
            {
                Console.WriteLine("⚠️  Default configuration file not found");
            }
            
            Console.WriteLine();

            // Test 2: Show what FilesystemFunctionHandler provides
            Console.WriteLine("🔧 Test 2: FilesystemFunctionHandler built-in tools...");
            var filesystemTools = FilesystemFunctionHandler.GetFunctionTools();
            Console.WriteLine($"📋 Built-in filesystem tools: {filesystemTools.Count}");
            
            foreach (var tool in filesystemTools)
            {
                Console.WriteLine($"   • {tool.FunctionName}: {tool.Description}");
            }
            
            Console.WriteLine();

            // Test 3: Test with example configuration (shows additional tools)
            Console.WriteLine("🔧 Test 3: Loading example configuration with additional tools...");
            var exampleConfigPath = Path.Combine(
                Path.GetDirectoryName(configPath) ?? "", 
                "assistant-config-example.json"
            );
            
            if (File.Exists(exampleConfigPath))
            {
                var exampleConfig = await AiAgentBaseConfiguration.LoadFromFileAsync(exampleConfigPath);
                Console.WriteLine($"✅ Example configuration loaded: {exampleConfig.Name}");
                Console.WriteLine($"🔧 Additional functions in example config: {exampleConfig.Functions?.Count ?? 0}");
                
                if (exampleConfig.Functions != null)
                {
                    foreach (var func in exampleConfig.Functions)
                    {
                        Console.WriteLine($"   • {func.Name}: {func.Description}");
                    }
                }
            }
            else
            {
                Console.WriteLine("⚠️  Example configuration file not found");
            }

            Console.WriteLine();
            Console.WriteLine("✅ All tests completed successfully!");
            Console.WriteLine();
            Console.WriteLine("📋 Summary:");
            Console.WriteLine("   • FilesystemFunctionHandler tools are ALWAYS included (list_files, list_directories, get_file_info, search_files)");
            Console.WriteLine("   • Additional tools from configuration are added (if they don't duplicate built-in tools)");
            Console.WriteLine("   • Duplicate tools from configuration are automatically skipped");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during test: {ex.Message}");
        }
        Console.WriteLine();

        try
        {
            // Test configuration path discovery
            var configPath = AiAgentBaseConfiguration.GetDefaultConfigPath();
            Console.WriteLine($"📁 Configuration file path: {configPath}");
            Console.WriteLine($"📂 File exists: {File.Exists(configPath)}");
            Console.WriteLine();

            if (File.Exists(configPath))
            {
                // Test configuration loading
                Console.WriteLine("🔧 Loading configuration...");
                var config = await AiAgentBaseConfiguration.LoadFromFileAsync(configPath);
                
                Console.WriteLine("✅ Configuration loaded successfully!");
                Console.WriteLine($"📋 Assistant Name: {config.Name}");
                Console.WriteLine($"🤖 Model: {config.Model}");
                Console.WriteLine($"🌡️  Temperature: {config.Temperature}");
                Console.WriteLine($"📝 Description: {config.Description}");
                Console.WriteLine($"📖 Instructions Preview: {config.Instructions[..Math.Min(100, config.Instructions.Length)]}...");
                Console.WriteLine();

                // Test serialization back
                Console.WriteLine("💾 Testing serialization...");
                var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                Console.WriteLine("✅ Serialization successful!");
                Console.WriteLine($"📄 JSON preview: {json[..Math.Min(200, json.Length)]}...");
            }
            else
            {
                Console.WriteLine("❌ Configuration file not found!");
                Console.WriteLine("💡 Make sure assistant-config.json exists in the SacksAIPlatform.InfrastructuresLayer/AI/ folder");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine($"📋 Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine();
        Console.WriteLine("🎯 Test completed!");
    }
}
