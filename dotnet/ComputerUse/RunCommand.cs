using System.Drawing;
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

    // Internal zoom state - this replaces the AI-visible zoom path concept
    private List<Coord> _currentZoomPath = new();

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

    public async Task ExecuteAsync()
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

        // Take initial screenshot using current zoom state
        ZoomPath? currentZoomPath = _currentZoomPath.Count > 0 ? new ZoomPath(_currentZoomPath) : null;
        var initialScreenshots = TakeAndSaveScreenshots(storageFolder, currentZoomPath);
        var primaryScreenshot = initialScreenshots.Primary;

        // Initialize conversation
        var messages = new List<ChatMessage> { new SystemChatMessage(SystemPrompt) };

        // Helper method to write log entries immediately
        async Task WriteLogAsync(params string[] entries)
        {
            await File.AppendAllLinesAsync(outputFile.FullName, entries);
        }

        // Define available tools
        var tools = new List<ChatTool>
        {
            CreateZoomInTool(),
            CreateZoomOutTool(),
            CreateZoomFullscreenTool(),
            CreatePanTool(),
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
                    _currentZoomPath,
                    initialScreenshots.Overview
                );
                messages.Add(contextMessage);

                // Log the user prompt immediately
                await WriteLogAsync(
                    $"=== Iteration {iteration} - User Prompt ===",
                    $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    $"Focused Window: {focusedWindow?.Title ?? "None"}",
                    $"Unfocused Windows: {string.Join(", ", unfocusedWindows.Select(w => w.Title))}",
                    $"Screenshot: {primaryScreenshot.Name}",
                    ""
                );

                // Submit to GPT
                _statusReporter.Report("Thinking...");
                var completionResult = await client.CompleteChatAsync(messages, options);
                var completion = completionResult.Value;

                // Log GPT response immediately
                await WriteLogAsync(
                    $"=== GPT Response ===",
                    $"Content: {completion.Content.FirstOrDefault()?.Text ?? "No content"}",
                    $"Finish Reason: {completion.FinishReason}",
                    $"Tool Calls: {completion.ToolCalls.Count}",
                    ""
                );

                _statusReporter.Report(completion.Content.FirstOrDefault()?.Text ?? "No content");

                // Handle the response
                switch (completion.FinishReason)
                {
                    case ChatFinishReason.Stop:
                        await WriteLogAsync("=== AI decided to stop ===", "Reason: Task completed successfully");
                        continueLoop = false;
                        break;

                    case ChatFinishReason.ToolCalls:
                        // Add assistant message with tool calls
                        messages.Add(new AssistantChatMessage(completion));

                        // Track if we need to delay before taking screenshot
                        bool shouldDelay = false;

                        // Process each tool call
                        foreach (var toolCall in completion.ToolCalls)
                        {
                            var toolResult = await ProcessToolCall(toolCall, storageFolder, outputFile);
                            messages.Add(new ToolChatMessage(toolCall.Id, toolResult));

                            // Check if this tool requires a delay (actions that change the screen)
                            if (toolCall.FunctionName is "mouse_click" or "key_press" or "type")
                            {
                                shouldDelay = true;
                            }
                        }

                        // Add delay only after screen-changing actions, not zoom operations
                        if (shouldDelay)
                        {
                            await Task.Delay(1000); // Wait 1 second for screen reaction
                        }

                        // Take screenshot after processing all tool calls
                        ZoomPath? updatedZoomPath = _currentZoomPath.Count > 0 ? new ZoomPath(_currentZoomPath) : null;
                        var newScreenshots = TakeAndSaveScreenshots(storageFolder, updatedZoomPath);
                        primaryScreenshot = newScreenshots.Primary;
                        break;

                    case ChatFinishReason.Length:
                        await WriteLogAsync("=== Stopped due to length limit ===");
                        continueLoop = false;
                        break;

                    default:
                        await WriteLogAsync($"=== Stopped due to: {completion.FinishReason} ===");
                        continueLoop = false;
                        break;
                }
            }

            if (iteration >= maxIterations)
            {
                await WriteLogAsync("=== Stopped due to maximum iterations reached ===");
            }
        }
        catch (OperationCanceledException)
        {
            await WriteLogAsync("=== User cancelled the operation ===");
            throw;
        }
        catch (Exception ex)
        {
            await WriteLogAsync($"=== Error occurred: {ex.Message} ===");
            throw;
        }
    }

    private static ChatMessage CreateContextMessage(
        string promptText,
        FileInfo screenshot,
        WindowInfo? focusedWindow,
        List<WindowInfo> unfocusedWindows,
        List<Coord> currentZoomPath,
        FileInfo? overviewScreenshot = null
    )
    {
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine($"Goal: {promptText}");
        contextBuilder.AppendLine($"Current Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        contextBuilder.AppendLine($"Focused Window: {focusedWindow?.Title ?? "None"}");
        contextBuilder.AppendLine($"Other Windows: {string.Join(", ", unfocusedWindows.Select(w => w.Title))}");
        contextBuilder.AppendLine();

        // Add current zoom state information
        if (currentZoomPath.Count == 0)
        {
            contextBuilder.AppendLine("Current Zoom: Fullscreen view");
        }
        else
        {
            var zoomPathString = string.Join(",", currentZoomPath.Select(c => c.ToString()));
            contextBuilder.AppendLine($"Current Zoom: {zoomPathString}");
        }

        // Add click eligibility information
        if (currentZoomPath.Count < 2)
        {
            var moreZoomsNeeded = 2 - currentZoomPath.Count;
            contextBuilder.AppendLine(
                $"Click Status: You need to zoom in {moreZoomsNeeded} more time(s) before you can click for accuracy."
            );
        }
        else
        {
            contextBuilder.AppendLine(
                "Click Status: You are zoomed in enough to click accurately at the center of the current view."
            );
        }
        contextBuilder.AppendLine();

        // Calculate grid ranges for the primary screenshot
        using (var img = Image.FromFile(screenshot.FullName))
        {
            var aspectRatio = (double)img.Width / img.Height;
            var numColumns = Coord.CalculateColumns(aspectRatio);
            var numRows = Coord.NUM_ROWS;

            var maxColumn = (char)('A' + numColumns - 1);
            var maxRow = numRows - 1;

            if (overviewScreenshot != null)
            {
                // Dual screenshot mode - explain both images
                contextBuilder.AppendLine("Two screenshots provided:");
                contextBuilder.AppendLine(
                    $"1. Zoomed-in screenshot: Grid ranges A-{maxColumn}, 0-{maxRow} - Use these coordinates for precise targeting"
                );
                contextBuilder.AppendLine(
                    "2. Overview screenshot: Shows the zoomed area (highlighted in magenta) within the full desktop context"
                );

                // Calculate overview grid ranges
                using (var overviewImg = Image.FromFile(overviewScreenshot.FullName))
                {
                    var overviewAspectRatio = (double)overviewImg.Width / overviewImg.Height;
                    var overviewNumColumns = Coord.CalculateColumns(overviewAspectRatio);
                    var overviewMaxColumn = (char)('A' + overviewNumColumns - 1);
                    var overviewMaxRow = Coord.NUM_ROWS - 1;

                    contextBuilder.AppendLine($"   Overview grid ranges: A-{overviewMaxColumn}, 0-{overviewMaxRow}");
                }
            }
            else
            {
                // Single screenshot mode
                contextBuilder.AppendLine($"Screenshot grid ranges: A-{maxColumn}, 0-{maxRow}");
            }
        }

        contextBuilder.AppendLine();
        contextBuilder.AppendLine("Please analyze the screenshot and decide what action to take next.");
        contextBuilder.AppendLine("You can use the following tools:");
        contextBuilder.AppendLine("- zoom_in: Zoom into a specific grid coordinate");
        contextBuilder.AppendLine("- zoom_out: Zoom out one level");
        contextBuilder.AppendLine("- zoom_fullscreen: Return to fullscreen view");
        contextBuilder.AppendLine("- pan: Move the current zoom view by grid offsets");
        contextBuilder.AppendLine(
            "- mouse_click: Click at the center of the current zoom view (requires sufficient zoom level)"
        );
        contextBuilder.AppendLine("- key_press: Press a key combination");
        contextBuilder.AppendLine("- type: Type text");
        contextBuilder.AppendLine();
        contextBuilder.AppendLine("Screenshots are automatically provided after each action.");
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

    private async Task<string> ProcessToolCall(ChatToolCall toolCall, StorageFolder storageFolder, FileInfo outputFile)
    {
        // Helper method to write log entries immediately
        async Task WriteLogAsync(params string[] entries)
        {
            await File.AppendAllLinesAsync(outputFile.FullName, entries);
        }

        await WriteLogAsync($"=== Executing Tool: {toolCall.FunctionName} ===");
        await WriteLogAsync($"Arguments: {toolCall.FunctionArguments}");

        try
        {
            return toolCall.FunctionName switch
            {
                "zoom_in" => await ProcessZoomInTool(toolCall),
                "zoom_out" => await ProcessZoomOutTool(toolCall),
                "zoom_fullscreen" => await ProcessZoomFullscreenTool(toolCall),
                "pan" => await ProcessPanTool(toolCall),
                "mouse_click" => await ProcessMouseClickTool(toolCall),
                "key_press" => await ProcessKeyPressTool(toolCall),
                "type" => await ProcessTypeTool(toolCall),
                _ => $"Unknown tool: {toolCall.FunctionName}",
            };
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error executing {toolCall.FunctionName}: {ex.Message}";
            await WriteLogAsync($"Error: {errorMessage}");
            return errorMessage;
        }
    }

    private Task<string> ProcessZoomInTool(ChatToolCall toolCall)
    {
        using var argsDoc = JsonDocument.Parse(toolCall.FunctionArguments);
        var root = argsDoc.RootElement;

        var coordString = root.GetProperty("coord").GetString();
        if (string.IsNullOrEmpty(coordString))
            throw new ArgumentException("coord is required");

        var coord = Coord.Parse(coordString);
        _currentZoomPath.Add(coord);

        var zoomPathString = string.Join(",", _currentZoomPath.Select(c => c.ToString()));
        return Task.FromResult($"Zoomed in to {coordString}. Current zoom path: {zoomPathString}");
    }

    private Task<string> ProcessZoomOutTool(ChatToolCall toolCall)
    {
        if (_currentZoomPath.Count == 0)
        {
            return Task.FromResult("Already at fullscreen - cannot zoom out further.");
        }

        var removedCoord = _currentZoomPath[^1];
        _currentZoomPath.RemoveAt(_currentZoomPath.Count - 1);

        var zoomPathString =
            _currentZoomPath.Count > 0 ? string.Join(",", _currentZoomPath.Select(c => c.ToString())) : "fullscreen";
        return Task.FromResult($"Zoomed out from {removedCoord}. Current zoom path: {zoomPathString}");
    }

    private Task<string> ProcessZoomFullscreenTool(ChatToolCall toolCall)
    {
        var previousZoomPath = string.Join(",", _currentZoomPath.Select(c => c.ToString()));
        _currentZoomPath.Clear();

        return Task.FromResult($"Returned to fullscreen view. Previous zoom path was: {previousZoomPath}");
    }

    private Task<string> ProcessPanTool(ChatToolCall toolCall)
    {
        if (_currentZoomPath.Count == 0)
        {
            return Task.FromResult("Cannot pan - currently at fullscreen view. Use zoom_in first.");
        }

        using var argsDoc = JsonDocument.Parse(toolCall.FunctionArguments);
        var root = argsDoc.RootElement;

        var verticalOffset = root.TryGetProperty("vertical", out var vertElement) ? vertElement.GetInt32() : 0;
        var horizontalOffset = root.TryGetProperty("horizontal", out var horizElement) ? horizElement.GetInt32() : 0;

        if (verticalOffset == 0 && horizontalOffset == 0)
        {
            return Task.FromResult("No movement - both vertical and horizontal offsets are zero.");
        }

        // Modify the last coordinate in the zoom path
        var lastCoord = _currentZoomPath[^1];
        var newRow = Math.Max(0, Math.Min(Coord.NUM_ROWS - 1, lastCoord.RowIndex + verticalOffset));
        var newCol = Math.Max(0, lastCoord.ColumnIndex + horizontalOffset); // Column limit depends on aspect ratio

        var newCoord = new Coord(newRow, newCol);
        _currentZoomPath[^1] = newCoord;

        var zoomPathString = string.Join(",", _currentZoomPath.Select(c => c.ToString()));
        return Task.FromResult($"Panned by ({horizontalOffset},{verticalOffset}). Current zoom path: {zoomPathString}");
    }

    private async Task<string> ProcessMouseClickTool(ChatToolCall toolCall)
    {
        // Check if we have sufficient zoom level for accurate clicking
        if (_currentZoomPath.Count < 2)
        {
            return "Error: You must zoom in at least 2 levels before clicking for accuracy. "
                + "Use zoom_in to zoom into target areas before attempting to click.";
        }

        using var argsDoc = JsonDocument.Parse(toolCall.FunctionArguments);
        var root = argsDoc.RootElement;

        var buttonString = root.TryGetProperty("button", out var buttonElement) ? buttonElement.GetString() : "left";
        var doubleClick = root.TryGetProperty("double", out var doubleElement) && doubleElement.GetBoolean();

        var mouseButton = buttonString?.ToLowerInvariant() switch
        {
            "left" => MouseButtons.Left,
            "right" => MouseButtons.Right,
            "middle" => MouseButtons.Middle,
            _ => MouseButtons.Left,
        };

        // Use current zoom path for clicking at the implicit center
        var zoomPath = new ZoomPath(_currentZoomPath);
        var zoomPathString = string.Join(",", _currentZoomPath.Select(c => c.ToString()));

        await Task.Run(() => _mouseUse.Click(zoomPath, mouseButton, doubleClick));

        // Reset zoom path to fullscreen after clicking
        _currentZoomPath.Clear();

        return $"Mouse click performed: {buttonString} at center of zoom level {zoomPathString}"
            + (doubleClick ? " (double-click)" : "")
            + ". Zoom has been reset to fullscreen.";
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

    private static ChatTool CreateZoomInTool()
    {
        return ChatTool.CreateFunctionTool(
            functionName: "zoom_in",
            functionDescription: "Zoom into a specific grid coordinate",
            functionParameters: BinaryData.FromBytes(
                """
                {
                    "type": "object",
                    "properties": {
                        "coord": {
                            "type": "string",
                            "description": "Grid coordinate to zoom into (e.g., 'A1', 'B2')"
                        }
                    },
                    "required": ["coord"]
                }
                """u8.ToArray()
            )
        );
    }

    private static ChatTool CreateZoomOutTool()
    {
        return ChatTool.CreateFunctionTool(
            functionName: "zoom_out",
            functionDescription: "Zoom out one level from the current zoom state",
            functionParameters: BinaryData.FromBytes(
                """
                {
                    "type": "object",
                    "properties": {}
                }
                """u8.ToArray()
            )
        );
    }

    private static ChatTool CreateZoomFullscreenTool()
    {
        return ChatTool.CreateFunctionTool(
            functionName: "zoom_fullscreen",
            functionDescription: "Return to fullscreen view, clearing all zoom levels",
            functionParameters: BinaryData.FromBytes(
                """
                {
                    "type": "object",
                    "properties": {}
                }
                """u8.ToArray()
            )
        );
    }

    private static ChatTool CreatePanTool()
    {
        return ChatTool.CreateFunctionTool(
            functionName: "pan",
            functionDescription: "Move the current zoom view by the specified grid cell offsets",
            functionParameters: BinaryData.FromBytes(
                """
                {
                    "type": "object",
                    "properties": {
                        "vertical": {
                            "type": "integer",
                            "description": "Vertical offset in grid cells (positive = down, negative = up)"
                        },
                        "horizontal": {
                            "type": "integer", 
                            "description": "Horizontal offset in grid cells (positive = right, negative = left)"
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
            functionDescription: "Click the mouse at the center of the current zoom view. Requires at least 2 zoom levels for accuracy.",
            functionParameters: BinaryData.FromBytes(
                """
                {
                    "type": "object",
                    "properties": {
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
                    }
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
        2. A screenshot of the current desktop state with your current zoom level
        3. Information about currently open windows and your current zoom state
        4. A set of tools you can use to interact with the computer

        Your tools are:
        - zoom_in: Zoom into a specific grid coordinate to get a closer view
        - zoom_out: Zoom out one level to see more of the screen
        - zoom_fullscreen: Return to fullscreen view
        - pan: Move the current zoom view by grid cell offsets
        - mouse_click: Click at the center of your current zoom view (requires at least 2 zoom levels)
        - key_press: Press keyboard keys with optional modifiers (Shift, Ctrl, Alt)
        - type: Type text into the currently focused input

        The screenshots have a grid overlay with coordinates. Each grid cell is labeled with a coordinate like A1, B2, etc.

        ZOOM STATE MANAGEMENT:
        - You maintain an internal zoom state that persists across interactions
        - When zoomed in, you will see two images: the zoomed view and an overview showing context
        - Use zoom_in to drill down to specific areas you want to target
        - Use zoom_out or zoom_fullscreen to see more of the screen when needed
        - Use pan to adjust your current view without changing zoom level

        CLICKING ACCURACY:
        - You MUST zoom in at least 2 levels before clicking for accuracy
        - The system will tell you when you're zoomed in enough to click
        - Clicks always target the center of your current zoom view
        - If you need to click somewhere else, zoom out and zoom back in to that location

        AUTOMATIC SCREENSHOTS:
        - Screenshots are automatically provided after every action
        - You don't need to request screenshots - they happen automatically
        - Focus on using zoom tools to navigate and position yourself for precise actions

        Process:
        1. Analyze the current screenshot and zoom state
        2. Navigate using zoom tools to target the area you need to interact with
        3. Once properly positioned and zoomed in, perform actions (click, type, key press)
        4. Continue until the task is complete

        If you believe the task is complete or cannot be completed, respond with a message explaining the outcome without calling any tools.

        Be methodical with your zoom navigation. Take your time to position yourself correctly before acting.
        """;
}
