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
    
    internal enum FileType {
        JSON,
        PNG,
        ANY
    }

    internal struct FileData
    {
        public readonly string Name;
        public readonly string OriginalPath;
        public readonly FileType Type;
        public readonly AssetType Asset;
        public readonly string AssetDir;
        public readonly string AssetName;

        public string RelativeTmpPath => string.Format("{0}{1}{2}", AssetDir, AssetName, Name);
        public string TmpPath(string tmpPath) => Path.GetFullPath(RelativeTmpPath, tmpPath);
        public FileData(string path, FileType type, AssetType asset, string assetName)
        {
            Name = Path.GetFileName(path);
            OriginalPath = path;
            Type = type;
            Asset = asset;
            AssetName = assetName;
            
            AssetDir = "";
            switch (asset) {
                case AssetType.MAP:
                    AssetDir = "Maps/";
                    break; 
                    
                case AssetType.TILESET:
                    AssetDir = "Tilesets/";
                    break;
                    
                case AssetType.RPGSYSTEM:
                    AssetDir = "RPGSys/";
                    break;
                    
                case AssetType.META:
                    AssetDir = "Meta/";
                    break;
            }
        }
    }

    
    internal static class CapUtils {
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
                    
                case FileType.ANY:
                    return true;
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

                case FileType.ANY:
                    return CapError.NoError();
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
            if (!checkError.IsOk) {
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
                    
                    if (!result.Error.IsOk)
                        return (null, result.Error);

                    source = result.Ok;
                } else {
                    source = Path.GetFullPath(source, parentDir);
                }
                
                var tilesetResult = ParseTilesetFile(source);
                if (!tilesetResult.Error.IsOk) {
                    return (null, tilesetResult.Error);
                }

                res.AddRange(tilesetResult.Ok);
            }
            
            res.Add(new FileData(jsonPath, FileType.JSON, AssetType.MAP, ""));
            return (res, CapError.NoError());
        }
        
        internal static (List<FileData> Ok, CapError Error) ParseTilesetFile(string jsonPath) {
            var res = new List<FileData>();
            
            var errorCheck = VerifyFileForErrors(jsonPath, FileType.JSON);
            if (!errorCheck.IsOk) {
                return (null, errorCheck);
            }
            
            string json = File.ReadAllText(jsonPath);
            var tileset = JsonConvert.DeserializeObject<CapJson.CapJsonTileset>(json);

            var parentDir = Directory.GetParent(jsonPath).FullName;
            
            if (string.IsNullOrEmpty(tileset.image)) {
                return (null, new CapError(
                    CapError.ErrorTypes.FileIsInvalid, 
                    string.Format("The tileset {0} has an empty image source.", jsonPath)
                ));
            }

            var imagePath = Path.GetFullPath(tileset.image, parentDir);
            errorCheck = VerifyFileForErrors(imagePath, FileType.PNG);
            if (!errorCheck.IsOk) {
                return (null, errorCheck);
            }
            
            var assetName = GetAssetNameFromPath(jsonPath);
            res.Add(new FileData(jsonPath, FileType.JSON, AssetType.TILESET, assetName));
            res.Add(new FileData(imagePath, FileType.PNG, AssetType.TILESET, assetName));

            return (res, CapError.NoError());
        }
        
        internal static string GetAssetNameFromPath(string path) {
            var assetName = Path.GetFileNameWithoutExtension(path);
            if (!string.IsNullOrEmpty(assetName))
                assetName += "/";
            
            return assetName;
        }
    }
}