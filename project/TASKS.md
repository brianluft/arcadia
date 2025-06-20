# Bug fixes
- [ ] When `config.jsonc` does not specify a storage directory, we are incorrectly going one level too high when placing the `storage` directory. I have arcadia in `C:\tools\arcadia` and the default config is `C:\tools\arcadia\config.jsonc`, but the storage folder ended up in `C:\tools\storage` instead of `C:\tools\arcadia\storage`. Our tests are spitting files into `/storage` right off the repository root instead of `build/storage`.
- [ ] Add a series of SQL Server tests that are selectively skipped. Only run them if `$ARCADIA_CONFIG_FILE` (we already require this to be set globally in order to run our real-deal test harness) points to a config.json that contains an SQL Server connection named "test". If not, skip them. They will be skipped in CI but I have a SQL Server configuration locally. The tests must work on any database that has some tables already, but the test doesn't hardcode any database, schema, or table names so it works on arbitrary databases.
    - [ ] `list_database_schemas`: Test that it succeeds and returns at least one result.
    - [ ] `list_database_objects`: Test that it succeeds and returns at least one result.
    - [ ] `describe_database_object`: First run `list_database_objects`, grab the first result, and then test `describe_database_object` on that result.
    - [ ] `run_sql_command`: Test `SELECT 1 AS foo`
