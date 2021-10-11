using System;
using LibCap;

namespace cap
{
    class Program
    {
        private const string DG_TOCOCUMBA = "/home/jader/projs/dotnet/cap/LibCap.Tests/test_assets/json/Dungeon_tococumba.json";
        static void Main(string[] args)
        {
            var builder = new CapBuilder();
            
            var res = builder.AddAsset(DG_TOCOCUMBA, AssetType.MAP);

            if (!res.IsOk) {
                Console.WriteLine(res.Msg);
                return;
            }

            if (builder.ExportCap("DungeonTococumba.zip", true))
                Console.WriteLine("Done.");
            else
                Console.WriteLine("Dependency problem.");
        }
    }
}
