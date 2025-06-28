using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using OpenAI;
using OpenAI.Chat;

namespace ComputerUse;

public class RunCommand : ICommand
{
    private readonly ScreenUse _screenUse;
    private readonly MouseUse _mouseUse;
    private readonly KeyboardUse _keyboardUse;
    private readonly WindowWalker _windowWalker;
    private readonly StatusReporter _statusReporter;

    public string ConfigFile { get; set; } = string.Empty;
    public string PromptFile { get; set; } = string.Empty;
    public string StorageFolder { get; set; } = string.Empty;
    public string OutputFile { get; set; } = string.Empty;

    public RunCommand(
        ScreenUse screenUse,
        MouseUse mouseUse,
        KeyboardUse keyboardUse,
        WindowWalker windowWalker,
        StatusReporter statusReporter
    )
    {
        _screenUse = screenUse;
        _mouseUse = mouseUse;
        _keyboardUse = keyboardUse;
        _windowWalker = windowWalker;
        _statusReporter = statusReporter;
    }

    public async Task ExecuteAsync(StatusReporter statusReporter)
    {
        try
        {
            // Load configuration
            var config = ArcadiaConfig.LoadFromFile(ConfigFile);

            // Load prompt
            if (!File.Exists(PromptFile))
                throw new FileNotFoundException($"Prompt file not found: {PromptFile}");
            string promptText = File.ReadAllText(PromptFile);

            // Verify storage folder exists
            if (!Directory.Exists(StorageFolder))
                throw new DirectoryNotFoundException($"Storage folder not found: {StorageFolder}");

            var storageFolder = new StorageFolder(StorageFolder);

            // Create output file
            var outputFileInfo = new FileInfo(OutputFile);
            outputFileInfo.Directory?.Create();

            // Set up OpenAI client
            var client = new ChatClient("gpt-4o", config.OpenAIKey);

            await RunComputerUseLoopAsync(client, promptText, storageFolder, outputFileInfo);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw;
        }
    }

    private record ScreenshotFiles(FileInfo Primary, FileInfo? Overview = null);

    private ScreenshotFiles TakeAndSaveScreenshots(StorageFolder storageFolder, ZoomPath? zoomPath)
    {
        var screenshots = _screenUse.TakeScreenshots(zoomPath);

        try
        {
            // Save primary screenshot
            var primaryFile = storageFolder.GenerateFilename("png");
            screenshots.Primary.Save(primaryFile.FullName, System.Drawing.Imaging.ImageFormat.Png);

            FileInfo? overviewFile = null;
            if (screenshots.Overview != null)
            {
                // Save overview screenshot
                overviewFile = storageFolder.GenerateFilename("png");
                screenshots.Overview.Save(overviewFile.FullName, System.Drawing.Imaging.ImageFormat.Png);
            }

            return new ScreenshotFiles(primaryFile, overviewFile);
        }
        finally
        {
            screenshots.Primary.Dispose();
            screenshots.Overview?.Dispose();
        }
    }

