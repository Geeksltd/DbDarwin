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
