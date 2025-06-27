using System.Text.Json;
using System.Text.RegularExpressions;

namespace ComputerUse;

public class ArcadiaConfig
{
    public string OpenAIKey { get; set; } = string.Empty;

    public static ArcadiaConfig LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Configuration file not found: {filePath}");

        string jsonContent = File.ReadAllText(filePath);

        // Remove comments and trailing commas from JSONC
        string[] lines = jsonContent.Split('\n');
        var cleanedLines = new List<string>();

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            // Skip lines that start with # or //
            if (trimmedLine.StartsWith("#") || trimmedLine.StartsWith("//"))
                continue;

            // Remove inline comments (# or // after content)
            string cleanLine = Regex.Replace(line, @"\s*(#|//).*$", "");
            cleanedLines.Add(cleanLine);
        }

        string cleanJson = string.Join("\n", cleanedLines);

        // Remove trailing commas before } or ]
        cleanJson = Regex.Replace(cleanJson, @",(\s*[}\]])", "$1");

        try
        {
            using JsonDocument document = JsonDocument.Parse(cleanJson);

            // Navigate to .apiKeys.openai
            if (!document.RootElement.TryGetProperty("apiKeys", out JsonElement apiKeysElement))
                throw new InvalidOperationException("Configuration file must contain an 'apiKeys' section");

            if (!apiKeysElement.TryGetProperty("openai", out JsonElement openaiElement))
                throw new InvalidOperationException(
                    "OpenAI API key is required in config.jsonc at path 'apiKeys.openai'"
                );

            string? openaiKey = openaiElement.GetString();
            if (string.IsNullOrWhiteSpace(openaiKey))
                throw new InvalidOperationException("OpenAI API key cannot be empty in config.jsonc");

            return new ArcadiaConfig { OpenAIKey = openaiKey };
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid JSON in configuration file: {ex.Message}", ex);
        }
    }
}
