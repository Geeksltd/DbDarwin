using System;
using System.Collections.Generic;
using System.Linq;
using DbDarwin.Service;

namespace DbDarwin
{

    class Program
    {
        static void Main(string[] args)
        {
            var argList = args.ToList();
            if (argList.Count > 0)
            {
                var first = args.First();
                if (!string.IsNullOrEmpty(first))
                {
                    if (first.ToLower() == "extract-schema")
                    {
                        if (ValidateArgumentExtaractSchema(argList, out var connection, out var outputFile))
                            ExtractSchemaService.ExtractSchema(connection, outputFile);
                    }
                    else if (first.ToLower() == "generate-diff")
                    {
                        if (ValidateArgumentGenerateDiff(argList, out var currentFile, out var newSchemaFile,
                            out var outputFile))
                            CompareSchemaService.StartCompare(currentFile, newSchemaFile, outputFile);
                    }
                    else if (first.ToLower() == "generate-script")
                    {
                        if (ValidateArgumentGenerateScript(argList, out var diffFile, out var migrateSqlFile))
                            GenerateScriptService.GenerateScript(diffFile, migrateSqlFile);
                    }
                    else if (first.ToLower() == "rename")
                    {
                        if (ValidateArgumentTransformation(argList, out var diffFile, out var tableName, out var fromName, out var toName, out var diffFileOutput))
                            CompareSchemaService.TransformationDiffFile(diffFile, tableName, fromName, toName, diffFileOutput);
                    }
                }
            }
        }


        public static bool ValidateArgumentTransformation(List<string> argList, out string diffFile, out string tableName, out string fromName, out string toName, out string migrateSqlFile)
        {
            Console.WriteLine("Start generate migration script...");
            // Read -diff parameter
            diffFile = ReadArgument("-diff", argList, "-diff parameter is requirement");
            // Read -out parameter
            migrateSqlFile = ReadArgument("-out", argList, "-out parameter is requirement");

            // Read table name parameter
            var table = argList.FirstOrDefault(x => x.ToLower().StartsWith("table"));
            tableName = !string.IsNullOrEmpty(table) ?
                table.Split(new[] { '=' }).LastOrDefault()?.Trim(new[] { '\"' }) :
                null;

            // Read from parameter
            var fromParam = argList.FirstOrDefault(x => x.ToLower().StartsWith("from"));
            if (!string.IsNullOrEmpty(fromParam))
                fromName = fromParam.Split(new[] { '=' }).LastOrDefault()?.Trim(new[] { '\"' });
            else
            {
                fromName = null;
                Console.WriteLine("from parameter is requirement");
                Console.ReadLine();
            }

            // Read to parameter
            var toParam = argList.FirstOrDefault(x => x.ToLower().StartsWith("to"));
            if (!string.IsNullOrEmpty(toParam))
                toName = toParam.Split(new[] { '=' }).LastOrDefault()?.Trim(new[] { '\"' });
            else
            {
                toName = null;
                Console.WriteLine("to parameter is requirement");
                Console.ReadLine();
            }
            return !string.IsNullOrEmpty(diffFile) &&
                   !string.IsNullOrEmpty(fromParam) &&
                   !string.IsNullOrEmpty(migrateSqlFile) &&
                   !string.IsNullOrEmpty(fromName) &&
                   !string.IsNullOrEmpty(toName);
        }

        private static bool ValidateArgumentGenerateScript(List<string> argList, out string diffFile, out string migrateSqlFile)
        {
            Console.WriteLine("Start generate migration script...");
            // Read -diff parameter
            diffFile = ReadArgument("-diff", argList, "-diff parameter is requirement");
            // Read -out parameter
            migrateSqlFile = ReadArgument("-out", argList, "-out parameter is requirement");

            return !string.IsNullOrEmpty(diffFile) && !string.IsNullOrEmpty(migrateSqlFile);

        }

        private static bool ValidateArgumentExtaractSchema(List<string> argList, out string connection, out string outputFile)
        {
            Console.WriteLine("Start Extract Schema...");
            // Read -connect parameter
            connection = ReadArgument("-connect", argList, "-connect parameter is requirement");
            // Read -out parameter
            outputFile = ReadArgument("-out", argList, "-out parameter is requirement");

            return !string.IsNullOrEmpty(connection) && !string.IsNullOrEmpty(outputFile);

        }

        private static bool ValidateArgumentGenerateDiff(List<string> argList, out string currentFile, out string newSchemaFile, out string outputFile)
        {
            Console.WriteLine("Start generate the differences...");
            // Read -from parameter
            currentFile = ReadArgument("-from", argList, "-from parameter is requirement");
            // Read -to parameter
            newSchemaFile = ReadArgument("-to", argList, "-to parameter is requirement");
            // Read -out parameter
            outputFile = ReadArgument("-out", argList, "-out parameter is requirement");

            return !string.IsNullOrEmpty(currentFile) && !string.IsNullOrEmpty(newSchemaFile) &&
                   !string.IsNullOrEmpty(outputFile);
        }

        private static string ReadArgument(string argument, List<string> argList, string message)
        {
            var index = argList.IndexOf(argument);
            if (index == -1)
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
