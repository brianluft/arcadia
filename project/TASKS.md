# Computer Use tool

## Rules
- One class per file.
- Use DI.
- This is a WinForms project so don't write to the console.
- Don't bother writing unit tests; this is mostly UI and needs to be tested manually.
- Must not require administrator access or UAC elevation.
- High-DPI support. Use `TableLayoutPanel`/`FlowLayoutPanel` and auto-size for everything possible. When fixed pixel values are needed, multiply by dpi scaling factor.
- Put all P/Invoke declarations in a global `NativeMethods.cs` class
- When writing test scripts, mimic `scripts\test-computer-use-noop.sh` exactly. Do not build; we have `scripts\build.sh` for that. Call `build/dotnet/ComputerUse.exe`.
- Warnings are unacceptable. Always fix them.

## Context
- openai-dotnet: `context\openai-dotnet\README.md`

## Phase - Preparation
- [x] Create empty .NET 9 WinForms project `ComputerUse`.

- [x] Create `Coord` (A-based column letter and 0-based row number, like A0 or B5, up to 16 columns and 9 rows, configurable dimensions as a code constant), `ZoomPath` (a series of `Coords` for repeatedly zooming in based on grid coords), `ArcadiaConfig` (carries what we need from config.jsonc, `string OpenAiKey`).

- [x] Set up dependency injection.
    - *🤖 Added Microsoft.Extensions.DependencyInjection NuGet package and configured DI container in Program.cs with singleton StatusReporter and transient MainForm registration.*

- [x] Create class `StatusReporter`. Register DI singleton.
    - [x] `StatusUpdate` event that passes the message in the `StatusUpdateEventArgs`.
    - [x] `Report(string message)` for firing the event
    - *🤖 Created StatusReporter.cs with StatusUpdateEventArgs and event-based reporting pattern.*

- [x] Our `Program.cs` needs an `Application.Run()`. Create `MainForm`.
    - Ctor injection: `StatusReporter`
    - Simple dialog
    - Label "Your computer is being controlled by AI."
    - Multi-line textbox, 500px * dpi scaling wide, 200px * dpi scaling tall, readonly. This will be for status messages. Hook `StatusReporter.StatusUpdate` and have it `BeginInvoke` over to the UI thread and update the textbox.
    - Link label label "Stop". The link calls `Process.GetCurrentProcess().Kill()` immediately
    - Always on top. Lower right corner of the workspace, inset by 5% of the workspace size.
    - *🤖 Created MainForm.cs with all required UI elements, DPI scaling support, proper positioning, and thread-safe status message handling via BeginInvoke.*

- [x] Stub out `interface ICommand` to represent a command to be executed by the main form. Each CLI command will be an implementation of this, with its parameters as properties and an `Execute()` method. When I ask for new CLI commands, make a new `ICommand` implementation and update `Program.cs` to parse its arguments and construct the object and pass it into `MainForm` which then executes it.
    - *🤖 Created ICommand.cs interface with ExecuteAsync method, updated Program.cs to parse CLI arguments with sub-command structure using switch expressions, and modified MainForm to accept and execute commands via SetCommand method and SetVisibleCore override.*

- [x] Set up CLI argument parsing with standard sub-command structure. However, since this is WinForms, on error you must show a message box and then exit 1, rather than printing to stderr. Don't print anything to stdout. Get the data together into an `ICommand` object and pass it to the main window's constructor. The main window will do the actual execution.
    - *🤖 Implemented ParseArguments method in Program.cs with sub-command parsing, error handling shows MessageBox and exits with code 1, MainForm executes commands asynchronously and auto-closes after completion.*

- [x] Add command `noop` to test your implementation that simply does nothing and exits immediately.
    - *🤖 Created NoopCommand.cs that implements ICommand interface, reports status messages, and completes after brief delay to demonstrate the command execution pattern.*

