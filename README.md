# DbDarwin
*Forces evolution upon live databases*
DbDarwin is a utility for Sql database migration script generation, optimised for M# dev ops.  

## Commands reference
The CLI tool will support the following commands:
### Extract schema
```
DbDarwin.exe extract-schema -connect "my connection string" -out "someFilePath.xml"
```
It will connect to a specified sql database and extract the schema into an XML file that is easier to process in the next steps. This command is intended to be invoked once on the existing live database and once on the new database. The produced two xml files will be used later for actual comprison.

NOTE: Other than just schema, we need the data for reference tables also.

### Generate diff
```
DbDarwin.exe generate-diff -from "CurrentLiveDatabaseFilePath.xml" -to "NewSchemaFilePath.xml" -out "Diff.xml"
```
It will compare the schemas and generates a new xml file that represents the required changes. The generated file contains a flat list of additions and removals.

We will have a UI for the developer to select any addition/removal pair (for tables and columns only) and optionally mark them as RENAME.
