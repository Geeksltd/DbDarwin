using System;
using System.Collections.Generic;
using System.Linq;
using DbDarwin.Service;
using Olive;

namespace DbDarwin
{
    class Program
    {
        static void Main(string[] args)
        {
            var argList = args.ToList();
            if (argList.Any())
            {
                var first = args.First();
                if (first.HasAny())
                {
                    if (first.ToLower() == "extract-schema")
                    {
                        if (IsArgumentExtractSchemaValid(argList, out var connection, out var outputFile))
                            ExtractSchemaService.ExtractSchema(connection, outputFile);
                    }
                    else if (first.ToLower() == "generate-diff")
                    {
                        if (IsArgumentGenerateValid(argList, out var currentFile, out var newSchemaFile,
                            out var outputFile))
                            CompareSchemaService.StartCompare(currentFile, newSchemaFile, outputFile);
                    }
                    else if (first.ToLower() == "generate-script")
                    {
                        if (IsArgumentGenerateScriptValid(argList, out var diffFile, out var migrateSqlFile))
                            GenerateScriptService.GenerateScript(diffFile, migrateSqlFile);
                    }
                    else if (first.ToLower() == "rename")
                    {
                        if (IsArgumentTransformationValid(argList, out var diffFile, out var tableName, out var fromName, out var toName, out var diffFileOutput))
                            CompareSchemaService.TransformationDiffFile(diffFile, tableName, fromName, toName, diffFileOutput);
                    }
                }
            }
        }

        static bool IsArgumentTransformationValid(List<string> argList, out string diffFile, out string tableName, out string fromName, out string toName, out string migrateSqlFile)
        {
            Console.WriteLine("Start generate migration script...");
            // Read -diff parameter
            diffFile = ReadArgument("-diff", argList, "-diff parameter is requirement");
            // Read -out parameter
            migrateSqlFile = ReadArgument("-out", argList, "-out parameter is requirement");
            // Read table name parameter
            tableName = ReadArgument("table", argList, string.Empty, false);
            // Read from name parameter
            fromName = ReadArgument("from", argList, "from parameter is requirement");
            // Read from name parameter
            toName = ReadArgument("to", argList, "to parameter is requirement");



            return diffFile.HasValue() &&
                   fromName.HasValue() &&
                   migrateSqlFile.HasValue() &&
                   fromName.HasValue() &&
                   toName.HasValue();
        }

        static bool IsArgumentGenerateScriptValid(List<string> argList, out string diffFile, out string migrateSqlFile)
        {
            Console.WriteLine("Start generate migration script...");
            // Read -diff parameter
            diffFile = ReadArgument("-diff", argList, "-diff parameter is requirement");
            // Read -out parameter
            migrateSqlFile = ReadArgument("-out", argList, "-out parameter is requirement");

            return diffFile.HasValue() && migrateSqlFile.HasValue();
        }

        static bool IsArgumentExtractSchemaValid(List<string> argList, out string connection, out string outputFile)
        {
            Console.WriteLine("Start Extract Schema...");
            // Read -connect parameter
            connection = ReadArgument("-connect", argList, "-connect parameter is requirement");
            // Read -out parameter
            outputFile = ReadArgument("-out", argList, "-out parameter is requirement");

            return connection.HasValue() && outputFile.HasValue();
        }

        static bool IsArgumentGenerateValid(List<string> argList, out string currentFile, out string newSchemaFile, out string outputFile)
        {
            Console.WriteLine("Start generate the differences...");
            // Read -from parameter
            currentFile = ReadArgument("-from", argList, "-from parameter is requirement");
            // Read -to parameter
            newSchemaFile = ReadArgument("-to", argList, "-to parameter is requirement");
            // Read -out parameter
            outputFile = ReadArgument("-out", argList, "-out parameter is requirement");

            return currentFile.HasValue() && newSchemaFile.HasValue() &&
                   outputFile.HasValue();
        }

        static string ReadArgument(string argument, List<string> argList, string message, bool requirement = true)
        {
            var index = argList.IndexOf(argument);
            if (index == -1 && requirement)
            {
                Console.WriteLine(message);
                Console.ReadLine();
                return string.Empty;
            }
            if (argList.Count > index + 1)
                return argList[index + 1];
            return string.Empty;
        }
    }
}