- [x] I have created `/temp/input.json` with a real SQL Server connection and test. Fix this error. It successfully writes the output file but then prints this error and exits nonzero. This works on SQLite but fails on SQL Server.
    ```
    $ build/dotnet/Database.exe --input "C:/Projects/arcadia/temp/input.json" --output "C:/Projects/arcadia/temp/output.json"
    There is already an open DataReader associated with this Connection which must be closed first.
    ```
    - *🤖 Fixed by replacing the `using var reader` pattern with explicit DataReader lifecycle management using try-finally block. The issue was that SQL Server requires the DataReader to be fully closed and disposed before transaction rollback operations. The fix ensures `reader.CloseAsync()` and `reader.DisposeAsync()` are called in the finally block before any transaction operations.*
