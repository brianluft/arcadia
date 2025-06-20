## Completed Tasks

- [x] Fix SQL Server collation conflict in describe_database_object test
    - *🤖 Fixed by adding `COLLATE DATABASE_DEFAULT` to all string expressions in the UNION ALL query in the SQL Server describe_database_object implementation. The error was caused by different collations being used in the UNION ALL operator between system function results and column data. The fix ensures all strings use the same collation.*

## Pending Tasks

(None currently)
