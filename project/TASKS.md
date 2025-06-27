# Computer Use tool

## Rules
- One class per file.
- Use DI.
- This is a WinForms project so don't write to the console.
- Don't bother writing unit tests; this is mostly UI and needs to be tested manually.
- Must not require administrator access or UAC elevation.
- High-DPI support. Use auto-size for everything possible. When fixed pixel values are needed, multiply by dpi scaling factor.
- Put all P/Invoke declarations in a global `NativeMethods.cs` class

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

## Phase - Safety
Before every action (screenshot, mouse click, key press) we will inform the user and allow them to cancel.

- [ ] Create class `SafetyPromptForm`. This is a dialog with a label that tells the user what we're going to do, has a progressbar that goes backwards (filled to empty) as an N-second countdown, and a Cancel button.
    - Label text provided in constructor. Multi-line string.
    - Countdown in seconds provided in constructor.
    - 100ms timer updates the progressbar as it counts down to 0.
    - DialogResult OK is the countdown expired or Cancel if the user clicked Cancel or closed the dialog.
    - Set "Always On Top"

- [ ] Create class `SafetyCrosshairForm`. This is a borderless, transparent, modeless window with a thick blinking crosshair drawn at a particular screen coordinate. Crosshair thickness is 3px * dpi scaling factor with the target dead center, length 64px * dpi scaling factor. The idea is for mouse click confirmation, we will show the crosshair as an overlay on the screen, and then show the prompt on top.
    - Crosshair center `Point` in physical screen coordinates provided in constructor. It sets its own Location/Size in order to correctly cover the target position with the crosshair.
    - 250ms timer that toggles the crosshair between magenta and transparent.
    - Set "Always On Top"
    - Caller is expected to close the form, there's no other way for the user to close it.

- [ ] Create class `SafetyRectangleForm`. This is a borderless, transparent, modeless window with either a magenta or transparent blinking solid color fill. The idea is for screenshot confirmation, we will blink a rectangle over the region about to be screenshotted.
    - `Rectangle` in physical screen coordinates provided in constructor. It sets its own Location/Size to these.
    - 250ms that toggles the fill between magenta and transparent.
    - Set "Always On Top"
    - Caller is expected to close the form, there's no other way for the user to close it.

- [ ] Create class `SafetyManager`. Register DI singleton.
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

- [ ] Create commands:
    - [ ] `confirm-screenshot --x 0 --y 0 --w 100 --h 100`
    - [ ] `confirm-click --x 100 -y 100`
    - [ ] `confirm-type --text "foo bar"`

- [ ] Create scripts with the test arguments above:
    - [ ] `scripts/test-computer-use-confirm-screenshot.sh`
    - [ ] `scripts/test-computer-use-confirm-click.sh`
    - [ ] `scripts/test-computer-use-confirm-type.sh`

## Phase - Screenshot
- [ ] Create class `ScreenUse` with one function `TakeScreenshot`. Register DI singleton.
    - Overview: Screenshot, scale, draw a grid, generate PNG.
    - Mandatory parameter: `FileInfo outputFile`. PNG file to write.
    - Optional parameter: `ZoomPath? zoomPath`. When omitted it screenshots the whole screen, otherwise it zooms into each grid cell in the path in succession.
    - Procedure
        0. `SafetyManager.ConfirmScreenshot`
        1. Screenshot the primary monitor only. Include the mouse pointer.
        2. Calculate the target region of the primary screen via `zoomPath.GetRectangle()`. Crop the screenshot to that rectangle.
        3. Scale (down _or_ up) to 1080 px height, with width according to the original aspect ratio. When scaling down, use a high quality scaling algorithm. When scaling up, use a nearest-neighbor algorithm.
        4. Impose a grid of rectangles on the cropped image per `Coord.NUM_ROWS` and `Coord.NUM_COLUMNS`. Draw 2px inverted color grid lines.
        5. In the dead center of each grid cell, draw a 3x3 inverted color rectangle, with the center pixel of that 3x3 rectangle being the center of the grid cell. To the right, write the grid coordinate like "A0" (via `Coord.ToString()`) in small text 12px tall (make that font size a constant in the code so we can tweak it later).
        6. Save to PNG in `outputFile`.

