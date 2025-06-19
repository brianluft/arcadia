# SQL Server tool
This tool will be a thin wrapper around a C# .NET console application that we will write. The .NET app will be a one-shot process; we give it a request JSON file and it writes a response JSON file. It prints nothing to stdout and exits 0. If it has any error, it prints the error to stderr and exits 1.
- [ ] C# .NET console application
    - [x] Create a C#, .NET 9, console application project in `database/`.
    - [x] Update `scripts/build.sh`.
        - [x] Build into `build/database/`.
        - [x] New input parameter that says whether we're building for development or release. This doesn't change anything for the existing builds but we'll need it for C#.
        - [x] Build the .NET application. For development use `dotnet build`, debug, framework dependent. For publish use `dotnet publish`, release, self-contained, ready-to-run, single-file. Tell `dotnet` to be quiet, we only want to see warnings/errors.
    - [x] Update `scripts\publish.sh.`
        - [x] Copy `build/database/` to `dist/database/`.
    - [x] Add `Microsoft.Data.Sqlite` and `Microsoft.Data.SqlClient` using `dotnet`. We will test using Sqlite because it's easy, but in production our goal is to use SQL Server. It's too hard to test with SQL Server so implement it but don't test it.
    - [x] Implement the C# program.
        - [x] Mandatory CLI argument `--input <file>`: input .json file path with this structure. All properties are mandatory. See "## Sample input JSON" below.
        - [x] Mandatory CLI argument `--output <file>`: output .json file path to be written to. The output format will be an array of rows, where each row is an array. The first one is the column headers. This is basically a CSV structure encoded into JSON. See "## Sample output JSON" below.
        - [x] Optional CLI argument `--expect <file>`: if specified, then after the output file is read, it is compared to the preexisting contents of this file. Trim leading/trailing whitespace and ignore line ending differences. If they do match, print `Pass: <output filename> matches <expect filename>` and exit 0. If they don't match, print `Fail: <output filename> does not match <expect filename>` and exit 1. This will be used for testing.
        - [x] Connect to the database as specified, make the query, skip the requested rows, take up the requested max rows, and write the given JSON format in the output file.
            - [x] Disable ADO.NET connection pooling, this is a one-shot process so it's pointless.
    - [x] Instead of a C# unit test, use `--expect` to test it directly from `scripts/build.sh` after building. Use the SQLite test file in `test/files/foo.sqlite3` for tests. The table `foo` has one column `a` with a thousand rows with the numbers 1 through 1000. Stick your JSON files in `test/files/`.
- [x] Add SQL Server connection strings to the server's `config.jsonc`. Configuration is not required for SQLite. See `## Sample config.json` below for the syntax.
    - [x] `connections` section is optional.
    - [x] Provide two example commented-out data sources:
        - [x] SQL Server with Windows authentication
        - [x] SQL Server with SQL authentication
        - [x] Include the Encrypt flag explicitly so the user can turn it off if needed.
    - [x] Read them in at startup with the rest of the config but don't do anything with this information until an MCP tool is called.
- [x] Create a helper in the server for executing SQL commands via our C# program.
    - [x] The C# program is located relative to the path of our server script which it already knows in `index.ts`, it's in `../database/`.
    - [x] Observe the program's exit code. If nonzero then it's a failure and the program should have printed an error message to stderr. Grab that message and throw an exception. On zero it's success and it prints nothing.
