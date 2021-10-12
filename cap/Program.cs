using System;
using System.IO;
using LibCap;
using System.Linq;
using System.Collections.Generic;

namespace cap
{
    class Program
    {
        private const string DG_TOCOCUMBA = "/home/jader/projs/dotnet/cap/LibCap.Tests/test_assets/json/Dungeon_tococumba.json";
        
        static void WriteUsage() {
            Console.WriteLine("usage: cap target.cap -cmap map1.json map2.json -ctile tile1.json ...");
            Console.WriteLine("Options:");
            Console.WriteLine("  -cmap:  compress map files");
            Console.WriteLine("  -ctile: compress tileset files");
            Console.WriteLine("  -cmeta: compress meta data files");
            Console.WriteLine("  -crpg:  compress rpg system files");
            Console.WriteLine();
        }
        
        static int AddFiles(IEnumerable<string> args, AssetType assetType, ref CapBuilder builder) {
            int pos = 0;
            if (builder == null) {
                builder = new CapBuilder();
            }
            
            foreach (var file in args) {
                if (file[0] == '-')
                    break;
                pos++; 

                if (!File.Exists(file)) {
                    Console.WriteLine("File {0} not found.", file);
                    Environment.Exit(1);
                }
            
                var res = builder.AddAsset(file, assetType);
                if (!res.IsOk) {
                    Console.WriteLine(res.Msg);
                    Environment.Exit(1);
                }
            }
            
            return pos;
        }
        static void Main(string[] args)
        {
            var builder = new CapBuilder();
            
            if (args.Length <= 2) {
                WriteUsage();
                Environment.Exit(1);
            }
            
            string targetFile = args[0];
            int pos = 1;

            do {
                switch (args[pos]) {
                    case "-cmap":
                        pos++;
                        pos += AddFiles(args.Skip(pos), AssetType.MAP, ref builder);
                        break;
                        
                    case "-cmeta":
                        pos++;
                        pos += AddFiles(args.Skip(pos), AssetType.META, ref builder);
                        break; 

                    case "-ctile":
                        pos++;
                        pos += AddFiles(args.Skip(pos), AssetType.TILESET, ref builder);
                        break; 

                    case "-crpg":
                        pos++;
                        pos += AddFiles(args.Skip(pos), AssetType.RPGSYSTEM, ref builder);
                        break; 
                    
                    default:
                        WriteUsage();
                        Environment.Exit(1);
                        break;
                }
            } while (pos < args.Length);


            if (builder.ExportCap(targetFile, true))
                Console.WriteLine("Done.");
            else
                Console.WriteLine("Something went wrong.");

        }
    }
}