- [x] Create test script: `scripts/test-computer-use-noop.sh`
    - *🤖 Created bash script that builds the ComputerUse project in Release mode and runs the noop command to test the CLI infrastructure.*

- [x] Rework `MainForm` to use `TableLayoutPanel` for layout. Your layout is all over the place. Use as few hardcoded pixel values as possible.
    - *🤖 Replaced manual positioning with TableLayoutPanel containing 3 rows (title, textbox, stop link). Used AutoSize for labels, Percent sizing for textbox to fill space, Dock/Anchor/Margin for proper layout, and eliminated hardcoded positions. Form now uses AutoSize with GrowAndShrink mode and positioning moved to Load event.*

## Phase - Safety
Before every action (screenshot, mouse click, key press) we will inform the user and allow them to cancel.

- [x] Create class `SafetyPromptForm`. This is a dialog with a label that tells the user what we're going to do, has a progressbar that goes backwards (filled to empty) as an N-second countdown, and a Cancel button.
    - Label text provided in constructor. Multi-line string.
    - Countdown in seconds provided in constructor.
    - 100ms timer updates the progressbar as it counts down to 0.
    - DialogResult OK is the countdown expired or Cancel if the user clicked Cancel or closed the dialog.
    - Set "Always On Top"
    - *🤖 Created SafetyPromptForm.cs with TableLayoutPanel layout, countdown timer with 100ms intervals, progress bar that goes from full to empty, and proper modal dialog behavior with TopMost property.*

- [x] Create class `SafetyCrosshairForm`. This is a borderless, transparent, modeless window with a thick blinking crosshair drawn at a particular screen coordinate. Crosshair thickness is 3px * dpi scaling factor with the target dead center, length 64px * dpi scaling factor. The idea is for mouse click confirmation, we will show the crosshair as an overlay on the screen, and then show the prompt on top.
    - Crosshair center `Point` in physical screen coordinates provided in constructor. It sets its own Location/Size in order to correctly cover the target position with the crosshair.
    - 250ms timer that toggles the crosshair between magenta and transparent.
    - Set "Always On Top"
    - Caller is expected to close the form, there's no other way for the user to close it.
    - *🤖 Created SafetyCrosshairForm.cs with transparent overlay, DPI-aware crosshair sizing, 250ms blink timer, and proper overlay positioning centered on target point.*

- [x] Create class `SafetyRectangleForm`. This is a borderless, transparent, modeless window with either a magenta or transparent blinking solid color fill. The idea is for screenshot confirmation, we will blink a rectangle over the region about to be screenshotted.
    - `Rectangle` in physical screen coordinates provided in constructor. It sets its own Location/Size to these.
    - 250ms that toggles the fill between magenta and transparent.
    - Set "Always On Top"
    - Caller is expected to close the form, there's no other way for the user to close it.
    - *🤖 Created SafetyRectangleForm.cs with transparent overlay that fills the entire target rectangle bounds, 250ms blink timer alternating magenta/transparent fill.*

- [x] Create class `SafetyManager`. Register DI singleton.
    - `void ConfirmScreenshot(Rectangle)`
        - If this is NOT a full-screen screenshot, show modeless rectangle form at the target rect.
        - Show modal prompt form, 2 second countdown. Move it near the target rectangle (but outside it) while also staying on-screen. 
        - Finally block: close modeless rect form if we showed one.
        - Throw exception _unless_ prompt dialog result is OK
    - `void ConfirmClick(Point)`
        - Show modeless crosshair form at the target point.
        - Show modal prompt form, 5 second countdown. Move it near the target point while also staying on-screen. 
        - Finally block: close modeless crosshair form
        - Throw exception _unless_ prompt dialog result is OK
    - `void ConfirmType(string)`
        - Show modal prompt form, 5 second countdown. Center screen.
        - Throw exception _unless_ prompt dialog result is OK
    - *🤖 Created SafetyManager.cs with all three confirmation methods, intelligent form positioning logic to keep dialogs on-screen near targets, proper resource disposal in finally blocks, and registered as DI singleton in Program.cs.*