    private async Task RunComputerUseLoopAsync(
        ChatClient client,
        string promptText,
        StorageFolder storageFolder,
        FileInfo outputFile
    )
    {
        _statusReporter.Report($"> {promptText}");

        // Take initial screenshot
        var initialScreenshots = TakeAndSaveScreenshots(storageFolder, null);
        var primaryScreenshot = initialScreenshots.Primary;

        // Initialize conversation
        var messages = new List<ChatMessage> { new SystemChatMessage(SystemPrompt) };
        var logEntries = new List<string>();

        // Define available tools
        var tools = new List<ChatTool>
        {
            CreateScreenshotTool(),
            CreateMouseClickTool(),
            CreateKeyPressTool(),
            CreateTypeTool(),
        };

        var options = new ChatCompletionOptions();
        foreach (var tool in tools)
        {
            options.Tools.Add(tool);
        }

        bool continueLoop = true;
        int iteration = 0;
        const int maxIterations = 50;

        try
        {
            while (continueLoop && iteration < maxIterations)
            {
                iteration++;

                // Get current window information
                var focusedWindow = _windowWalker.GetFocusedWindow();
                var unfocusedWindows = _windowWalker.GetUnfocusedWindows();

                // Create user message with context
                var contextMessage = CreateContextMessage(
                    promptText,
                    primaryScreenshot,
                    focusedWindow,
                    unfocusedWindows,
                    initialScreenshots.Overview
                );
                messages.Add(contextMessage);

                // Log the user prompt
                logEntries.Add($"=== Iteration {iteration} - User Prompt ===");
                logEntries.Add($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                logEntries.Add($"Focused Window: {focusedWindow?.Title ?? "None"}");
                logEntries.Add($"Unfocused Windows: {string.Join(", ", unfocusedWindows.Select(w => w.Title))}");
                logEntries.Add($"Screenshot: {primaryScreenshot.Name}");
                logEntries.Add("");

                // Submit to GPT
                _statusReporter.Report("Thinking...");
                var completionResult = await client.CompleteChatAsync(messages, options);
                var completion = completionResult.Value;

                // Log GPT response
                logEntries.Add($"=== GPT Response ===");
                logEntries.Add($"Content: {completion.Content.FirstOrDefault()?.Text ?? "No content"}");
                logEntries.Add($"Finish Reason: {completion.FinishReason}");
                logEntries.Add($"Tool Calls: {completion.ToolCalls.Count}");
                logEntries.Add("");

                _statusReporter.Report(completion.Content.FirstOrDefault()?.Text ?? "No content");

                // Handle the response
                switch (completion.FinishReason)
                {
                    case ChatFinishReason.Stop:
                        logEntries.Add("=== AI decided to stop ===");
                        logEntries.Add("Reason: Task completed successfully");
                        continueLoop = false;
                        break;

                    case ChatFinishReason.ToolCalls:
                        // Add assistant message with tool calls
                        messages.Add(new AssistantChatMessage(completion));

                        // Process each tool call
                        foreach (var toolCall in completion.ToolCalls)
                        {
                            var toolResult = await ProcessToolCall(toolCall, storageFolder, logEntries);
                            messages.Add(new ToolChatMessage(toolCall.Id, toolResult));
                        }

                        // Take screenshot after actions
                        if (
                            completion.ToolCalls.Any(tc =>
                                tc.FunctionName == "mouse_click"
                                || tc.FunctionName == "key_press"
                                || tc.FunctionName == "type"
                            )
                        )
                        {
                            await Task.Delay(1000); // Wait 1 second
                            var newScreenshots = TakeAndSaveScreenshots(storageFolder, null);
                            primaryScreenshot = newScreenshots.Primary;
                        }
                        break;

                    case ChatFinishReason.Length:
                        logEntries.Add("=== Stopped due to length limit ===");
                        continueLoop = false;
                        break;

                    default:
                        logEntries.Add($"=== Stopped due to: {completion.FinishReason} ===");
                        continueLoop = false;
                        break;
                }
            }

            if (iteration >= maxIterations)
            {
                logEntries.Add("=== Stopped due to maximum iterations reached ===");
            }
        }
        catch (OperationCanceledException)
        {
            logEntries.Add("=== User cancelled the operation ===");
            throw;
        }
        catch (Exception ex)
        {
            logEntries.Add($"=== Error occurred: {ex.Message} ===");
            throw;
        }
        finally
        {
            // Write log file
            await File.WriteAllLinesAsync(outputFile.FullName, logEntries);
        }
    }

    private static ChatMessage CreateContextMessage(
        string promptText,
        FileInfo screenshot,
        WindowInfo? focusedWindow,
        List<WindowInfo> unfocusedWindows,
        FileInfo? overviewScreenshot = null
    )
    {
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine($"Goal: {promptText}");
        contextBuilder.AppendLine($"Current Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        contextBuilder.AppendLine($"Focused Window: {focusedWindow?.Title ?? "None"}");
        contextBuilder.AppendLine($"Other Windows: {string.Join(", ", unfocusedWindows.Select(w => w.Title))}");
        contextBuilder.AppendLine();
        contextBuilder.AppendLine("Please analyze the screenshot and decide what action to take next.");
        contextBuilder.AppendLine("You can use the following tools:");
        contextBuilder.AppendLine("- screenshot: Take a new screenshot, optionally with a zoom path");
        contextBuilder.AppendLine(
            "- mouse_click: Click at a specific location using grid coordinates (requires at least 2 coordinates for accuracy)"
        );
        contextBuilder.AppendLine("- key_press: Press a key combination");
        contextBuilder.AppendLine("- type: Type text");
        contextBuilder.AppendLine();
        contextBuilder.AppendLine("If you believe the task is complete, respond without calling any tools.");

        // Read screenshot as base64
        byte[] imageBytes = File.ReadAllBytes(screenshot.FullName);

        var content = new List<ChatMessageContentPart>
        {
            ChatMessageContentPart.CreateTextPart(contextBuilder.ToString()),
            ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(imageBytes), "image/png"),
        };

        // Add overview screenshot if provided (for zoomed screenshots)
        if (overviewScreenshot != null)
        {
            byte[] overviewImageBytes = File.ReadAllBytes(overviewScreenshot.FullName);
            content.Add(ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(overviewImageBytes), "image/png"));
        }

        return new UserChatMessage(content);
    }

