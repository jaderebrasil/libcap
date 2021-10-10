using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using CapJson = LibCap.Json;

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
        
        internal static CapError VerifyFileForErrors(string path, FileType type) {
            if (string.IsNullOrEmpty(path)) {
                return new CapError(
                    CapError.ErrorTypes.FileIsInvalid,
                    "the parameter `path` can't be empty or null."   
                ); 
            }
            
            if (!File.Exists(path)) {
                return new CapError(
                    CapError.ErrorTypes.FileNotFound,
                    string.Format("{0} not found.", path)  
                );
            }

            
            switch (type) {
                case FileType.JSON:
                    if (path.Length > 5 && path.Substring(path.Length - 5).Equals(".json"))
                        return CapError.NoError();
                    break;

                case FileType.PNG:
                    if (path.Length > 4 && path.Substring(path.Length - 4).Equals(".png"))
                        return CapError.NoError();
                    break;
            }
            
            return new CapError(
                CapError.ErrorTypes.FileIsInvalid,
                string.Format("{0} should be a {1} file.", path, type.ToString("g"))
            );
        }
        
        internal static (string Ok, CapError Error) FileTsxToJson(string tsxPath, string parentDir) {
            if (tsxPath.Length < 5)
                return (null, new CapError(
                    CapError.ErrorTypes.FileIsInvalid,
                    string.Format("{0} is too short to be a valid filename.", tsxPath)
                ));
            
            var jsonPath = string.Concat(tsxPath.Substring(0, tsxPath.Length - 3), "json");
            var fullPath = Path.GetFullPath(jsonPath, parentDir);
            
            if (File.Exists(fullPath))
                return (fullPath, CapError.NoError());
            
            return (null, new CapError(
                CapError.ErrorTypes.FileNotFound,
                string.Format("{0} was not found.", fullPath)
            ));
        }
        
        internal static (List<FileData> Ok, CapError Error) ParseMapFile(string jsonPath) {
            var res = new List<FileData>();
            
            var checkError = VerifyFileForErrors(jsonPath, FileType.JSON);
            if (checkError.HasError) {
                return (null, checkError);
            }
            
            var parentDir = Directory.GetParent(jsonPath).FullName;
            string json = File.ReadAllText(jsonPath);
            var map = JsonConvert.DeserializeObject<CapJson.CapJsonMap>(json);

            foreach (var tileset in map.tilesets) {
                var source = tileset.source;
                var len = source.Length;

                if (len > 4 && source.Substring(len - 4).Equals(".tsx")) {
                    var result = FileTsxToJson(source, parentDir);
                    
                    if (result.Error.HasError)
                        return (null, result.Error);

                    source = result.Ok;
                }
                
                var tilesetResult = ParseTilesetFile(source);
                if (tilesetResult.Error.HasError) {
                    return (null, tilesetResult.Error);
                }

                res.AddRange(tilesetResult.Ok);
            }
            
            res.Add(new FileData(jsonPath, FileType.JSON));
            return (res, CapError.NoError());
        }
        
        internal static (List<FileData> Ok, CapError Error) ParseTilesetFile(string jsonPath) {
            var res = new List<FileData>();
            
            var errorCheck = VerifyFileForErrors(jsonPath, FileType.JSON);
            if (errorCheck.HasError) {
                return (null, errorCheck);
            }
            
            string json = File.ReadAllText(jsonPath);
            var tileset = JsonConvert.DeserializeObject<CapJson.CapJsonTileset>(json);

            var parentDir = Directory.GetParent(jsonPath).FullName;
            var imagePath = Path.GetFullPath(tileset.image, parentDir);
            
            errorCheck = VerifyFileForErrors(imagePath, FileType.PNG);
            if (errorCheck.HasError) {
                return (null, errorCheck);
            }
            
            res.Add(new FileData(jsonPath, FileType.JSON));
            res.Add(new FileData(imagePath, FileType.PNG));

            return (res, CapError.NoError());
        }
    }
}