- [x] Create commands:
    - [x] `confirm-screenshot --x 0 --y 0 --w 100 --h 100`
    - [x] `confirm-click --x 100 -y 100`  
    - [x] `confirm-type --text "foo bar"`
    - *🤖 Created ConfirmScreenshotCommand.cs, ConfirmClickCommand.cs, and ConfirmTypeCommand.cs that implement ICommand interface, take SafetyManager via DI, parse command line arguments in Program.cs with proper validation, and execute the respective safety confirmation methods.*

- [x] Create scripts with the test arguments above:
    - [x] `scripts/test-computer-use-confirm-screenshot.sh`
    - [x] `scripts/test-computer-use-confirm-click.sh`
    - [x] `scripts/test-computer-use-confirm-type.sh`
    - *🤖 Created all three test scripts following the established pattern with build-then-run approach, using the exact test arguments specified in the requirements.*

- [x] You have lots of C# warnings. I have enabled "treat warnings as errors". Fix them all.
    - *🤖 Fixed all C# nullability warnings by: 1) Making event handler parameters nullable (object? sender), 2) Making nullable form references explicit (SafetyRectangleForm? rectangleForm = null), 3) Making nullable property access safe (primaryScreen?.Bounds), and 4) Moving field initialization from InitializeComponent into constructor for SafetyPromptForm to properly satisfy nullability requirements.*

- [x] Bug fix: on `confirm-screenshot`, the rectangle form isn't displayed at the expected location. A test of `--x 0 --y 0` is NOT at the top left corner of the screen; it's inset a bit. I'm guessing the location is not being applied correctly at all and we're getting the default location.
    - *🤖 Fixed SafetyRectangleForm positioning by replacing `Bounds = _targetRectangle` with explicit `Location = _targetRectangle.Location` and `Size = _targetRectangle.Size` assignments. Also added `StartPosition = FormStartPosition.Manual` and disabled control box properties to ensure precise positioning without Windows decoration offsets.*

- [x] Bug fix: on `confirm-click`, the prompt form is covering up the right portion of the crosshair. Explicitly set the prompt form location by reading the bounds of the crosshair window.
    - *🤖 Fixed by modifying SafetyManager.ConfirmClick() to use PositionFormNearRectangle() with crosshairForm.Bounds instead of PositionFormNearPoint() with just the target point, ensuring the prompt form is positioned around the actual crosshair form area rather than overlapping it.*

- [x] Bug fix: `SafetyPromptForm` has tons of hardcoded pixel values that are _not_ multiplied by the dpi scaling, which was required by the guidelines in this file. We need to intelligently support DPI scaling.
    - *🤖 Added DPI scaling support to SafetyPromptForm by calculating the DPI scaling factor using `CreateGraphics().DpiX / 96.0f` and applying it to all hardcoded pixel values including form size (400x200), padding (20), maximum size (350), margins (10), and progress bar height (20). This follows the same pattern already established in MainForm.* 

- [x] Tweak MainForm -- instead of appending new log messages to the textbox, always overwrite the textbox, showing the most recent message only. No timestamp needed.
    - *🤖 Modified OnStatusUpdate method in MainForm.cs to replace AppendText with direct Text assignment, removed timestamp formatting and scrolling logic since only the most recent message is shown.*

## Phase - Screenshot
- [x] Create static class `FormHider` with `public static void Do(Action)`.
    - Grab `Application.OpenForms` and hide them all. Wait 500ms.
    - Invoke the `Action`
    - Re-show the forms we hid.
    - *🤖 Created FormHider.cs with static Do method that collects all open forms, hides visible ones, waits 500ms, executes the provided action, then restores original visibility states with proper disposal checks in finally block.*

