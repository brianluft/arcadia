using System.CommandLine;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace Database;

public class InputJson
{
    public string provider { get; set; } = "";
    public string connectionString { get; set; } = "";
    public string query { get; set; } = "";
    public Dictionary<string, ParameterValue>? parameters { get; set; }
    public string commandType { get; set; } = "Text";
    public int timeoutSeconds { get; set; } = 30;
    public int skipRows { get; set; } = 0;
    public int takeRows { get; set; } = 1000;
}

public class ParameterValue
{
    public string type { get; set; } = "";
    public object? value { get; set; }
}

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var inputOption = new Option<FileInfo>("--input", "Input JSON file path") { IsRequired = true };

        var outputOption = new Option<FileInfo>("--output", "Output JSON file path") { IsRequired = true };

        var expectOption = new Option<FileInfo?>("--expect", "Expected output file for comparison")
        {
            IsRequired = false,
        };

        var rootCommand = new RootCommand("Database query executor") { inputOption, outputOption, expectOption };

        rootCommand.SetHandler(
            async (inputFile, outputFile, expectFile) =>
            {
                try
                {
                    await ExecuteQuery(inputFile, outputFile, expectFile);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                    Environment.Exit(1);
                }
            },
            inputOption,
            outputOption,
            expectOption
        );

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task ExecuteQuery(FileInfo inputFile, FileInfo outputFile, FileInfo? expectFile)
    {
        // Read and parse input JSON
        var inputText = await File.ReadAllTextAsync(inputFile.FullName);
        var input = JsonSerializer.Deserialize<InputJson>(
            inputText,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (input == null)
        {
            throw new Exception("Failed to parse input JSON");
        }

        // Create database connection
        DbConnection connection = input.provider switch
        {
            "Microsoft.Data.Sqlite" => new SqliteConnection(input.connectionString),
            "Microsoft.Data.SqlClient" => new SqlConnection(input.connectionString),
            _ => throw new Exception($"Unsupported provider: {input.provider}"),
        };

        // Disable connection pooling
        if (connection is SqliteConnection sqliteConn)
        {
            // SQLite doesn't use connection pooling by default
        }
        else if (connection is SqlConnection sqlClientConn)
        {
            // For SQL Server, we add Pooling=false to connection string if not already present
            var builder = new SqlConnectionStringBuilder(input.connectionString);
            builder.Pooling = false;
            sqlClientConn.ConnectionString = builder.ConnectionString;
        }

        try
        {
            await connection.OpenAsync();

            // Start transaction that will always roll back
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = input.query;
                command.CommandType = Enum.Parse<CommandType>(input.commandType, true);
                command.CommandTimeout = input.timeoutSeconds;

                // Add parameters if provided
                if (input.parameters != null)
                {
                    foreach (var param in input.parameters)
                    {
                        var dbParam = command.CreateParameter();
                        dbParam.ParameterName = param.Key;
                        dbParam.Value = ConvertParameterValue(param.Value.type, param.Value.value);
                        command.Parameters.Add(dbParam);
                    }
                }

                var reader = await command.ExecuteReaderAsync();
                var results = new List<List<object?>>();

                try
                {
                    // Add column headers
                    var headers = new List<object?>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        headers.Add(reader.GetName(i));
                    }
                    results.Add(headers);

                    // Skip requested rows
                    int skipped = 0;
                    while (skipped < input.skipRows && await reader.ReadAsync())
                    {
                        skipped++;
                    }

                    // Take requested rows
                    int taken = 0;
                    while (taken < input.takeRows && await reader.ReadAsync())
                    {
                        var row = new List<object?>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row.Add(reader.IsDBNull(i) ? null : reader.GetValue(i));
                        }
                        results.Add(row);
                        taken++;
                    }
                }
                finally
                {
                    // Ensure reader is always closed
                    if (!reader.IsClosed)
                    {
                        await reader.CloseAsync();
                    }
                    await reader.DisposeAsync();
                }

                // Write output JSON (after reader is fully disposed)
                var outputJson = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = false });
                await File.WriteAllTextAsync(outputFile.FullName, outputJson);

                // Always roll back the transaction
                await transaction.RollbackAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        finally
        {
            await connection.CloseAsync();
        }

        // Handle --expect comparison if provided
        if (expectFile != null)
        {
            var expectedText = await File.ReadAllTextAsync(expectFile.FullName);
            var actualText = await File.ReadAllTextAsync(outputFile.FullName);

            // Trim leading/trailing whitespace and normalize line endings
            expectedText = expectedText.Trim().Replace("\r\n", "\n");
            actualText = actualText.Trim().Replace("\r\n", "\n");

            if (expectedText == actualText)
            {
                Console.WriteLine($"Pass: {outputFile.Name} matches {expectFile.Name}");
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine($"Fail: {outputFile.Name} does not match {expectFile.Name}");
                Environment.Exit(1);
            }
        }
    }

    private static object? ConvertParameterValue(string type, object? value)
    {
        if (value == null)
            return DBNull.Value;

        // Handle JsonElement if the value comes from JSON deserialization
        if (value is JsonElement jsonElement)
        {
            return type switch
            {
                "String" => jsonElement.GetString(),
                "Int32" => jsonElement.GetInt32(),
                "Int64" => jsonElement.GetInt64(),
                "Double" => jsonElement.GetDouble(),
                "Boolean" => jsonElement.GetBoolean(),
                "DateTime" => jsonElement.GetDateTime(),
                "Decimal" => jsonElement.GetDecimal(),
                _ => jsonElement.ToString(),
            };
        }

        return type switch
        {
            "String" => value.ToString(),
            "Int32" => Convert.ToInt32(value),
            "Int64" => Convert.ToInt64(value),
            "Double" => Convert.ToDouble(value),
            "Boolean" => Convert.ToBoolean(value),
            "DateTime" => Convert.ToDateTime(value),
            "Decimal" => Convert.ToDecimal(value),
            _ => value,
        };
    }
}
