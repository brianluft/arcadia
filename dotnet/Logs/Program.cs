using System;
using System.CommandLine;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Logs;

public class Config
{
    [JsonPropertyName("storage")]
    public Storage Storage { get; set; } = new();
}

public class Storage
{
    [JsonPropertyName("directory")]
    public string? Directory { get; set; }
}

class Program
{
    private static string? _currentLogFile;
    private static long _currentPosition;
    private static readonly object _lockObject = new();

    static async Task<int> Main(string[] args)
    {
        var snapshotOption = new Option<bool>(
            "--snapshot",
            description: "Print the current contents of the last log file and exit");

        var rootCommand = new RootCommand("Monitor and tail log files from the storage directory")
        {
            snapshotOption
        };

        rootCommand.SetHandler(async (bool snapshot) =>
        {
            await RunAsync(snapshot);
        }, snapshotOption);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task RunAsync(bool snapshot)
    {
        try
        {
            // Read configuration
            string configPath = GetConfigPath();
            string storageDirectory = await ReadStorageDirectoryAsync(configPath);

            if (!Directory.Exists(storageDirectory))
            {
                Console.WriteLine($"Storage directory does not exist: {storageDirectory}");
                return;
            }

            // Find the alphabetically last log file
            string? lastLogFile = GetLastLogFile(storageDirectory);
            
            if (lastLogFile == null)
            {
                if (snapshot)
                {
                    Console.WriteLine("No log files found in storage directory.");
                    return;
                }
                
                // Wait for a log file to appear
                Console.WriteLine("No log files found. Waiting for log files to appear...");
                await WaitForLogFilesToAppear(storageDirectory, snapshot);
                return;
            }

            // Print entire content of the last log file
            _currentLogFile = lastLogFile;
            _currentPosition = 0;
            
            Console.WriteLine($"Reading from: {Path.GetFileName(lastLogFile)}");
            await PrintExistingContent(lastLogFile);

            if (snapshot)
            {
                return; // Exit after printing snapshot
            }

            // Start monitoring for new files and tailing current file
            await MonitorAndTail(storageDirectory);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static string GetConfigPath()
    {
        string? envConfigPath = Environment.GetEnvironmentVariable("ARCADIA_CONFIG_FILE");
        if (!string.IsNullOrEmpty(envConfigPath))
        {
            return envConfigPath;
        }

        // Default to ../config.jsonc relative to the executable
        string exeDirectory = AppContext.BaseDirectory;
        return Path.Combine(exeDirectory, "..", "config.jsonc");
    }

    static async Task<string> ReadStorageDirectoryAsync(string configPath)
    {
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Config file not found: {configPath}");
        }

        string configContent = await File.ReadAllTextAsync(configPath);
        
        // Remove comments from JSONC for parsing
        string jsonContent = RemoveJsonComments(configContent);
        
        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
        Config? config = JsonSerializer.Deserialize<Config>(jsonContent, options);
        
        string storageDir;
        
        if (!string.IsNullOrEmpty(config?.Storage?.Directory))
        {
            // If storage directory is specified, it must be an absolute path
            storageDir = config.Storage.Directory;
            
            if (!Path.IsPathRooted(storageDir))
            {
                throw new InvalidOperationException($"Storage directory must be an absolute path when specified. Got: {storageDir}. Use a Windows-style path like C:\\Tools\\arcadia\\storage or C:/Tools/arcadia/storage");
            }
        }
        else
        {
            // If not specified, default to '../storage/' relative to the executable
            // This makes storage and dotnet sibling directories
            string exeDirectory = AppContext.BaseDirectory;
            storageDir = Path.GetFullPath(Path.Combine(exeDirectory, "..", "storage"));
        }

        return storageDir;
    }

    static string RemoveJsonComments(string jsonc)
    {
        var lines = jsonc.Split('\n');
        var result = new List<string>();
        
        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("//"))
            {
                result.Add(""); // Replace comment lines with empty lines to preserve line numbers
            }
            else
            {
                // Remove inline comments
                int commentIndex = line.IndexOf("//");
                if (commentIndex >= 0)
                {
                    result.Add(line.Substring(0, commentIndex));
                }
                else
                {
                    result.Add(line);
                }
            }
        }
        
        return string.Join('\n', result);
    }

