- [x] I have created `/temp/input.json` with a real SQL Server connection and test. Fix this error. It successfully writes the output file but then prints this error and exits nonzero. This works on SQLite but fails on SQL Server.
    ```
    $ build/dotnet/Database.exe --input "C:/Projects/arcadia/temp/input.json" --output "C:/Projects/arcadia/temp/output.json"
    There is already an open DataReader associated with this Connection which must be closed first.
    ```
    - *🤖 Fixed by replacing the `using var reader` pattern with explicit DataReader lifecycle management using try-finally block. The issue was that SQL Server requires the DataReader to be fully closed and disposed before transaction rollback operations. The fix ensures `reader.CloseAsync()` and `reader.DisposeAsync()` are called in the finally block before any transaction operations.*
- [x] When `config.jsonc` does not specify a storage directory, we are incorrectly going one level too high when placing the `storage` directory. I have arcadia in `C:\tools\arcadia` and the default config is `C:\tools\arcadia\config.jsonc`, but the storage folder ended up in `C:\tools\storage` instead of `C:\tools\arcadia\storage`. Our tests are spitting files into `/storage` right off the repository root instead of `build/storage`.
