using NUnit.Framework;

namespace LibCap.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }
        
        [Test]
        public static void AddFile_CorrectDeps() {
            var builder = new CapBuilder();
            var check = builder.AddAsset(Const.DG_TOCOCUMBA, AssetType.MAP);
            
            if (!check.IsOk)
                Assert.Fail(string.Format("Error: {0}", check.Msg));
            
            Assert.True(builder.ContainsFile(Const.DG_TOCOCUMBA));
            Assert.AreEqual(5, builder.Count);
        }
        
        public static void AddFile_Test_Utils(string path, AssetType asset, CapError.ErrorTypes expected_error) {
            var builder = new CapBuilder();
            var check = builder.AddAsset(path, asset);
            
            Assert.False(check.IsOk);
            Assert.AreEqual(check.Type, expected_error);
            
            Assert.False(builder.ContainsFile(path));
            Assert.AreEqual(0, builder.Count);
        }
        
        [Test]
        public static void AddFile_BadDeps() {
            AddFile_Test_Utils(Const.DG_TOCOCUMBA_BADDEPS1, AssetType.TILESET, CapError.ErrorTypes.FileNotFound);
            AddFile_Test_Utils(Const.DG_TOCOCUMBA_BADDEPS2, AssetType.MAP, CapError.ErrorTypes.FileNotFound);
        }
        
        [Test]
        public static void AddFile_Invalid() {
            AddFile_Test_Utils(Const.DG_TOCOCUMBA_INVALID, AssetType.MAP, CapError.ErrorTypes.FileIsInvalid);
        }
    }
}