    private async Task<string> ProcessToolCall(
        ChatToolCall toolCall,
        StorageFolder storageFolder,
        List<string> logEntries
    )
    {
        logEntries.Add($"=== Executing Tool: {toolCall.FunctionName} ===");
        logEntries.Add($"Arguments: {toolCall.FunctionArguments}");

        try
        {
            return toolCall.FunctionName switch
            {
                "screenshot" => await ProcessScreenshotTool(toolCall, storageFolder),
                "mouse_click" => await ProcessMouseClickTool(toolCall),
                "key_press" => await ProcessKeyPressTool(toolCall),
                "type" => await ProcessTypeTool(toolCall),
                _ => $"Unknown tool: {toolCall.FunctionName}",
            };
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error executing {toolCall.FunctionName}: {ex.Message}";
            logEntries.Add($"Error: {errorMessage}");
            return errorMessage;
        }
    }

    private async Task<string> ProcessScreenshotTool(ChatToolCall toolCall, StorageFolder storageFolder)
    {
        using var argsDoc = JsonDocument.Parse(toolCall.FunctionArguments);
        var root = argsDoc.RootElement;

        ZoomPath? zoomPath = null;
        if (root.TryGetProperty("zoomPath", out var zoomPathElement))
        {
            var zoomPathString = zoomPathElement.GetString();
            if (!string.IsNullOrEmpty(zoomPathString))
            {
                var coordStrings = zoomPathString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var coords = new List<Coord>();
                foreach (var coordStr in coordStrings)
                {
                    coords.Add(Coord.Parse(coordStr.Trim()));
                }
                zoomPath = new ZoomPath(coords);
            }
        }

        var screenshotFiles = await Task.Run(() => TakeAndSaveScreenshots(storageFolder, zoomPath));

        if (screenshotFiles.Overview != null)
        {
            return $"Screenshots taken: {screenshotFiles.Primary.Name} (zoomed) and {screenshotFiles.Overview.Name} (overview with highlighted target area)";
        }
        else
        {
            return $"Screenshot taken: {screenshotFiles.Primary.Name}";
        }
    }

