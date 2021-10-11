using NUnit.Framework;

namespace LibCap.Tests
{
    public class Tests
    {
        private const string DG_TOCOCUMBA = "/home/jader/projs/dotnet/cap/LibCap.Tests/test_assets/json/Dungeon_tococumba.json";
        private const string DG_TOCOCUMBA_BADDEPS1 = "/home/jader/projs/dotnet/cap/LibCap.Tests/test_assets/json/baddeps/16x16_DungeonTileset.json";
        private const string DG_TOCOCUMBA_BADDEPS2 = "/home/jader/projs/dotnet/cap/LibCap.Tests/test_assets/json/baddeps/Dungeon_tococumba.json";

        private const string DG_TOCOCUMBA_INVALID = "/home/jader/projs/dotnet/cap/LibCap.Tests/test_assets/json/invalid/Dungeon_tococumba.json";
        
        [SetUp]
        public void Setup()
        {
        }
        
        [Test]
        public static void AddFile_CorrectDeps() {
            var builder = new CapBuilder();
            var check = builder.AddAsset(DG_TOCOCUMBA, AssetType.MAP);
            
            if (!check.IsOk)
                Assert.Fail(string.Format("Error: {0}", check.Msg));
            
            Assert.True(builder.ContainsFile(DG_TOCOCUMBA));
            Assert.AreEqual(5, builder.Count);
        }
        
        public static void AddFile_BadDeps_Utils(string path, AssetType asset) {
            var builder = new CapBuilder();
            var check = builder.AddAsset(path, asset);
            
            Assert.False(check.IsOk);
            Assert.AreEqual(check.Type, CapError.ErrorTypes.FileNotFound);
            
            Assert.False(builder.ContainsFile(path));
            Assert.AreEqual(0, builder.Count);
        }
        
        [Test]
        public static void AddFile_BadDeps() {
            AddFile_BadDeps_Utils(DG_TOCOCUMBA_BADDEPS1, AssetType.TILESET);
            AddFile_BadDeps_Utils(DG_TOCOCUMBA_BADDEPS2, AssetType.MAP);
        }
        
        [Test]
        public static void AddFile_Invalid() {
            AddFile_BadDeps_Utils(DG_TOCOCUMBA_INVALID, AssetType.MAP);
        }
    }
}