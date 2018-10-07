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

### Generate script
```
DbDarwin.exe generate-script -diff "Diff.xml" -out "migrate.sql" 
```
Generates the sql code equvalent to the diff xml file.
