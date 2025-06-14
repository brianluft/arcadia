- [x] Create `scripts/init.sh` to set things up after first cloning the repo.
    - [x] Use a variable for the node version, currently 22.16.0
    - [x] Use `scripts-get-native-arch.sh` to get the string "x64" or "arm64"
    - [x] Navigate to root of repository with `cd "$( dirname "${BASH_SOURCE[0]}" )"` and `cd ..`
    - [x] Create "downloads" folder if it doesn't exist.
    - [x] Download Node if it hasn't already been downloaded: https://nodejs.org/dist/v22.16.0/node-v22.16.0-win-arm64.zip (replace version and arch)
    - [x] Delete "node" folder if it exists.
    - [x] Expand Node into the root of the project. The zip contains a top-level folder `node-v22.16.0-win-arm64` (same as the filename). Rename that folder to "node". Verify that `node/node.exe` and `node/npm.exe` exist.
    - [x] Test `init.sh`
- [x] Add a rule about running node/npm out of our local folder, and that they are _not_ installed globally in the system.
- [x] Begin a `server/` folder with `package.json`. This will be the MCP server.
    - [x] Use TypeScript with `@modelcontextprotocol/sdk`
    - [x] Local prettier install. Override printWidth=120. Update `scripts/format.sh` to format the server code.
    - [x] Update `scripts/init.sh` to `npm install` the server project. Run it.
    - [x] Update `scripts/build.sh` to run TypeScript and produce .js output in `dist/server/`. Copy node into `dist/node/`.
    - [x] Test `build.sh`.
- [x] Begin a `test/` folder with another `package.json`. This will be an MCP client that runs real automated tests against the MCP server.
    - [x] Use TypeScript with `@modelcontextprotocol/sdk`
    - [x] Local prettier install. Override printWidth=120. Update `scripts/format.sh` to format the test code too.
    - [x] Update `scripts/init.sh` to `npm install` the test project. Run it.
    - [x] Create a simple way for us to write a series of MCP tool executions and then verify the responses, then tabulate the successes and failures.
    - [x] Update `scripts/build.sh` to build the test client into `dist/test/` and then run the test client, so that we run the tests every time after building.
- [x] Create a unit test system for the server, separate from our actually-for-real MCP test client. This will be used to test individual functions and components in the server without having to expose MCP tools for internal functionality. Use jest.
- [x] Create a configuration system. At startup the server will read a config.json file in the parent folder of the folder containing the running js file. That is, our server is in `dist/server/` and the config file is in `dist/`.
    - [x] Make an example config.json and copy it into `dist/` on build.
- [x] New config.json option: path to a storage directory. Default: `./storage/` (path relative to config.json, but absolute path also accepted if configured)
    - [x] Create the directory on startup. Create a test file and delete it. If any of that fails, print an error and exit.
- [x] I have manually updated our packages. I updated jest to the latest major version. I removed ts-jest because it seems like we shouldn't need it; we can build ourselves with tsc and that will avoid issues with jest having its own tsc configuration. I removed @types/jest because I think jest ships its own typings now, and @types/jest is out of date. These changes have broken the tests.
    - [x] Read `context\jest-typings.md`.
    - [x] Fix the test errors.
- [x] Use currently use `import.meta.url` to resolve paths relative to the executing script file. We use this in several modules. We just upgraded to Node 24. Get the dirname from `import.meta.dirname` at startup, then pass that path down to the initialization of any other module that wants it. Use a dependency injection pattern.
    - [x] Add a rule about never using `import.meta` outside of startup in `index.ts`.
    - [x] Add a rule about using dependency injection for modules.
