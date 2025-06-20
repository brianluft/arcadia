- [x] These tests can't both be telling the truth. If the first test passed, then it must have returned at least one result. But if so, then that result should be available for the second test to use, but it says there were none found. Remove the logic for skipping the second test; if we're running SQL Server tests, then run _all_ the SQL Server tests.
    ```
    🧪 Running test: sql_server_list_database_objects
    Description: Test SQL Server list_database_objects returns at least one result
    ✅ PASS
    ...
    🧪 Running test: sql_server_describe_database_object
    Description: Test SQL Server describe_database_object using first result from list_database_objects
    ✅ PASS (SKIPPED: No database objects found to test describe_database_object)
    ```
    - 🤖 _Removed the skipping logic in `runDescribeDatabaseObjectTest()` method in `test/src/index.ts`. Changed the behavior when no database objects are found from passing with a skip message to failing with an error. This exposes the real issue where the SQL Server test connection may not have accessible database objects, rather than hiding it behind a skip message. The test now properly fails when it can't find objects to test with, which is the correct behavior._
