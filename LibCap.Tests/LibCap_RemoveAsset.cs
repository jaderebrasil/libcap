using NUnit.Framework;

namespace LibCap.Tests
{
    public class LibCap_RemoveAsset
    {
        [Test]
        public static void RemoveAsset() {
            var builder = new CapBuilder();

            var check = builder.AddAsset(Const.DG_TOCOCUMBA, AssetType.MAP);
            if (!check.IsOk)
                Assert.Fail(string.Format("Error: {0}", check.Msg));
            
            check = builder.AddAsset(Const.DG_TOCOCUMBA2, AssetType.MAP);
            if (!check.IsOk)
                Assert.Fail(string.Format("Error: {0}", check.Msg));
                
            Assert.AreEqual(builder.Count, 6);

            check = builder.RemoveAsset(Const.DG_TOCOCUMBA);
            if (!check.IsOk)
                Assert.Fail(string.Format("Error: {0}", check.Msg));

            Assert.AreEqual(builder.Count, 5);

            check = builder.RemoveAsset(Const.DG_TOCOCUMBA2);
            if (!check.IsOk)
                Assert.Fail(string.Format("Error: {0}", check.Msg));

            Assert.AreEqual(builder.Count, 0);
        }
    }
}