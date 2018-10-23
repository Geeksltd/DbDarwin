using System;
using System.Collections.Generic;
using System.Linq;
using DbDarwin.Model.Command;
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
                        var model = IsArgumentExtractSchemaValid(argList);
                        if (model.IsValid)
                            ExtractSchemaService.ExtractSchema(model);
                    }
                    else if (first.ToLower() == "generate-diff")
                    {
                        var model = IsArgumentGenerateDiffFileValid(argList);
                        if (model.IsValid)
                            CompareSchemaService.StartCompare(model);
                    }
                    else if (first.ToLower() == "generate-script")
                    {
                        var model = IsArgumentGenerateScriptValid(argList);
                        if (model.IsValid)
                            GenerateScriptService.GenerateScript(model);
                    }
                    else if (first.ToLower() == "rename")
                    {
                        var model = IsArgumentTransformationValid(argList);
                        if (model.IsValid)
                            CompareSchemaService.TransformationDiffFile(model);
                    }
                }
            }
        }

        static Transformation IsArgumentTransformationValid(List<string> argList)
        {
            var model = new Transformation();
            Console.WriteLine("Start generate migration script...");
            // Read -diff parameter
            model.CurrentDiffFile = ReadArgument("-diff", argList, "-diff parameter is requirement");
            // Read -out parameter
            model.MigrateSqlFile = ReadArgument("-out", argList, "-out parameter is requirement");
            // Read table name parameter
            var notRequerment = false;
            model.TableName = ReadArgument("table", argList, string.Empty, notRequerment);
            // Read from name parameter
            model.FromName = ReadArgument("from", argList, "from parameter is requirement");
            // Read from name parameter
            model.ToName = ReadArgument("to", argList, "to parameter is requirement");
            return model;
        }

        static GenerateScript IsArgumentGenerateScriptValid(List<string> argList)
        {
            var model = new GenerateScript();

            Console.WriteLine("Start generate migration script...");
            // Read -diff parameter
            model.CurrentDiffFile = ReadArgument("-diff", argList, "-diff parameter is requirement");
            // Read -out parameter
            model.MigrateSqlFile = ReadArgument("-out", argList, "-out parameter is requirement");

            return model;
        }

        static ExtractSchema IsArgumentExtractSchemaValid(List<string> argList)
        {
            Console.WriteLine("Start Extract Schema...");
            return new ExtractSchema
            {
                // Read -connect parameter
                ConnectionString = ReadArgument("-connect", argList, "-connect parameter is requirement"),
                // Read -out parameter
                OutputFile = ReadArgument("-out", argList, "-out parameter is requirement")
            };
        }

        static GenerateDiffFile IsArgumentGenerateDiffFileValid(List<string> argList)
        {
            Console.WriteLine("Start generate the differences...");
            return new GenerateDiffFile
            {
                // Read -from parameter
                CurrentFile = ReadArgument("-from", argList, "-from parameter is requirement"),
                // Read -to parameter
                NewSchemaFile = ReadArgument("-to", argList, "-to parameter is requirement"),
                // Read -out parameter
                OutputFile = ReadArgument("-out", argList, "-out parameter is requirement")
            };
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