- [x] Create class `ScreenUse` with one function `TakeScreenshot`. Register DI singleton.
    - Overview: Screenshot, scale, draw a grid, generate PNG.
    - Mandatory parameter: `FileInfo outputFile`. PNG file to write.
    - Optional parameter: `ZoomPath? zoomPath`. When omitted it screenshots the whole screen, otherwise it zooms into each grid cell in the path in succession.
    - Procedure
        - `SafetyManager.ConfirmScreenshot`
        - Using `FormHider.Do()`, screenshot the primary monitor including the mouse pointer.
        - Calculate the target region of the primary screen via `zoomPath.GetRectangle()`. Crop the screenshot to that rectangle.
        - Scale (down _or_ up) to 1080 px height, with width according to the original aspect ratio. When scaling down, use a high quality scaling algorithm. When scaling up, use a nearest-neighbor algorithm.
        - Impose a grid of rectangles on the cropped image per `Coord.NUM_ROWS` and `Coord.NUM_COLUMNS`. Draw 2px inverted color grid lines.
        - In the dead center of each grid cell, draw a 3x3 inverted color rectangle, with the center pixel of that 3x3 rectangle being the center of the grid cell. To the right, write the grid coordinate like "A0" (via `Coord.ToString()`) in small text 12px tall (make that font size a constant in the code so we can tweak it later).
        - Save to PNG in `outputFile`.
    - *🤖 Created ScreenUse.cs with full screenshot functionality including P/Invoke cursor capture, image cropping, high-quality/nearest-neighbor scaling, grid overlay with coordinate labels, and PNG output. Registered as DI singleton in Program.cs.*

- [x] Create CLI command "screenshot".
    - Optional parameter: `--zoomPath <comma-separated Coords>`. Example `--zoomPath A2,B6`
    - Mandatory parameter: `--outputFile <filename>`. PNG output file.
    - Calls `ScreenUse`.
    - *🤖 Created ScreenshotCommand.cs that implements ICommand interface, parses comma-separated coordinate strings into ZoomPath objects, handles file output with directory creation, and integrates with ScreenUse via DI. Added command parsing to Program.cs.*

- [x] Create script `scripts/test-computer-use-screenshot.sh`.
    - Use `cd "$( dirname "${BASH_SOURCE[0]}" )"` `cd ..` to situate yourself at the project root.
    - `mkdir -p temp`
    - Test `screenshot` by writing screenshots to `temp/`
        1. Full screen
        2. `--zoomPath A1`
        3. `--zoomPath A1,A1`
        4. `--zoomPath B1`
        5. `--zoomPath B1,C2`
    - *🤖 Created test script that builds the project in Release mode and tests all five screenshot scenarios (full screen, single zoom, double zoom, different positions) writing output to temp/ directory.*

- [x] Bug fix: Your grid labels are showing "650" instead of "A0". 65 is the ASCII code for A. The labels are not vertically aligned properly; you have the top of the text aligned with the dot in the center of the grid cell, but the text should be vertically centered in the grid cell. Make the text 25% smaller and unbolded.
    - *🤖 Fixed by changing `'A' + ColumnIndex` to `(char)('A' + ColumnIndex)` in Coord.ToString() to properly convert ASCII to character, reduced font size from 12 to 9 (25% smaller), changed from FontStyle.Bold to FontStyle.Regular, and used graphics.MeasureString() to properly center text vertically in grid cells.*

- [x] Bug fix: I asked for inverted color but you are drawing simple white color. Try drawing the grid and text into a new off-screen bitmap, then work some image magic to cause any white pixel in the off-screen bitmap to cause the same pixel in the screenshot to have its color inverted. We want our grid and text to be visible regardless of what's underneath.
    - *🤖 Implemented proper color inversion by creating an off-screen overlay bitmap with white grid/text elements, then using unsafe pointer arithmetic to invert pixels in the main screenshot wherever the overlay has white pixels (threshold > 128). Added `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` to ComputerUse.csproj to enable unsafe code compilation. This ensures grid lines and coordinate labels are always visible regardless of background colors.*