- [ ] Create CLI command "screenshot".
    - Optional parameter: `--zoomPath <comma-separated Coords>`. Example `--zoomPath A2,B6`
    - Mandatory parameter: `--outputFile <filename>`. PNG output file.
    - Calls `ScreenUse`.

- [ ] Create script `scripts/test-computer-use-screenshot.sh`.
    - Use `cd "$( dirname "${BASH_SOURCE[0]}" )"` `cd ..` to situate yourself at the project root.
    - `mkdir -p temp`
    - Test `screenshot` by writing screenshots to `temp/`
        1. Full screen
        2. `--zoomPath A1`
        3. `--zoomPath A1,A1`
        4. `--zoomPath B1`
        5. `--zoomPath B1,C2`

# Phase - Mouse

- [ ] Create class `MouseUse` with one function `Click`. Register DI singleton.
    -  Mandatory parameter: `ZoomPath zoomPath`. The caller must specify the click location the same way that `ScreenUse` works, so they can drill down a series of screenshots until a grid dot is over the desired click location, then switch to `MouseUse` to click there. We click in the dead center of the zoom path's rectangle on the primary monitor.
    - Mandatory parameter: `MouseButtons button`
    - Mandatory parameter: `bool double` -- true for double-click.
    - Confirm first with `SafetyManager.ConfirmClick`

- [ ] Create CLI command "mouse-click".
    - Mandatory parameter: `--zoomPath <comma-separated Coords>`. Example `--zoomPath A2,B6`
    - Mandatory parameter: `--button <left|middle|right>`
    - Optional flag: `--double`
    - Calls `MouseUse`.

- [ ] Create script `scripts/test-computer-use-click.sh`
    - `--zoomPath P8,P8,P8`

# Phase - Keyboard

- [ ] Create class `KeyboardUse`. Register DI singleton.
    - `void Press(Keys keys)` -- sends a single keystroke with modifiers, including Ctrl/Alt/Shift/Win.
    - `void Type(string text)` -- Sends a series of normal keystrokes
    - For both, confirm first with `SafetyManager.ConfirmType`

- [ ] Create CLI command "key-press"
    - Mandatory parameter: `--key <Keys enum value>`
        - Examples: `--key X`, `--key Right`, `--key F4`
    - Optional modifier flags: `--shift`, `--ctrl`, `--alt`, `--win`

- [ ] Create CLI command "type"
    - Mandatory parameters: `--text "string"`

- [ ] Create script `scripts/test-computer-use-press.sh`
    - `--key R --win`
    - `sleep 1`
    - `--key Escape`

# Phase - Run AI feedback loop

- [ ] Create a `StorageFolder` class. NOT registered with DI. Constructed with a `string path`. Thread safe.
    - `FileInfo GenerateFilename(string extension)`. Filename is `{DateTime.UtcNow:yyyyMMddHHmmss}_{++counter}{extension}`. `counter` is an internal counter for disambiguating filenames assigned in rapid succession. Does not actually write a file to disk.

- [ ] Create `WindowWalker` class. Use P/Invoke to enumerate the visible top-level windows and return the one focused window (if any?) and the list of unfocused windows. Register DI singleton.

- [ ] Add command `run`. This is the main command that runs the AI computer use feedback loop.
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

- [ ] Create script `scripts/test-computer-use-run.sh`
    - Write `temp/prompt.txt`: "Open Notepad and type Hello World in it."
    - `--configFile "$ARCADIA_CONFIG_FILE" --promptFile "temp/prompt.txt" --storageFolder "temp/" --outputFile "temp/output.txt"`

# Phase - MCP Integration

- [ ] Add `computerUse` section to `config.jsonc` with optional `enable` property that defaults to false. Computer use tool is available only when an OpenAI key is present AND enable is true.

- [ ] Add `use_computer` tool. Takes a prompt (tell the client to include extensive, precise details because GPT doesn't have any other context) and calls our `ComputerUse.exe run ...`.