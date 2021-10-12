using System;
using System.IO;
using LibCap;
using System.Linq;

namespace cap
{
    class Program
    {
        private const string DG_TOCOCUMBA = "/home/jader/projs/dotnet/cap/LibCap.Tests/test_assets/json/Dungeon_tococumba.json";
        
        static void WriteUsage() {
            Console.WriteLine("usage: cap cm output.cap map1.json map2.json <...>");
            Console.WriteLine("Options:");
            Console.WriteLine("\tcm - compress map");
        }
        static void Main(string[] args)
        {
            CapError res;
            var builder = new CapBuilder();
            
            if (args.Length <= 2) {
                WriteUsage();
                Environment.Exit(1);
            }

            switch (args[0]) {
                case "cm":
                    foreach (var file in args.Skip(2)) {
                        if (!File.Exists(file)) {
                            Console.WriteLine("File {0} not found.", file);
                            Environment.Exit(1);
                        }
                    
                        res = builder.AddAsset(file, AssetType.MAP);
                        if (!res.IsOk) {
                            Console.WriteLine(res.Msg);
                            Environment.Exit(1);
                        }
                    }

                    if (builder.ExportCap(args[1], true))
                        Console.WriteLine("Done.");
                    else
                        Console.WriteLine("Something went wrong.");

                    break;
                
                default:
                    WriteUsage();
                    Environment.Exit(1);
                    break;
            }

        }
    }
}
