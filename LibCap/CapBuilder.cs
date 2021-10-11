using System;
using System.Collections.Generic;
using System.IO;

//using Newtonsoft.Json

namespace LibCap
{
    public class CapBuilder
    {
        private Dictionary<string, CapUtils.FileData> _content = new Dictionary<string, CapUtils.FileData>();
        public int Count => _content.Count;
        public List<string> FileList => new List<string>(_content.Keys);

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
            (List<CapUtils.FileData> Ok, CapError Error) filesResult = (null, CapError.NoError());
            List<CapUtils.FileData> files = filesResult.Ok;

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
            }

            foreach (var file in files) {
                AddFile(file.Name, file.Type);
            }
            
            return CapError.NoError();
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
        internal bool AddFile(string path, CapUtils.FileType type) {
            if (!CapUtils.IsValidFile(path, type) || ContainsFile(path)) {
                return false; 
            }

            this._content.Add(path, new CapUtils.FileData() { 
                Name = Path.GetFileName(path),
                Type = type
            });
            
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
        public bool CreateCap(string path, bool fReplace) {
            if (File.Exists(path)) {
                if (!fReplace)
                    return false;
                
                try {
                    File.Delete(path);
                } catch {
                    return false;
                }
            }
            
            return false;
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