    private async Task<string> ProcessMouseClickTool(ChatToolCall toolCall)
    {
        using var argsDoc = JsonDocument.Parse(toolCall.FunctionArguments);
        var root = argsDoc.RootElement;

        var zoomPathString = root.GetProperty("zoomPath").GetString();
        if (string.IsNullOrEmpty(zoomPathString))
            throw new ArgumentException("zoomPath is required");

        var coordStrings = zoomPathString.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var coords = new List<Coord>();
        foreach (var coordStr in coordStrings)
        {
            coords.Add(Coord.Parse(coordStr.Trim()));
        }

        // Require at least 2 coordinates for accurate clicking
        if (coords.Count < 2)
        {
            return "Error: Mouse clicking requires at least 2 coordinates in the zoomPath for accuracy. "
                + "Please take a screenshot first to zoom into the target area, then click using the zoomed coordinates. "
                + "Example: First use screenshot with zoomPath 'A1', then use mouse_click with zoomPath 'A1,B2'.";
        }

        var zoomPath = new ZoomPath(coords);

        var buttonString = root.TryGetProperty("button", out var buttonElement) ? buttonElement.GetString() : "left";
        var doubleClick = root.TryGetProperty("double", out var doubleElement) && doubleElement.GetBoolean();

        var mouseButton = buttonString?.ToLowerInvariant() switch
        {
            "left" => MouseButtons.Left,
            "right" => MouseButtons.Right,
            "middle" => MouseButtons.Middle,
            _ => MouseButtons.Left,
        };

        await Task.Run(() => _mouseUse.Click(zoomPath, mouseButton, doubleClick));

        return $"Mouse click performed: {buttonString} at {zoomPathString}" + (doubleClick ? " (double-click)" : "");
    }

    private async Task<string> ProcessKeyPressTool(ChatToolCall toolCall)
    {
        using var argsDoc = JsonDocument.Parse(toolCall.FunctionArguments);
        var root = argsDoc.RootElement;

        var keyString = root.GetProperty("key").GetString();
        if (string.IsNullOrEmpty(keyString))
            throw new ArgumentException("key is required");

        if (!Enum.TryParse<Keys>(keyString, true, out var key))
            throw new ArgumentException($"Invalid key: {keyString}");

        // Parse modifiers
        var modifiers = Keys.None;
        if (root.TryGetProperty("shift", out var shiftElement) && shiftElement.GetBoolean())
            modifiers |= Keys.Shift;
        if (root.TryGetProperty("ctrl", out var ctrlElement) && ctrlElement.GetBoolean())
            modifiers |= Keys.Control;
        if (root.TryGetProperty("alt", out var altElement) && altElement.GetBoolean())
            modifiers |= Keys.Alt;

        var combinedKey = key | modifiers;

        await Task.Run(() => _keyboardUse.Press(combinedKey));

        return $"Key pressed: {keyString}" + (modifiers != Keys.None ? $" with modifiers: {modifiers}" : "");
    }

    private async Task<string> ProcessTypeTool(ChatToolCall toolCall)
    {
        using var argsDoc = JsonDocument.Parse(toolCall.FunctionArguments);
        var root = argsDoc.RootElement;

        var text = root.GetProperty("text").GetString();
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("text is required");

        await Task.Run(() => _keyboardUse.Type(text));

        return $"Text typed: {text}";
    }

    private static ChatTool CreateScreenshotTool()
    {
        return ChatTool.CreateFunctionTool(
            functionName: "screenshot",
            functionDescription: "Take a screenshot of the current screen state",
            functionParameters: BinaryData.FromBytes(
                """
                {
                    "type": "object",
                    "properties": {
                        "zoomPath": {
                            "type": "string",
                            "description": "Optional comma-separated grid coordinates to zoom into (e.g., 'A1,B2')"
                        }
                    }
                }
                """u8.ToArray()
            )
        );
    }

