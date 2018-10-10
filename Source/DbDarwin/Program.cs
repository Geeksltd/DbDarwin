using System;
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
                if (!string.IsNullOrEmpty(first) && first.ToLower() == "extract-schema")
                {
                    Console.WriteLine("Start Extract Schema...");

                    // Read -connect parameter
                    var index = argList.IndexOf("-connect");
                    if (index == -1)
                    {
                        Console.WriteLine("-connect parameter is requirement");
                        Console.ReadLine();
                        return;
                    }

                    string connection = string.Empty;
                    if (argList.Count > index + 1)
                        connection = argList[index + 1];

                    // Read -out parameter
                    var indexOut = argList.IndexOf("-out");
                    if (indexOut == -1)
                    {
                        Console.WriteLine("-out parameter is requirement");
                        Console.ReadLine();
                        return;
                    }
                    string outFile = string.Empty;
                    if (argList.Count > indexOut + 1)
                        outFile = argList[indexOut + 1];
                    ExtractSchemaService.ExtractSchema(connection, outFile);
                }
            }
        }
    }
}