- [x] New MCP tools.
    - Guidelines
        - Outputs and paging: All tools will use the same output storage mechanism that `run_bash_command` does. We'll write the full output to a file, then return a truncated snippet from the top of the file in the MCP tool response. For this, make a new function for truncation that takes a maximum number of characters and returns as many full lines as it can before hitting that total character limit. For database tools, the output limit is 10,000 characters. Make that a constant we can adjust later. When truncated, append a line to the response indicating how much output was returned in the response, how much is actually available in the file, and what filename and line number to request to continue paging.
        - Use fully qualified and bracket-quoted object names in SQL Server, the syntax is `[{database}].[{schema}].[{table}]`. SQLite syntax is the `"`-quoted name, `"{table}"`, if the table name actually includes a literal `"` they are doubled.
        - For these tools, it's all about integration. Skip the jest unit tests and use our real-deal test harness to test against an SQLite database in `test/files/foo.sqlite3`. Don't test SQL Server; leave that to me to test manually.
    - [x] In the docs, explain that "object" here means a table, view, stored procedure, user defined function, or user defined type.
    - [x] Tool: `list_database_connections`. This is the list of SQL Server connections but does NOT include SQLite, because SQLite databases can be used on-the-fly without configuration. Explain this in the doc.
        - [x] Generate one name per line. Don't include the connection string itself since it may include passwords.
    - [x] Tool: `list_database_schemas`. SQL Server only. Lists databases and their schemas. No parameters. Returns a list of every `[{database}].[{schema}]`, one per line.
    - [x] Tool: `list_database_objects`. Go here next if you don't know where to find something in the database.
        - [x] Mandatory parameter: `connection`. May be the name of an SQL Server connection or the absolute path to an SQLite file.
        - [x] Mandatory parameter: `type`. May be:
            - [x] `relation`: table or view (SQL Server and SQLite)
            - [x] `procedure`: stored procedure (SQL Server only)
            - [x] `function`: user defined function (SQL Server only)
            - [x] `type`: user defined type (SQL Server only)
        - [x] Optional parameter: `search_regex`. If a pattern is provided it will be used to filter the names. Case insensitive.
        - [x] Optional `database` parameter: SQL Server only. If specified, it's one of the database names (no schema) from `list_database_schemas`, and the search is performed only on that database. Otherwise all databases are scanned.
        - [x] Generate one name per line.
    - [x] Tool: `describe_database_object`. Use this to get the definition of an object you found in `list_database_objects`.
        - [x] Mandatory parameter: `connection`. May be the name of an SQL Server connection or the absolute path to an SQLite file.
        - [x] Mandatory parameter: `name`. Same syntax as the output from `list_database_objects`.
        - [x] SQLite: just return the `sql` from `sqlite_master`, it's already literal SQL.
        - [x] SQL Server: reconstruct pseudo-SQL from the columns, primary key and other constraints (including constraint names), secondary indexes (including names and options), options. Don't worry about it being exactly syntactically correct, this is just documentation.
    - [x] Tool: `list_database_types`. This is unlike the other endpoints. It's static information, just a dump of the `DbType` enum value names. Don't page or write to an output file, just enumerate `DbType` and return the names one per line. This is to help MCP clients that need to bind parameters in `run_sql_command` and are having a hard time guessing the names.
    - [x] Tool: `run_sql_command`.
        - [x] Wrap command execution in a transaction that ALWAYS rolls back. It never commits under any circumstance. In the doc, tell the client that this will happen.
        - [x] Mandatory parameter: `connection`. May be the name of an SQL Server connection or the absolute path to an SQLite file.
        - [x] Mandatory parameter: `command`. This is the literal SQL command with `@foo` style named parameters. In the doc, remind the client that this can be multiple statements (ADO.NET handles this for us). In SQL Server, it can be a whole T-SQL script.
        - [x] Mandatory parameter: `timeout_seconds`. In the doc, recommend 30 seconds as a good starting place.
        - [x] Optional parameter: `arguments`. This provides the value for each `@foo` named parameter used in the command text. May be omitted if the command doesn't have any named parameters. This is a key-value object where the keys are the parameter names and the values are like `{ "type": "Int32", "value": 123 }`. In the doc, mention that on SQLite the only types needed are: `Int64`, `Double`, `String`. In SQL Server there are many more, use `list_database_types` for the full list. We don't support `Byte[]`.
        - [x] The output is line-oriented JSON. Each row is a JSON object returned in one line of the response. No "outer" array, just one JSON object per row directly into the MCP tool response.
        - [x] If it timed out, then return the error instead.
- [x] Update `.github\README.md` with our new feature.
- [x] Update `server\INSTALLING.html` with guidance on how to configure SQL Server connections in `config.jsonc` if desired.
- Bug fixes
    - [x] Build/publish process needs to handle compiling for ARM64 vs. x64, you are only building for x64 now. Update `build.sh` to accept an optional arch just like `publish.sh` does, have publish pass it down. When not specified, use the native arch of the build machine (we are on arm64 right now).
    - [x] `publish.sh` needs to tell `build.sh` to build for release, we are publishing a debug build now.
- [x] GitHub Actions: `.github\workflows\build-and-publish.yml` is broken because we attempt to run the arm64 Database.exe on the x64 runner. Additionally, it's slow to build arm64 and x64 serially.
    - [x] Refactor `scripts/init.sh` to extract the parts that download files into a new `scripts/download.sh`. `download.sh` will be a noop if the downloaded files already exist, as is the case with `init.sh` currently. `init.sh` will now fail with an explicit error if an expected downloaded file does not exist; the message will inform the user to run `download.sh` first.
        - [x] Update `CONTRIBUTING.md` about running `download.sh` first.
    - [x] Restructure the single GitHub Actions job into a fork pattern to build x64 and arm64 in parallel.
        1. Initialization job.
            - Run `download.sh`
            - Upload the contents of the `downloads/` folder as a single artifact
        2. Fan out to two jobs: x64 and arm64.
            - x64 runs on `windows-latest`, arm64 runs on `windows-11-arm64`.
            - Download the artifact to put `downloads/` back into place.
            - Proceed like `.github\workflows\build-and-publish.yml` does now, but uploading a separate artifact for x64 and arm64.

## Sample input JSON
```
{
    "provider": "Microsoft.Data.SqlClient",
    "connectionString": "<ADO.NET connection string>",
    "query": "SELECT * FROM Users WHERE IsActive = @isActive",
    "parameters": {
        "@isActive": { "type": "Boolean", "value": true }
    },
    "commandType": "Text",
    "timeoutSeconds": 30,
    "skipRows", 0,
    "takeRows": 1000
}
```

## Sample output JSON
```
[
    ["column_name_1","column_name_2","column_name_3","column_name_4"],
    [100,"foo",true,null]
]
```

## Sample config.json
```
"connections": {
    "sqlServer": [
        "my_db": "Server=myServerAddress;Database=myDataBase;Trusted_Connection=True;"
    ]
}
```
