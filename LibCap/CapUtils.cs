using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using CapJson = LibCap.Json;
using CapResListFile = LibCap.CapResult<System.Collections.Generic.List<LibCap.CapUtils.FileData>>;

namespace LibCap {
    public enum AssetType
    { 
        META,
        MAP, 
        TILESET,
        RPGSYSTEM
    }
    
    internal static class CapUtils {
        internal enum FileType {
            JSON,
            PNG
        }

        internal struct FileData
        {
            public string Name;
            public FileType Type;

            public FileData(string name, FileType type)
            {
                Name = name;
                Type = type;
            }
        }

        //
        // Summary:
        //     Verify if the file `path` contains a compatible file for
        //     the informed AssetType. 
        //
        internal static bool IsValidFile(string path, FileType type) {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) {
                return false; 
            }
            
            switch (type) {
                case FileType.JSON:
                    if (path.Length > 5)
                        return path.Substring(path.Length - 5).Equals(".json");
                    break;

                case FileType.PNG:
                    if (path.Length > 4)
                        return path.Substring(path.Length - 4).Equals(".png");
                    break;
            }
            
            return false;
        }
        
        internal static CapErrOption VerifyFileForErrors(string path, FileType type) {
            if (string.IsNullOrEmpty(path)) {
                return CapErrOption.SomeErr(new CapError(
                    CapError.ErrorTypes.FileIsInvalid,
                    "the parameter `path` can't be empty or null."   
                )); 
            }
            
            if (!File.Exists(path)) {
                return CapErrOption.SomeErr(new CapError(
                    CapError.ErrorTypes.FileNotFound,
                    string.Format("{0} not found.", path)  
                ));
            }

            
            switch (type) {
                case FileType.JSON:
                    if (path.Length > 5 && path.Substring(path.Length - 5).Equals(".json"))
                        return CapErrOption.NoErr();
                    break;

                case FileType.PNG:
                    if (path.Length > 4 && path.Substring(path.Length - 4).Equals(".png"))
                        return CapErrOption.NoErr();
                    break;
            }
            
            return CapErrOption.SomeErr(new CapError(
                CapError.ErrorTypes.FileIsInvalid,
                string.Format("{0} should be a {1} file.", path, type.ToString("g"))
            ));
        }
        
        internal static CapResult<string> FileTsxToJson(string tsxPath, string parentDir) {
            if (tsxPath.Length < 5)
                return CapResult<string>.Err(new CapError(
                    CapError.ErrorTypes.FileIsInvalid,
                    string.Format("{0} is too short to be a valid filename.", tsxPath)
                ));
            
            var jsonPath = string.Concat(tsxPath.Substring(0, tsxPath.Length - 3), "json");
            var fullPath = Path.GetFullPath(jsonPath, parentDir);
            
            if (File.Exists(fullPath))
                return CapResult<string>.Ok(fullPath);
            
            return CapResult<string>.Err(new CapError(
                CapError.ErrorTypes.FileNotFound,
                string.Format("{0} was not found.", fullPath)
            ));
        }
        
        internal static CapResListFile ParseMapFile(string jsonPath) {
            var res = new List<FileData>();
            
            if (!IsValidFile(jsonPath, FileType.JSON)) {
                return CapResListFile.Err(new CapError(
                    CapError.ErrorTypes.FileIsInvalid,
                    string.Format("{0} is invalid.", jsonPath)
                ));
            }
            
            var parentDir = Directory.GetParent(jsonPath).FullName;
            string json = File.ReadAllText(jsonPath);
            var map = JsonConvert.DeserializeObject<CapJson.CapJsonMap>(json);

            foreach (var tileset in map.tilesets) {
                var source = tileset.source;
                var len = source.Length;

                if (len > 4 && source.Substring(len - 4).Equals(".tsx")) {
                    var result = FileTsxToJson(source, parentDir);
                    
                    if (result.IsOk) {
                        source = result.OkValue();
                    } else {
                        return CapResListFile.Err(result.ErrValue());
                    }
                }
                
                var tilesetResult = ParseTilesetFile(source);
                if (!tilesetResult.IsOk) {
                    return CapResListFile.Err(tilesetResult.ErrValue());
                }

                res.AddRange(tilesetResult.OkValue());
            }
            
            res.Add(new FileData(jsonPath, FileType.JSON));

            return CapResListFile.Ok(res);
        }
        
        internal static CapResListFile ParseTilesetFile(string jsonPath) {
            var res = new List<FileData>();
            
            var check = VerifyFileForErrors(jsonPath, FileType.JSON);
            if (check.HasSomeError) {
                return CapResListFile.Err(check.ErrValue());
            }
            
            string json = File.ReadAllText(jsonPath);
            var tileset = JsonConvert.DeserializeObject<CapJson.CapJsonTileset>(json);

            var parentDir = Directory.GetParent(jsonPath).FullName;
            var imagePath = Path.GetFullPath(tileset.image, parentDir);
            
            check = VerifyFileForErrors(imagePath, FileType.PNG);
            if (check.HasSomeError) {
                return CapResListFile.Err(check.ErrValue());
            }
            
            res.Add(new FileData(jsonPath, FileType.JSON));
            res.Add(new FileData(imagePath, FileType.PNG));

            return CapResListFile.Ok(res);
        }
    }
}