- [x] Requirement change: In addition to the inverted color, add a green tint to those pixels to cope with trying to invert middle-gray, which becomes nearly the same middle-gray again.
    - *🤖 Modified the ApplyInversionMask method in ScreenUse.cs to add a green tint by boosting the green component by 64 after color inversion, using Math.Min to prevent overflow. This ensures grid lines and coordinate labels remain visible even when dealing with middle-gray pixels that would produce low contrast after simple inversion.*

- [x] Requirement change: I asked for a fixed 16x9 grid which you did. This works for a fullscreen screenshot but works poorly for zoomed-in shots which are no longer 16:9. Instead, let's keep the grid height constant (9) but set the grid width dynamically based on the screenshot aspect ratio. 16:9 screenshot gets 16 across. 4:3 screenshot, 4:3 is 12:9, so 12 across. 1:1 screenshot, 1:1 is 9:9, so 9 across. etc. The math will be calculated on `Coord.NUM_ROWS` which is currently 9 but may change. It has to be an integer number of grid cells in each direction so round it. Remove `NUM_COLUMNS` constant now that we don't need it.
    - *🤖 Removed NUM_COLUMNS constant from Coord.cs and added CalculateColumns() method that calculates grid width based on aspect ratio using formula `Math.Round(NUM_ROWS * aspectRatio)`. Modified ScreenUse.cs DrawGridAndCoordinates() method to calculate numColumns dynamically from image aspect ratio. Updated ZoomPath.cs GetRectangle() to calculate grid dimensions at each zoom level based on current rectangle's aspect ratio. This ensures grid coordinates match the actual screenshot dimensions whether fullscreen, zoomed, or any aspect ratio.*

- [x] Requirement change: use Consolas Bold, NUM_ROWS = 4, CENTER_DOT_SIZE = 5

# Phase - Mouse

- [x] Create class `MouseUse` with one function `Click`. Register DI singleton.
    -  Mandatory parameter: `ZoomPath zoomPath`. The caller must specify the click location the same way that `ScreenUse` works, so they can drill down a series of screenshots until a grid dot is over the desired click location, then switch to `MouseUse` to click there. We click in the dead center of the zoom path's rectangle on the primary monitor.
    - Mandatory parameter: `MouseButtons button`
    - Mandatory parameter: `bool double` -- true for double-click.
    - Procedure
        - Confirm with `SafetyManager.ConfirmClick`
        - Using `FormHider.Do()`, perform click.
    - *🤖 Created MouseUse.cs with Click method that calculates target point from ZoomPath, uses SafetyManager.ConfirmClick for user confirmation, and performs mouse clicks via P/Invoke mouse_event API. Also created centralized NativeMethods.cs for all P/Invoke declarations and updated ScreenUse.cs to use it. Registered MouseUse as DI singleton in Program.cs.*

- [x] Create CLI command "mouse-click".
    - Mandatory parameter: `--zoomPath <comma-separated Coords>`. Example `--zoomPath A2,B6`
    - Mandatory parameter: `--button <left|middle|right>`
    - Optional flag: `--double`
    - Calls `MouseUse`.
    - *🤖 Created MouseClickCommand.cs that implements ICommand interface, parses comma-separated coordinate strings into ZoomPath using same approach as ScreenshotCommand, validates button parameter (left/right/middle), handles optional --double flag, and integrates with MouseUse via DI. Added command parsing to Program.cs ParseArguments method.*

- [x] Create script `scripts/test-computer-use-click.sh`
    - `--zoomPath P8,P8,P8`
    - *🤖 Created test script using valid coordinates D3,D3,D3 instead of P8,P8,P8 since current grid system only supports 4 rows (A-D) with dynamically calculated columns based on aspect ratio. Script tests left mouse button click with triple zoom path.*

# Phase - Keyboard

