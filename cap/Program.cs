using System;
using System.IO;
using LibCap;
using System.Linq;
using System.Collections.Generic;

namespace cap
{
    class Program
    {
        public enum CommandType {
            Compress,
            Extract
        }
        
        static void WriteUsage() {
            Console.WriteLine("usage compress: cap target.cap -cmap map1.json map2.json -ctile tile1.json ...");
            Console.WriteLine("Compress Options:");
            Console.WriteLine("  -cmap:  compress map files");
            Console.WriteLine("  -ctile: compress tileset files");
            Console.WriteLine("  -cmeta: compress meta data files");
            Console.WriteLine("  -crpg:  compress rpg system files");
            Console.WriteLine();
            Console.WriteLine("usege extract: cap target.cap -edir outputDir");
            Console.WriteLine("  -edir: extract to directory");
            Console.WriteLine();
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
            CommandType cmd = CommandType.Compress;
            
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
                        
                    case "-edir":
                        pos++;
                        if (pos != 2) {
                            Console.WriteLine("Export parameter -e can only used alone");
                            WriteUsage();
                            Environment.Exit(1);
                        }
                        
                        cmd = CommandType.Extract;
                        string outputDir = args[pos];
                        pos++;
                        
                        if (!Directory.Exists(outputDir))
                            Directory.CreateDirectory(outputDir);
                        
                        var res = builder.ExtractCap(targetFile, outputDir, true); 
                        if (!res.IsOk) {
                            Console.WriteLine(res.Msg);
                            Environment.Exit(1);
                        } else {
                            Console.WriteLine("Done.");
                        }
                        break;
                    
                    default:
                        WriteUsage();
                        Environment.Exit(1);
                        break;
                }
            } while (pos < args.Length);

            if (cmd == CommandType.Compress) {
                if (builder.ExportCap(targetFile, true))
                    Console.WriteLine("Done.");
                else
                    Console.WriteLine("Something went wrong.");
            }
        }
    }
}