    static string? GetLastLogFile(string storageDirectory)
    {
        var logFiles = Directory.GetFiles(storageDirectory, "*.log")
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToArray();

        return logFiles.LastOrDefault();
    }

    static async Task PrintExistingContent(string logFile)
    {
        if (!File.Exists(logFile))
            return;

        try
        {
            using var reader = new StreamReader(new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string filename = Path.GetFileName(logFile);
                Console.WriteLine($"{timestamp} {filename}: {line}");
            }
            
            _currentPosition = reader.BaseStream.Position;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading file {logFile}: {ex.Message}");
        }
    }

    static async Task WaitForLogFilesToAppear(string storageDirectory, bool snapshot)
    {
        using var watcher = new FileSystemWatcher(storageDirectory, "*.log");
        var tcs = new TaskCompletionSource<string>();

        watcher.Created += (sender, e) =>
        {
            tcs.TrySetResult(e.FullPath);
        };

        watcher.EnableRaisingEvents = true;

        // Wait for first log file to appear
        string firstLogFile = await tcs.Task;
        Console.WriteLine($"New log file detected: {Path.GetFileName(firstLogFile)}");
        
        // Find the current last log file (might be different from the one that triggered)
        string? lastLogFile = GetLastLogFile(storageDirectory);
        if (lastLogFile != null)
        {
            _currentLogFile = lastLogFile;
            _currentPosition = 0;
            
            Console.WriteLine($"Reading from: {Path.GetFileName(lastLogFile)}");
            await PrintExistingContent(lastLogFile);

            if (!snapshot)
            {
                await MonitorAndTail(storageDirectory);
            }
        }
    }

    static async Task MonitorAndTail(string storageDirectory)
    {
        using var fileWatcher = new FileSystemWatcher(storageDirectory, "*.log");
        var cancellationTokenSource = new CancellationTokenSource();

        // Monitor for new log files
        fileWatcher.Created += async (sender, e) =>
        {
            await Task.Delay(100); // Brief delay to ensure file is ready
            
            string? newLastFile = GetLastLogFile(storageDirectory);
            if (newLastFile != null && newLastFile != _currentLogFile)
            {
                lock (_lockObject)
                {
                    Console.WriteLine($"Switching to new log file: {Path.GetFileName(newLastFile)}");
                    _currentLogFile = newLastFile;
                    _currentPosition = 0;
                }
            }
        };

        fileWatcher.EnableRaisingEvents = true;

        // Start tailing task
        var tailingTask = TailCurrentFile(cancellationTokenSource.Token);

        // Keep the application running
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        try
        {
            await tailingTask;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nStopping log monitoring...");
        }
    }

    static async Task TailCurrentFile(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            string? currentFile;
            long currentPos;
            
            lock (_lockObject)
            {
                currentFile = _currentLogFile;
                currentPos = _currentPosition;
            }

            if (currentFile != null && File.Exists(currentFile))
            {
                try
                {
                    using var fileStream = new FileStream(currentFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    fileStream.Seek(currentPos, SeekOrigin.Begin);
                    
                    using var reader = new StreamReader(fileStream);
                    string? line;
                    
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        string filename = Path.GetFileName(currentFile);
                        Console.WriteLine($"{timestamp} {filename}: {line}");
                    }

                    lock (_lockObject)
                    {
                        if (_currentLogFile == currentFile) // Only update if we're still on the same file
                        {
                            _currentPosition = fileStream.Position;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error tailing file {currentFile}: {ex.Message}");
                }
            }

            // Wait before checking again
            await Task.Delay(1000, cancellationToken);
        }
    }
}