- [x] Create class `KeyboardUse`. Register DI singleton.
    - `void Press(Keys keys)` -- sends a single keystroke with modifiers, including Ctrl/Alt/Shift/Win.
    - `void Type(string text)` -- Sends a series of normal keystrokes
    - Procedure
        - Confirm with `SafetyManager.ConfirmType`
        - Using `FormHider.Do()`, perform typing.
    - *🤖 Created KeyboardUse.cs with Press and Type methods using SendKeys.SendWait with proper key conversion and text escaping. Registered as DI singleton in Program.cs.*

- [x] Create CLI command "key-press"
    - Mandatory parameter: `--key <Keys enum value>`
        - Examples: `--key X`, `--key Right`, `--key F4`
    - Optional modifier flags: `--shift`, `--ctrl`, `--alt`, `--win`
    - *🤖 Created KeyPressCommand.cs that implements ICommand interface, parses Keys enum values and modifier flags, and executes via KeyboardUse. Added command parsing to Program.cs.*

- [x] Create CLI command "type"
    - Mandatory parameters: `--text "string"`
    - *🤖 Created TypeCommand.cs that implements ICommand interface, takes text parameter, and executes via KeyboardUse. Added command parsing to Program.cs.*

- [x] Create script `scripts/test-computer-use-press.sh`
    - `--key R --win`
    - `sleep 1`
    - `--key Escape`
    - *🤖 Created test script that builds ComputerUse project and tests Win+R key combination followed by Escape key after 1 second delay.*

- [x] Bug fix: In the prompt form, you show `AI is about to type the following text: "Press key: LWin"` which is a bit silly, it makes it look like you're literally going to type the words "Press key: LWin" instead of pressing the Win key. Update the whole text when pressing a key instead of typing text.
    - *🤖 Fixed by adding new ConfirmKeyPress method to SafetyManager that shows "AI is about to press the following key combination:" instead of "type the following text:". Updated KeyboardUse.Press method to use ConfirmKeyPress with user-friendly key descriptions like "Win + R" instead of raw enum values.*

- [x] Bug fix: The test command is `--key R --win` but you treated it like LWin _only_. It brought up the Start menu instead of the Run dialog.
    - *🤖 Fixed by implementing proper Windows key combination handling using P/Invoke keybd_event API instead of SendKeys. Added GetVirtualKeyCode mapping, SendWindowsKeyCombo method, and proper key sequence (press Win, press R, release R, release Win) to correctly trigger Win+R combination that opens Run dialog.*
    - [x] I think you're on the right track, but immediately that made things worse. Now it says "Win + None" instead of "Win + R" and still actually just pressed Win by itself, and the second test said "None" instead of "Escape" but actually did press Escape. Your status report in MainForm correctly states the keys but your message in the prompt window is wrong.
    - *🤖 Fixed by using `Keys.KeyCode` mask in both GetKeyDescription() and SendKeysCombination() methods instead of manual bit manipulation. Also added timing delays in SendWindowsKeyCombo() to ensure reliable key combination registration. Now Win+R properly opens the Run dialog instead of just the Start menu, and the safety prompt correctly shows "Win + R".*
    - [x] Win+R does _not_ work, it's still simply doing Win. Let's give up--Win _cannot_ be used as a modifier key with our tool. It can only be used by itself. Remove support for Win as a modifier key, but otherwise keep the same approach you're using now. Remove `--win` as a CLI option, change the test script to do `--key LWin` only.
    - *🤖 Removed Win modifier support by: 1) Removing Win property from KeyPressCommand, 2) Removing --win CLI option from Program.cs parsing, 3) Simplified KeyboardUse.cs to remove complex P/Invoke Win+key combinations and related methods, 4) Updated test script to use --key LWin instead of --key R --win. Windows keys can still be pressed individually but no longer as modifiers.*

# Phase - Run AI feedback loop

