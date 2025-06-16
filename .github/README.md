<h1 align="center"><img src="icon_256.png" width=64><br>Arcadia MCP Server</h1>

<p align="center">Cursor has lots of bugs on Windows and Arcadia provides workarounds for some of them.</p>

## Features

- **Bash command execution**
  - MCP tools: `run_bash_command`, `read_output`
  - Avoids Cursor bug: [run tool attempts to use xterm bracketed paste on Windows where that isn't supported](https://forum.cursor.com/t/run-terminal-cmd-tool-adds-incorrect-bracketed-paste-markers-2728/52558).
  - Avoids Cursor bug: [run tool sometimes hangs before actually starting the command](https://forum.cursor.com/t/cursor-agent-mode-when-running-terminal-commands-often-hangs-up-the-terminal-requiring-a-click-to-pop-it-out-in-order-to-continue-commands/59969/11).
  - Avoids Cursor bug: [commands sometimes execute but never return control to the model](https://forum.cursor.com/t/run-command-is-hanging/38904).
  - Improves on Cursor by automatically paging command outputs. Paging avoids blowing out the model's context from an unexpected long command output.
  - Improves on Cursor by adding mandatory timeouts to all command executions.

- **Image analysis with AI**
  - MCP tool: `read_image`
  - Uses GPT-4o to analyze and describe images, transcribe text, and answer questions about visual content
  - Improves on Cursor by letting the agent autonomously read images on disk. Cursor's multimodal function requires you to manually add images as context.
  - Requires an OpenAI API key in `config.jsonc`.

## Installation

### Prerequisites

- Windows 10/11
- Cursor IDE
- Git for Windows (for Git Bash)

### Setup Instructions

1. **Download Arcadia**: Download the latest `arcadia.zip` from our GitHub Releases.

2. **Extract**: Extract `arcadia.zip` to a folder of your choice (e.g., `C:\Tools\arcadia\`)

3. **Configure Cursor**: Follow the `INSTALLING.html` guide.

4. **Verify Installation**: Ask the model to verify the presence of Arcadia MCP tools.