- [x] Create an internal system for assigning unique timestamped filenames in the storage directory. Make it one dense numeric ID with the date and time information encoded in it. Ensure the file does not exist when you return a newly assigned filename. We'll use this to store the output from tool executions so the client can page through it. This is not directly exposed as an MCP tool; it's used in tasks below.
- [x] New MCP tool: `run_bash_command`
    - [x] Required parameter: String for the command line.
    - [x] Required parameter: Working directory, absolute path required. The MCP client can specify either C:\Foo\Bar.txt or /c/Foo/Bar or /c:/Foo/Bar and all of them should work, even though only the first one is proper on Windows. Throw an error if it's not one of those forms; don't make any attempt to handle a relative path. The client is in a better position to fix their input.
    - [x] Required parameter: Timeout in seconds. In the doc for the client, recommend a 120 second default timeout. On timeout, forcibly kill the command.
    - [x] New config.json option: path to bash. Default: `C:\Program Files\Git\bin\bash.exe`
        - [x] At startup, check that the configured shell exists, if not print an error and exit.
    - [x] Assign a storage filename. Write output from the command (both stdout and stderr) into this file.
    - [x] The command is run via bash, so that the command can be a bash command line with pipes, redirects, etc. Synchronously with the specified timeout.
    - [x] Returns:
        - [x] Last 20 lines of output.
        - [x] If the program exited on its own, "Exit code: {N}". Otherwise, "The command timed out after {N} seconds."
        - [x] If more than 20 lines of output were printed, then the last line of response is: "Truncated output. Full output is {Count} lines. Use `read_output` tool with filename "{Filename}" line 0 to read more." (the read_output tool is coming later)
    - [x] Write happy path tests. Since the tool accepts bash commands, just test with `echo`.
    - [x] Write a test for a command that doesn't exist and check how the error is presented.
    - [x] Write a test for timeout; make it a 1 second timeout and test with `ping -n 10 127.0.0.1`
- [x] New MCP tool: `read_output`
    - [x] Required parameter: Filename
    - [x] Required parameter: Start line index, zero based
    - [x] If file doesn't exist, return an error.
    - [x] If file does exist, jump to that line. If it's past the end, return a message that the requested line is past the end of the file.
    - [x] Start reading from that line forward and returning those lines in the response array. Tokenize by whitespace so you can count how many words we're returning, update the running total after each line. Once you exceed 1000 words, stop and append line "Truncated output. There are {N} lines left. Use `read_output` tool with filename "{Filename}" line {NextLineIndex} to read more."
    - [x] Make a long test file in `test/files/` that you can test with `run_bash_command` and `cat` for a truncated result.
- [x] `scripts/init.sh`:
    - [x] Download `https://www.7-zip.org/a/7za920.zip` to `downloads/` if it doesn't exist.
    - [x] There is no top-level folder inside the zip. Create `7zip/` and extract the files there.
    - [x] Run it.
- [x] Tests are taking 11 seconds which is deeply suspicious--remember our `ping -n 10` test. It's supposed to be killed after 1 second. Write additional tests to verify that we really are killing the process after the desired timeout.
- [x] Create `scripts/publish.sh`.
    1. Clean `dist/`
    2. Run `build.sh`
    3. Delete `dist/test/`
    4. Copy `node/` to `dist/node`
    5. Use `7zip/7za.exe` to make `arcadia.zip` from `dist/*`
- [x] Write a readme that describes how to set up Cursor with our MCP server, assuming the user has downloaded `arcadia.zip` from our GitHub releases. Give the sample `mcp.json` file:
    ```
    {
      "mcpServers": {
        "arcadia": {
          "command": "<arcadia path>\\node\\node.exe",
          "args": [
            "<arcadia path>\\server\\index.js"
          ]
        }
      }
    }
    ```
- [x] Fix `scripts/publish.sh` to only ship runtime dependencies, not dev dependencies.
    - [x] Problem: Currently `scripts/build.sh` copies `server/node_modules` (which includes dev dependencies from `npm install`) to `dist/server/node_modules`. When we publish, we're shipping all the dev dependencies unnecessarily.
    - [x] Solution approach: During publish, create a production-only install for the server dependencies.
    - [x] Modify `scripts/build.sh`: Rename `dist` to `build`. `build` will be the development build and `dist` will be the final production build.
    - [x] Modify `scripts/publish.sh`:
        - [x] `build.sh` now produces `build/` instead of `dist/`, adapt to that.
        - [x] Create `dist/` and copy our files from `build` but not any `node_modules/` and not `test/`
        - [x] Run `npm ci --omit=dev` inside `dist/server/` to install only production dependencies
        - [x] This way the zip only contains the runtime dependencies needed to run the MCP server
- [x] `bash.ts`: If our attempts to kill the subprocess on timeout fail to actually kill the process, you need to resolve the promise anyway. We will simply abandon the process and let it terminate on its own; we will not wait around for it. No matter what, we must resolve the promise right away after the timeout expires. We can't let a rogue subprocess hang us up; there are scenarios where killing doesn't work, or doesn't work immediately.
