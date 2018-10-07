# DbDarwin
*Forces evolution upon live databases*.

DbDarwin is a utility for Sql database migration script generation, optimised for M# dev ops.  

## Background
As a part of our transition to full DevOps, we have identified a need to automate our database migration process in a safe, reliable and highly efficient way.

Tools such as SQL Compare are very mature and have lots of features. But they are not suitable for our purpose, due to licensing costs, learning curve and lack of flexibility for full flexibility and control to match our processes. In particular, they cannot give us enough control for full automation which we need for maximised efficiency. For that reason, we have decided to create our own database migration tool, codenamed DbDarwin. We take a very pragmatic and agile approach to designing and enhancing the tool, as our needs evolve.

## Open source
This project will be open source and available here on GitHub. It's licensed under GPLv3, and available to anyone who is interested in using it. However, beware that some of its features will be opinionated and designed for our specific needs and technical environment. 

## Overall architecture
DbDarwin will consist of a CLI tool, `DbDarwin.dll`, and also a Graphical User Interface tool called `DbDarwin.UI.exe`.

- Both will be developed using .NET Core 2.1.
- The UI app will be a self-hosted ASP.NET Core app.

> The UI app will be a lightweight application and it will not contain any actual logic related to SQL stuff. It will simply make calls to the CLI tool. It's purpose is just to simplify the developer decisions in teh process. All the real logic will be programmed in the CLI tool.

# CLI Commands
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

### Generate script
```
DbDarwin.exe generate-script -diff "Diff.xml" -out "migrate.sql" 
```
Generates the sql code equvalent to the diff xml file.
