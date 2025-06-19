# Logs app
This will be a C# .NET 9 console program. We plan to ship our existing `Database.exe` and this new `Logs.exe` in the same directory so they can share the .NET files. `Database.exe` is currently published as single-file; we'll have to change that.

## Requirements
- [x] It will monitor the storage directory for new *.log files and it will "tail" the alphabetically last one. When a new log file appears, stop tailing the one you're tailing and start tailing the new one. Print a message when that happens. Note: the log files may be CR, CRLF, or LF in the input; use `Console.WriteLine()` on the output for uniform native line endings.
- [x] At startup, print the whole content of the alphabetically last log file, then start tailing it for new lines. The log files themselves don't have timestamps; take your own timestamp per line while tailing when you see new lines appear.
- [x] Read `../config.jsonc` (relative to `Logs.exe`) at startup in order to get the storage directory, which is specified relative to `config.jsonc`. If `%ARCADIA_CONFIG_FILE%` is set, then that's the path to `config.jsonc` instead of `../config.jsonc`.
- [x] Logs are printed in chronological order with this format: `{timestamp} {filename}: {message}`.
- [x] Optional CLI flag `--snapshot` that will print the current contents of the last log file as usual but then simply exit rather than waiting for new messages.

## Tasks
- [x] Restructure the existing single-project `database/` code into a new parent folder `dotnet/` to support a multi-project solution. Rely on unix tools and `dotnet` for this, don't read files and then rewrite them from memory when possible.
    ``` 
    (project root)
        dotnet/
            .config/
                dotnet-tools.json
            Database/
                Database.csproj
                Program.cs
            .csharpierrc
            dotnet.sln
    ```
    - [x] Update `server\src\database.ts` to look for `Database.exe` in `../dotnet/` instead of `../database/`.
    - [x] Fix `build.sh` and `publish.sh` breakages. Build the solution instead of the project, DON'T build as single-file.
    - *🤖 Created new `dotnet/` directory structure with solution file, moved existing database project files to `dotnet/Database/`, updated all references in `server/src/database.ts`, `scripts/build.sh`, `scripts/publish.sh`, and `scripts/format.sh` to use the new paths, removed single-file publishing option, and successfully tested the build.*
- [x] Add new `dotnet/Logs/` project with `dotnet`. Implement the app.
    - [x] Fix `build.sh` and `publish.sh` breakages. Ensure we get both `Database.exe` and `Logs.exe` in `build/dotnet/` / `dist/dotnet/`.
    - *🤖 Created new `dotnet/Logs/` console project with System.CommandLine and System.Text.Json packages, implemented comprehensive log monitoring application with config reading from JSONC, file system watching, log tailing with timestamps, and --snapshot CLI option. Updated build.sh to test Logs.exe by temporarily unsetting ARCADIA_CONFIG_FILE to verify default config.jsonc search behavior. Both Database.exe and Logs.exe are now built and tested successfully.*
- [x] Update `build.sh` to test it by clearing `build/storage`, writing a test file with a test message, and then running `build/dotnet/Logs.exe --snapshot` to see if it prints the test message.
    - *🤖 Added test section to build.sh that creates build/storage directory, creates test.log with test message, temporarily unsets ARCADIA_CONFIG_FILE to test default config search behavior, runs Logs.exe --snapshot, and verifies the test message appears in output.*
- [x] Create `dotnet/logs.bat`, have it `@ECHO OFF`, `cd /d "%~dp0"`, `cd dotnet`, `Logs.exe`. Update `publish.sh` to copy it into the root of the zip, next to `INSTALLING.html`.
    - *🤖 Created `dotnet/logs.bat` with proper batch file commands to launch Logs.exe from the dotnet subdirectory, and updated `scripts/publish.sh` to copy logs.bat to the root of the distribution zip alongside INSTALLING.html.*
- [x] Update `server\INSTALLING.html` letting the user know they can click `logs.bat` to view active logs.
    - *🤖 Added a new "📋 Viewing Active Logs" section to INSTALLING.html with step-by-step instructions for using logs.bat, including information about real-time monitoring and --snapshot mode functionality.*
- [x] Update `.github\README.md`.
    - *🤖 Added "Real-time log monitoring" as a new feature bullet point in the Features section, describing logs.bat launcher, automatic monitoring, timestamps, and both continuous and snapshot modes.*
