﻿using System;
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
                        if (ValidateArguamentExtaractSchema(argList, out var connection, out var outputFile))
                            ExtractSchemaService.ExtractSchema(connection, outputFile);
                    }
                    else if (first.ToLower() == "generate-diff")
                    {
                        if (ValidateArguamentGenerateDiff(argList, out var currentFile, out var newSchemaFile,
                            out var outputFile))
                            CompareSchemaService.StartCompare(currentFile, newSchemaFile, outputFile);
                    }
                }
            }
        }

        private static bool ValidateArguamentExtaractSchema(List<string> argList, out string connection, out string outputFile)
        {
            Console.WriteLine("Start Extract Schema...");
            // Read -connect parameter
            connection = ReadArgument("-connect", argList, "-connect parameter is requirement");
            // Read -out parameter
            outputFile = ReadArgument("-out", argList, "-out parameter is requirement");

            return !string.IsNullOrEmpty(connection) && !string.IsNullOrEmpty(outputFile);

        }

        private static bool ValidateArguamentGenerateDiff(List<string> argList, out string currentFile, out string newSchemaFile, out string outputFile)
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
