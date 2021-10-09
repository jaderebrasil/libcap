using System.Collections.Generic;

// 
// Summary:
//      used for Json internal stuff
//
namespace LibCap.Json {
    public class CapJsonMap {
        public class JsonTileset {
            public int firstgid;
            public string source;
        }
        public List<JsonTileset> tilesets;
    }
    
    public class CapJsonTileset {
       public string image;
    }
}