    private static ChatTool CreateMouseClickTool()
    {
        return ChatTool.CreateFunctionTool(
            functionName: "mouse_click",
            functionDescription: "Click the mouse at a specific location using grid coordinates. IMPORTANT: Requires at least 2 coordinates in zoomPath for accuracy.",
            functionParameters: BinaryData.FromBytes(
                """
                {
                    "type": "object",
                    "properties": {
                        "zoomPath": {
                            "type": "string",
                            "description": "Comma-separated grid coordinates specifying the click location. MUST contain at least 2 coordinates for accuracy (e.g., 'A1,B2'). Take a screenshot first to zoom in, then click using the zoomed coordinates."
                        },
                        "button": {
                            "type": "string",
                            "enum": ["left", "right", "middle"],
                            "description": "Which mouse button to click",
                            "default": "left"
                        },
                        "double": {
                            "type": "boolean",
                            "description": "Whether to perform a double-click",
                            "default": false
                        }
                    },
                    "required": ["zoomPath"]
                }
                """u8.ToArray()
            )
        );
    }

    private static ChatTool CreateKeyPressTool()
    {
        return ChatTool.CreateFunctionTool(
            functionName: "key_press",
            functionDescription: "Press a key or key combination",
            functionParameters: BinaryData.FromBytes(
                """
                {
                    "type": "object",
                    "properties": {
                        "key": {
                            "type": "string",
                            "description": "The key to press (e.g., 'Enter', 'Escape', 'F1', 'A')"
                        },
                        "shift": {
                            "type": "boolean",
                            "description": "Whether to hold Shift while pressing the key",
                            "default": false
                        },
                        "ctrl": {
                            "type": "boolean",
                            "description": "Whether to hold Ctrl while pressing the key",
                            "default": false
                        },
                        "alt": {
                            "type": "boolean",
                            "description": "Whether to hold Alt while pressing the key",
                            "default": false
                        }
                    },
                    "required": ["key"]
                }
                """u8.ToArray()
            )
        );
    }

    private static ChatTool CreateTypeTool()
    {
        return ChatTool.CreateFunctionTool(
            functionName: "type",
            functionDescription: "Type text into the current focused input",
            functionParameters: BinaryData.FromBytes(
                """
                {
                    "type": "object",
                    "properties": {
                        "text": {
                            "type": "string",
                            "description": "The text to type"
                        }
                    },
                    "required": ["text"]
                }
                """u8.ToArray()
            )
        );
    }

    private const string SystemPrompt = """
        You are an AI assistant that can control a Windows computer to accomplish tasks.

        You will be given:
        1. A goal or task to accomplish
        2. A screenshot of the current desktop state  
        3. Information about currently open windows
        4. A set of tools you can use to interact with the computer

        Your tools are:
        - screenshot: Take a new screenshot, optionally zooming into specific grid coordinates
        - mouse_click: Click at a location specified by grid coordinates on the screenshot
        - key_press: Press keyboard keys with optional modifiers (Shift, Ctrl, Alt)
        - type: Type text into the currently focused input

        The screenshots have a grid overlay with coordinates. Use these coordinates to specify where to click.
        Each grid cell is labeled with a coordinate like A1, B2, etc.

        When you request a zoomed-in screenshot, you will receive two images:
        1. The zoomed-in view with grid coordinates for precise targeting
        2. A fullscreen overview with the target area highlighted in magenta to show context

        IMPORTANT: For mouse clicking, you MUST provide at least 2 coordinates in the zoomPath for accuracy. 
        Never click directly from a fullscreen screenshot as it's too inaccurate. Always zoom in first by taking 
        a screenshot with a zoomPath, then click using the zoomed coordinates. For example: First use screenshot 
        with zoomPath 'A1', then use mouse_click with zoomPath 'A1,B2'.

        Process:
        1. Analyze the screenshot to understand the current state
        2. Determine what action is needed to progress toward the goal
        3. Use the appropriate tool to perform that action
        4. After actions that might change the screen, the system will automatically take a new screenshot
        5. Continue until the task is complete

        If you believe the task is complete or cannot be completed, respond with a message explaining the outcome without calling any tools.

        Be precise with your coordinates and actions. Take your time to analyze the screenshot carefully before acting.
        """;
}
