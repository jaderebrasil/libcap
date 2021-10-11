using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using CapJson = LibCap.Json;
using System.Linq;

//using Newtonsoft.Json

namespace LibCap
{
    public class CapBuilder
    {
        private Dictionary<string, FileData> _content = new Dictionary<string, FileData>();
        public int Count => _content.Count;
        private string _tmpPath;

        public CapBuilder()
        {
            _content = new Dictionary<string, FileData>();
            _tmpPath = Path.GetFullPath(".tmp/CapBuilder/", Directory.GetCurrentDirectory());

            if (Directory.Exists(_tmpPath)) {
                Directory.Delete(_tmpPath, true);
            }

            Directory.CreateDirectory(_tmpPath);
        }
        
        ~CapBuilder()
        {
            Directory.Delete(_tmpPath, true);
        }

        private bool CheckDependencies() {
            string json, assetName;

            foreach (var (path, file) in _content) {
                switch (file.Asset, file.Type) {
                    case (AssetType.MAP, FileType.JSON):
                        json = File.ReadAllText(file.TmpPath(_tmpPath));
                        dynamic map = JsonConvert.DeserializeObject(json);
                        var parentDir = Directory.GetParent(path).FullName;

                        foreach (var ts in map["tilesets"])  {
                            assetName = CapUtils.GetAssetNameFromPath(ts["source"].ToString());

                            try {
                                var tileFile = _content.First(f => f.Value.Type == FileType.JSON
                                             && string.Equals(f.Value.AssetName, assetName));
                                
                                ts["source"] = Path.GetRelativePath(
                                    Path.GetFullPath("Maps", _tmpPath), 
                                    tileFile.Value.TmpPath(_tmpPath)
                                );
                            } catch {
                                return false;
                            }
                        }
                        
                        json = JsonConvert.SerializeObject(map, Formatting.Indented);

                        File.Delete(file.TmpPath(_tmpPath));
                        File.WriteAllText(file.TmpPath(_tmpPath), json);
                        break;
                        
                    case (AssetType.TILESET, FileType.JSON):
                        json = File.ReadAllText(file.TmpPath(_tmpPath));
                        dynamic tileset = JsonConvert.DeserializeObject(json);
                        
                        assetName = CapUtils.GetAssetNameFromPath(path);
                        var tilesetImgName = Path.GetFileName(tileset["image"].ToString());

                        try {
                            var imgFile = _content.First(i => string.Equals(i.Value.AssetName, assetName)
                                         && string.Equals(i.Value.Name, tilesetImgName));
                            
                            tileset["image"] = imgFile.Value.Name;
                            json = JsonConvert.SerializeObject(tileset, Formatting.Indented);
                            
                            File.Delete(file.TmpPath(_tmpPath));
                            File.WriteAllText(file.TmpPath(_tmpPath), json);
                        } catch {
                            return false;
                        }
                        break;
                    
                    default:
                        break;
                }
            }
            
            return true;
        }

        //
        // Summary:
        //     Verify if CapBuilder contains a file `path`.
        //
        public bool ContainsFile(string path) {
            return this._content.ContainsKey(path);
        }
        
        //
        // Summary:
        //     Add a Asset to CapBuilder. The json will be parsed and every
        //     dependent file will be checked.
        //
        // Returns:
        //     It fails if the file or its dependents doesn't exists, 
        //     the type is not valid or the file was already contained in CapBuilder.
        // 
        public CapError AddAsset(string jsonPath, AssetType type) {
            (List<FileData> Ok, CapError Error) filesResult = (null, CapError.NoError());
            List<FileData> files = filesResult.Ok;

            switch (type) {
                case AssetType.MAP:
                    filesResult = CapUtils.ParseMapFile(jsonPath);
                    
                    if (!filesResult.Error.IsOk) {
                        return filesResult.Error;
                    }

                    files = filesResult.Ok;
                    break;
                    
                case AssetType.TILESET:
                    filesResult = CapUtils.ParseTilesetFile(jsonPath);

                    if (!filesResult.Error.IsOk) {
                        return filesResult.Error;
                    }

                    files = filesResult.Ok;
                    break;
            } foreach (var file in files) {
                AddFile(file);
            }
            
            return CapError.NoError();
        }
        
        internal void VerifyBuildDirs(string assetDir, string assetName) {
            if (string.IsNullOrEmpty(assetDir.Trim()))
                return;

            var pathAssetDir = Path.GetFullPath(assetDir, _tmpPath);
            if (!Directory.Exists(pathAssetDir))
                Directory.CreateDirectory(pathAssetDir);
            
            if (string.IsNullOrEmpty(assetName))
                return;

            var pathAssetName = Path.GetFullPath(assetName, pathAssetDir);
            if (!Directory.Exists(pathAssetName))
                Directory.CreateDirectory(pathAssetName);
        }

        //
        // Summary:
        //     Add a file to CapBuilder.
        //
        // Returns:
        //     True if the file was added to preprocessor. It fails if
        //     the file doesn't exist, the type is not valid or the file
        //     was already contained in CapBuilder.
        // 
        internal bool AddFile(FileData file) {
            if (!CapUtils.IsValidFile(file.OriginalPath, file.Type) || ContainsFile(file.OriginalPath)) {
                return false; 
            }

            VerifyBuildDirs(file.AssetDir, file.AssetName);
            
            if (!File.Exists(file.TmpPath(_tmpPath))) {
                File.Copy(file.OriginalPath, file.TmpPath(_tmpPath));
                this._content.Add(file.OriginalPath, file);
            }

            return true;
        }
        
        //
        // Summary:
        //     Remove a file from CapBuilder.
        //
        // Returns:
        //     True if the file was removed. It fails when
        //     the preprocessor not contains the file `path`.
        // 
        public bool RemoveFile(string path) {
            if (   !string.IsNullOrEmpty(path) 
                && ContainsFile(path)) {
                var file = this._content[path];
                File.Delete(file.TmpPath(_tmpPath));
                this._content.Remove(path);
                return true;
            }

            return false;
        }
        
        //
        // Summary:
        //     Export all files added in the builder to a .cap file.
        //
        // Returns:
        //     True if the export was successful. If exists a file in
        //     `path` this function will fail unless `fReplace = true`;
        // 
        public bool ExportCap(string path, bool fReplace) {
            if (File.Exists(path)) {
                if (!fReplace)
                    return false;
                
                try {
                    File.Delete(path);
                } catch {
                    return false;
                }
            }
            
            if (!CheckDependencies())
                return false;

            ZipFile.CreateFromDirectory(_tmpPath, path);
            return true;
        }
        
        //
        // Summary:
        //     Import all files in a .cap and return a CapBuilder 
        //     with all files in it.
        // 
        public static CapBuilder FromCap(string path) {
            return new CapBuilder(); 
        }
        
        //
        // Summary:
        //     Extract all files in a .cap file to a given `path`.
        //
        // Returns:
        //     True if the extraction was successful. If `fReplace = true`
        //     any collided files will be deleted and replaced.
        // 
        public bool ExtractCap(string capPath, string dstPath, bool fReplace) {
            return false;
        }
    }
}
