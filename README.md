# DbDarwin
*Forces evolution upon live databases*.


DbDarwin is a utility for Sql database migration script generation, optimized for M# dev ops.  

## Background
As a part of our transition to full DevOps, we have identified a need to automate our database migration process in a safe, reliable and highly efficient way.

Tools such as SQL Compare are very mature and have lots of features. But they are not suitable for our purpose, due to licensing costs, learning curve and lack of flexibility for full flexibility and control to match our processes. In particular, they cannot give us enough control for full automation which we need for maximized efficiency. For that reason, we have decided to create our own database migration tool, codenamed DbDarwin. We take a very pragmatic and agile approach to designing and enhancing the tool, as our needs evolve.

## Open source
This project will be open source and available here on GitHub. It's licensed under GPLv3, and available to anyone who is interested in using it. However, beware that some of its features will be opinionated and designed for our specific needs and technical environment. 

## Overall architecture
DbDarwin will consist of a CLI tool, `DbDarwin.dll`, and also a Graphical User Interface tool called `DbDarwin.UI.exe`.

- Console app developed using .NET Core 2.1.
- The UI app will be a WPF .net framwork 4.6.1

> The UI app will be a lightweight application and it will not contain any actual logic related to SQL stuff. It will simply make calls to the CLI tool. It's purpose is just to simplify the developer decisions in teh process. All the real logic will be programmed in the CLI tool.

# CLI Commands
The CLI tool will support the following commands:

### Extract schema
```
DbDarwin.exe extract-schema -connect "my connection string" -out "someFilePath.xml"
```
It will connect to a specified sql database and extract the schema into an XML file that is easier to process in the next steps. This command is intended to be invoked once on the existing live database and once on the new database. The produced two xml files will be used later for actual comprison.

NOTE: Other than just schema, we need the data for reference tables also.

> In the first version, we only focus on tables, columns, keys and indexes. We ignore Views, schemas, owner, ...
```xml
<Schema>
  <Table Name="MyTable">
    <Column Name="Id" DataType="uniqueidentifier" Nullable="false" />
    <PrimaryKey Name="..." Column="Id" />
    
    <Column Name="MyColumn" DataType="nvarchar(4000)" Nullable="false" Default="..."/>
    <Column Name="MyAssociation" DataType="uniqueIdentifier" Nullable="true" />
    ...
    
    <ForeignKey Name="SomeKey" Column="MyAssociation" References="AnotherTable.Id" OnDelete="Cascade" OnUpdate="No Action" />
    <Index Name="..." Clustered="false" Unique="false" Columns="Col1|Col2|Col3" />
    <Records>
      <Add Id="">
        <Col1>Value 1</Col1>
        <Col2>Value 2</Col2>
        <Col3>...</Col3>
      </Add>
      ...
    </Records>
  </Table>
</Schema>
```

### Generate diff
```
DbDarwin.exe generate-diff -from "CurrentLiveDatabaseFilePath.xml" -to "NewSchemaFilePath.xml" -out "Diff.xml"
```
It will compare the schemas and generates a new xml file that represents the required changes.

#### Changed settings for existing components
Every component (table, column, etc) will be identified by its name (and parent table). When a component with the same name exists in both, but with changed setttings, it will be generated again, with only the changed settings.

For example, if in the above sample code, only the nullable setting of the MyAssociation column in the MyTable table was changed, the following diff XML will be generated:
```xml
<Diff>
  <Table Name="MyTable">
    <Column Name="MyAssociation" Set-Nullable="false" />    
  </Table>
</Diff>
```

Note that the changed settings' will get a `Set-` prefix in the XML.

#### Added / Removed table components
Per existing table (which exists in both), for every additions or removal of columns, indexes, etc whose names do not match, an `<add>` or `<remove>` tag will be generated.

```xml
<Diff>
  <Table Name="MyTable">
    <add>
      <Column Name="MyNewColumn" DataType="nvarchar(4000)" Nullable="true" />
      <Column Name="AnotherNewColumn" DataType="int" Nullable="true" />
    </add>
    <remove>
      <Column Name="MyOldColumn" DataType="nvarchar(4000)" Nullable="true" />
    </remove>    
    </Table>  
</Diff>
```

#### Renamed objects
If an existing component's name is changed, we normally identify that as a `remove / add` pair as there is no way to know that they are indeed the same. To solve this problem, we need the developer's manual intervention.

The UI app will allow the developer to select any addition/removal pair for the same object type under the same table, and click `It's a rename` button, which would then modify the generated diff XML. For instance, if in the above example, the developer marked `MyOldColumn` and `MyNewColumn` as a *Rename*, then the generated Diff would become:
```xml
<Diff>
  <Table Name="MyTable">
    <Column Name="MyOldColumn" Set-Name="MyNewColumn" />
    <add>      
      <Column Name="AnotherNewColumn" DataType="int" Nullable="true" />
    </add>
  </Table>  
</Diff>
```
Such transformation will be applied via a CLI command:
```
DbDarwin.exe rename -diff "Diff.xml" table "table-Name" from "MyOldColumn" to "MyNewColumn" -out "Diff v2.xml" 
```
> If the rename operation is on a table, rather than a table component, simply the `table="table-Name"` part will not be specified.

#### Modified data
A similar logic will be used for data changes in the reference tables.

### Generate script
Once the developer is happy with the `Diff.Xml` file, after all manuall interventions, the following command will be invoked:
```
DbDarwin.exe generate-script -diff "Diff.xml" -out "migrate.sql" 
```
It generates the sql code equvalent to the diff xml file.


# Graphical User Interface
DB Darwin has a clear UI for comparing schema/data of two tables and generating T-SQL result in the MS SQL Server.

![app db darwin](https://github.com/Geeksltd/DbDarwin/raw/master/doc/Capture2.PNG)

## Process
The developer would use the UI tool to go through a step-by-step process to:

1. Select the new (local) database such as `MyMSharpApp.Temp` on `.\SqlExpress`
2. Select the current live (or UAT) database such as `MyMSharpApp` through a live connection string.
   - Note: Ideally the live database should be first copied locally for a test run of the generated scripts.
3. All removals will be shown on the left side of the screen, and all additions will be shown on the right side
4. The developer can select an object from the left side, and one from the right side, and click a button reading "It's a rename"
5. Rename operations will be shown on the right side.
6. Clicking on any operation on either side, will show the equivalent generated SQL for it at the bottom of the screen.
7. Click Done - which shows a screen with the full generated scripts and a button to Download as a file.

# Running the generated migration SQL
During a deployment operation, we need to:

- Take the live database offline
- Create a backup
- Run the generated SQL file
- Bring the database back online

TODO: This is to be handled via the Jenkins process. We will need the DB change script to be injected into the pipeline. The simplest approach is to make it an ad-hoc deployment parameter. But then, the history will be lost. So ideally, we need the generated SQL to be added to the source repo. But how?

### Next version
- Upon every such action, a new version of the Diff.xml will be created, with the ability to Undo (go back to a previous version)
- Any change (on either side of the screen) can be manually excluded, which simply deletes that item from the Diff xml.
- We can add further manual intervention possibilities when the need araises.