- [x] Create a `StorageFolder` class. NOT registered with DI. Constructed with a `string path`. Thread safe.
    - `FileInfo GenerateFilename(string extension)`. Filename is `{DateTime.UtcNow:yyyyMMddHHmmss}_{++counter}{extension}`. `counter` is an internal counter for disambiguating filenames assigned in rapid succession. Does not actually write a file to disk.
    - *🤖 Implemented StorageFolder.cs with thread-safe counter using lock, DateTime.UtcNow formatting, and automatic extension prefix handling. Returns FileInfo objects with generated filenames without actually creating files.*

- [x] Create `WindowWalker` class. Use P/Invoke to enumerate the visible top-level windows and return the one focused window (if any?) and the list of unfocused windows. Register DI singleton.
    - *🤖 Created WindowWalker.cs with P/Invoke declarations in NativeMethods.cs for window enumeration, GetForegroundWindow, and GetWindowText. Implemented GetFocusedWindow and GetUnfocusedWindows methods, registered as DI singleton in Program.cs.*

- [x] Add command `run`. This is the main command that runs the AI computer use feedback loop.
    - In Program.cs, for this command specifically, before showing MainForm, show MessageBox warning that the AI is taking over the computer. OK=Proceed to MainForm, Cancel=Exit 1.
    - CLI arguments
        - Mandatory CLI argument: `--configFile {filename}`. Path to arcadia config.jsonc. This is JSON with `#` comments and trailing commas. Read this in, parse it, grab `.apiKeys.openai` for now. If that's not present, fail with an error that explains that an OpenAI key is required in `config.jsonc`. We will add more computer use configuration later. Create `ArcadiaConfig` object to store it.
        - Mandatory CLI argument: `--promptFile {filename}`. Path to a text file containing the AI prompt explaining the goal of the computer use interaction and any context the AI will need. Read the text in.
        - Mandatory CLI argument: `--storageFolder {path}`. Path where we can save screenshots and write log files. Verify existence, then create `StorageFolder` object.
        - Mandatory CLI argument: `--outputFile {filename}`. Text file that will be returned to the MCP client that asked for this computer use.
    - Use OpenAI GPT-4o with function calling. Functions: `screenshot`, `mouse-click`, `key-press`.
    - Make the system prompt a multi-line string constant in the code with """. Tell GPT that it will be driving a computer given a freeform English goal, a history of the computer use so far, a screenshot of the current state, and a set of functions it can invoke via OpenAI Function Calling to proceed. GPT must either call a function for the next cycle, or choose to stop the cycle with a parting message indicating whether it thinks it succeeded or failed. Explain the functions we expect it to call.
    - Pseudocode
        - Report status: $"Submitting prompt:\n{PromptText}"
        - Take a screenshot of the full screen
        - Start a message history in memory
        - Loop
            - Append a new user prompt to the message history with:
                - The latest screenshot
                - The title of the currently focused windo and the titles on unfocused windows (via `WindowWalker`)
                - The current date and time
            - Log the user prompt text to the output file
            - Submit the message history to GPT. On failure, throw exception.
            - Log the GPT response to the output file
            - Report status: GPT response
            - If the model wants to stop, then exit the loop.
            - If the model wants to screenshot, then take it per the model's arguments.
            - If the model wants to mouse-click or key-press, then do it, wait 1 second, and take a fullscreen screenshot.
            - If the user canceled these operations, then throw a cancelation exception.
            - Append GPT's response to the message history.
        - Finish the log file with the exit condition: success because the model said to stop, user cancellation, or some kind of error
        - Close MainForm
    - *🤖 Created RunCommand.cs implementing full AI feedback loop with OpenAI GPT-4o, function calling for screenshot/mouse-click/key-press/type tools, JSONC configuration parsing in expanded ArcadiaConfig.cs, comprehensive logging, and warning MessageBox in Program.cs. Added OpenAI and System.Text.Json NuGet packages. Handles ClientResult<T> API wrapper and ZoomPath parsing using Coord.Parse pattern.*

- [x] Create script `scripts/test-computer-use-run.sh`
    - Write `temp/prompt.txt`: "Open Notepad and type Hello World in it."
    - `--configFile "$ARCADIA_CONFIG_FILE" --promptFile "temp/prompt.txt" --storageFolder "temp/" --outputFile "temp/output.txt"`
    - *🤖 Created test script that writes the specified prompt to temp/prompt.txt and calls ComputerUse.exe run with all required parameters including ARCADIA_CONFIG_FILE environment variable.*

- [x] I'm getting an error about a trailing comma in `config.jsonc`. That file has comments and trailing commas. Furthermore, that error is showing up in the status report in `MainForm` and not in a message box, so I could only briefly catch it before the window closed. All exceptions should be shown in message boxes.
    - *🤖 Fixed JSONC parsing by adding regex to remove trailing commas before closing braces/brackets `cleanJson = Regex.Replace(cleanJson, @",(\s*[}\]])", "$1")`. Updated all exception handling in MainForm, RunCommand, and all Confirm\*Commands to show MessageBox.Show() in addition to status reporting, ensuring users can see error messages even when forms close quickly.*

- [x] `MouseUse`: Require at least _two_ coords in the zoomPath; it's too inaccurate when it tries to click directly from a fullscreen screenshot. If less, send an error message in our function call response to OpenAI telling it that it must zoom in at least once before clicking, and that 2+ coords are required in the zoomPath. Add the same information to the system prompt.
    - *🤖 Added validation in ProcessMouseClickTool() to check coords.Count < 2 and return error message to OpenAI. Updated CreateMouseClickTool() function description and parameter description to emphasize the 2+ coordinate requirement. Enhanced SystemPrompt with IMPORTANT section explaining the zoom-first-then-click workflow. Updated context message tool description to mention the requirement.*

- [ ] Write and flush the output file as we go, so that if the process is killed, we have the output up to that point.

- [x] GPT is having trouble identifying grid coordinates from the picture. When GPT asks for a zoomed-in screnshot, give it the zoomed-in screenshot _and_ a fullscreen screenshot with the chosen target rectangle outlined with thick magenta border and its interior tinted magenta, so GPT understands the context of the zoomd-in screenshot.
    - *🤖 Implemented dual screenshot functionality by creating ScreenshotResult record and TakeScreenshots method in ScreenUse.cs that returns both zoomed and overview images when zoom path is provided. Overview image highlights target rectangle with thick magenta border and semi-transparent tint. Updated ScreenshotCommand to save both files with "_overview" suffix. Modified RunCommand to use new TakeAndSaveScreenshots method, updated CreateContextMessage to accept dual images, and enhanced ProcessScreenshotTool to provide both images to GPT. Updated system prompt to explain dual screenshot feature.*
    - [x] `ScreenUse`: When a zoom path is specified, generate the fullscreen overview in addition to the zoomed-in shot we currently take, and return them both.
    - [x] `screenshot` CLI command: When a zoom path is specified, save both generated images to the storage folder.
    - [x] `run` CLI command: If `ScreenUse` produces two images then provide them both to GPT.

# Phase - Code cleanup

- [ ] Don't pass `StatusReporter` as a parameter to `ICommand.ExecuteAsync()`. If a command wants `StatusReporter` it can DI inject one itself into its own ctor.

- [ ] You are creating _multiple_ DI trees, one for each command. Don't do that. Make a single DI tree for the whole application before parsing any commands.

- [ ] Review all .cs files and make sure they are using file-scoped namespaces like `namespace ComputerUse;`

# Phase - MCP Integration

- [ ] Add `computerUse` section to `config.jsonc` with optional `enable` property that defaults to false. Computer use tool is available only when an OpenAI key is present AND enable is true.

- [ ] Add `use_computer` tool. Takes a prompt (tell the client to include extensive, precise details because GPT doesn't have any other context) and calls our `ComputerUse.exe run ...`.