- [ ] These tests can't both be telling the truth. If the first test passed, then it must have returned at least one result. But if so, then that result should be available for the second test to use, but it says there were none found. Remove the logic for skipping the second test; if we're running SQL Server tests, then run _all_ the SQL Server tests.
    ```
    🧪 Running test: sql_server_list_database_objects
    Description: Test SQL Server list_database_objects returns at least one result
    ✅ PASS
    ...
    🧪 Running test: sql_server_describe_database_object
    Description: Test SQL Server describe_database_object using first result from list_database_objects
    ✅ PASS (SKIPPED: No database objects found to test describe_database_object)
    ```
