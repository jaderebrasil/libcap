﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using System.Linq;

namespace LibCap
{
    public class CapBuilder
    {
        private Dictionary<string, FileData> _content = new Dictionary<string, FileData>();
        public int Count => _content.Count;
        private string _tmpPath;
        private bool _autoDeleteTmp;

        public CapBuilder(bool autoDeleteTmp = true)
        {
            _content = new Dictionary<string, FileData>();
            _tmpPath = Path.GetFullPath(".tmp/CapBuilder/", Directory.GetCurrentDirectory());
            _autoDeleteTmp = autoDeleteTmp;

            if (Directory.Exists(_tmpPath))
            {
                Directory.Delete(_tmpPath, true);
            }

            Directory.CreateDirectory(_tmpPath);
        }

        public CapBuilder(string tmpPath, bool autoDeleteTmp = true, bool cleanTmpOnCreate = true)
        {
            _content = new Dictionary<string, FileData>();
            _tmpPath = tmpPath;
            _autoDeleteTmp = autoDeleteTmp;

            if (Directory.Exists(_tmpPath) && cleanTmpOnCreate)
            {
                Directory.Delete(_tmpPath, true);
            }

            Directory.CreateDirectory(_tmpPath);
        }

        ~CapBuilder()
        {
            if (_autoDeleteTmp)
            {
                RemoveTmpDirectory();
            }

        }
        public void RemoveTmpDirectory()
        {
            if (Directory.Exists(_tmpPath))
            {
                Directory.Delete(_tmpPath, true);
            }
        }

        private bool CheckAllDependencies()
        {
            bool res = true;

            foreach (var (path, file) in _content)
            {
                res = CheckFileDependencie(path, file);

                if (!res) break;
            }

            return res;
        }

        private bool CheckFileDependencie(string path, FileData file)
        {
            string json, assetName;

            switch (file.Asset, file.Type)
            {
                case (AssetType.MAP, FileType.JSON):
                    json = File.ReadAllText(file.TmpPath(_tmpPath));
                    dynamic map = JsonConvert.DeserializeObject(json);
                    var parentDir = Directory.GetParent(path).FullName;

                    foreach (var ts in map["tilesets"])
                    {
                        assetName = CapUtils.GetAssetNameFromPath(ts["source"].ToString());

                        try
                        {
                            var tileFile = _content.First(f => f.Value.Type == FileType.JSON
                                         && string.Equals(f.Value.AssetName, assetName));

                            ts["source"] = tileFile.Value.RelativeTmpPath;
                        }
                        catch
                        {
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

                    try
                    {
                        var imgFile = _content.First(i => string.Equals(i.Value.AssetName, assetName)
                                     && string.Equals(i.Value.Name, tilesetImgName));

                        tileset["image"] = imgFile.Value.Name;
                        json = JsonConvert.SerializeObject(tileset, Formatting.Indented);

                        File.Delete(file.TmpPath(_tmpPath));
                        File.WriteAllText(file.TmpPath(_tmpPath), json);
                    }
                    catch
                    {
                        return false;
                    }
                    break;

                default:
                    break;
            }

            return true;
        }

        //
        // Summary:
        //     Verify if CapBuilder contains a file `path`.
        //
        public bool ContainsFile(string path)
        {
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
        public CapError AddAsset(string filePath, AssetType type)
        {
            (List<FileData> Ok, CapError Error) filesResult = (null, CapError.NoError());
            List<FileData> files = filesResult.Ok;
            CapError check;

            switch (type)
            {
                case AssetType.MAP:
                    filesResult = CapUtils.ParseMapFile(filePath);

                    if (!filesResult.Error.IsOk)
                    {
                        return filesResult.Error;
                    }

                    files = filesResult.Ok;
                    break;

                case AssetType.TILESET:
                    filesResult = CapUtils.ParseTilesetFile(filePath);

                    if (!filesResult.Error.IsOk)
                    {
                        return filesResult.Error;
                    }

                    files = filesResult.Ok;
                    break;

                case AssetType.META:
                    check = CapUtils.VerifyFileForErrors(filePath, FileType.ANY);
                    if (!check.IsOk)
                    {
                        return check;
                    }

                    files = new List<FileData>();
                    files.Add(new FileData(
                        filePath,
                        FileType.ANY,
                        AssetType.META,
                        ""
                    ));

                    break;

                case AssetType.RPGSYSTEM:
                    check = CapUtils.VerifyFileForErrors(filePath, FileType.JSON);
                    if (!check.IsOk)
                    {
                        return check;
                    }

                    files = new List<FileData>();
                    files.Add(new FileData(
                        filePath,
                        FileType.JSON,
                        AssetType.RPGSYSTEM,
                        ""
                    ));

                    break;
            }

            foreach (var file in files)
            {
                AddFile(file);
            }

            return CapError.NoError();
        }

        internal void VerifyBuildDirs(string assetDir, string assetName)
        {
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
        internal bool AddFile(FileData file)
        {
            if (!CapUtils.IsValidFile(file.OriginalPath, file.Type))
            {
                return false;
            }

            if (ContainsFile(file.OriginalPath))
            {
                var contentFile = _content[file.OriginalPath];

                foreach (var dep in file.Deps)
                {
                    if (!contentFile.Deps.Contains(dep))
                    {
                        contentFile.Deps.Add(dep);
                    }
                }

                return true;
            }

            VerifyBuildDirs(file.AssetDir, file.AssetName);

            if (!File.Exists(file.TmpPath(_tmpPath)))
            {
                File.Copy(file.OriginalPath, file.TmpPath(_tmpPath));
                this._content.Add(file.OriginalPath, file);
            }

            CheckFileDependencie(file.OriginalPath, file);

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
        private bool RemoveFile(string path)
        {
            if (!string.IsNullOrEmpty(path)
                && ContainsFile(path))
            {
                var file = this._content[path];
                File.Delete(file.TmpPath(_tmpPath));
                this._content.Remove(path);

                var filesToRemove = new List<FileData>();

                foreach (var (_, f) in this._content)
                {
                    if (f.Deps.Contains(path))
                    {
                        f.Deps.Remove(path);

                        if (f.Deps.Count == 0)
                        {
                            filesToRemove.Add(f);
                        }
                    }
                }

                foreach (var f in filesToRemove)
                {
                    RemoveFile(f.OriginalPath);
                }

                foreach (var f in filesToRemove)
                {
                    if (f.Asset == AssetType.TILESET)
                    {
                        var dir = f.TmpParent(_tmpPath);

                        if (Directory.Exists(dir))
                        {
                            Directory.Delete(f.TmpParent(_tmpPath), true);
                        }
                    }
                }

                return true;
            }

            return false;
        }

        public CapError RemoveAsset(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return new CapError(
                    CapError.ErrorTypes.FileIsInvalid,
                    "Argument `filePath` can't be null or empty."
                );
            }

            if (!ContainsFile(filePath))
            {
                return new CapError(
                    CapError.ErrorTypes.FileNotFound,
                    string.Format("{0} was not found in builder contents.", filePath)
                );
            }

            var file = this._content[filePath];

            if (file.Deps.Count > 0)
            {
                return new CapError(
                    CapError.ErrorTypes.FileIsInvalid,
                    string.Format("{0} is a required dependency for {1}",
                                   filePath,
                                   String.Join(", ", file.Deps.ToArray()))
                );
            }

            RemoveFile(filePath);

            return CapError.NoError();
        }

        //
        // Summary:
        //     Export all files added in the builder to a .cap file.
        //
        // Returns:
        //     True if the export was successful. If exists a file in
        //     `path` this function will fail unless `fReplace = true`;
        // 
        public bool ExportCap(string path, bool fReplace)
        {
            if (File.Exists(path))
            {
                if (!fReplace)
                    return false;

                try
                {
                    File.Delete(path);
                }
                catch
                {
                    return false;
                }
            }

            if (!CheckAllDependencies())
                return false;

            ZipFile.CreateFromDirectory(_tmpPath, path);
            return true;
        }

        public void AddAssetsFromDir(string assetPath, AssetType assetType)
        {
            if (!Directory.Exists(assetPath))
                return;

            foreach (var filePath in Directory.GetFiles(assetPath))
            {
                AddAsset(filePath, assetType);
            }
        }

        //
        // Summary:
        //     Import all files in a .cap        
        // 
        public CapError ImportCap(string path)
        {
            string tmpExpPath = ".tmp/CapBuilder.Extract";

            if (Directory.Exists(tmpExpPath))
                Directory.Delete(tmpExpPath, true);

            Directory.CreateDirectory(tmpExpPath);

            var check = ExtractCap(path, tmpExpPath, true);
            if (!check.IsOk)
                return check;

            AddAssetsFromDir(string.Format("{0}/{1}", tmpExpPath, "Maps"), AssetType.MAP);
            AddAssetsFromDir(string.Format("{0}/{1}", tmpExpPath, "Tilesets"), AssetType.TILESET);
            AddAssetsFromDir(string.Format("{0}/{1}", tmpExpPath, "Meta"), AssetType.META);
            AddAssetsFromDir(string.Format("{0}/{1}", tmpExpPath, "RPGSys"), AssetType.RPGSYSTEM);

            return CapError.NoError();
        }

        //
        // Summary:
        //     Import all files in a path, set autodelete to false and this path
        //     will be used as temporary path.
        // 
        public CapError ImportPath(string path)
        {
            if (!Directory.Exists(path))
                return new CapError(
                    CapError.ErrorTypes.FileNotFound,
                    string.Format("{0} not exists.", path)
                );
            
            AddAssetsFromDir(string.Format("{0}/{1}", path, "Maps"), AssetType.MAP);
            AddAssetsFromDir(string.Format("{0}/{1}", path, "Tilesets"), AssetType.TILESET);
            AddAssetsFromDir(string.Format("{0}/{1}", path, "Meta"), AssetType.META);
            AddAssetsFromDir(string.Format("{0}/{1}", path, "RPGSys"), AssetType.RPGSYSTEM);

            return CapError.NoError();
        }

        
        //
        // Summary:
        //     Extract all files in a .cap file to a given `path`.
        //
        // Returns:
        //     True if the extraction was successful.
        // 
        public CapError ExtractCap(string capPath, string dstPath, bool overwriteFiles)
        {
            if (!File.Exists(capPath))
            {
                return new CapError(
                    CapError.ErrorTypes.FileNotFound,
                    string.Format("{0} file not found.", capPath)
                );
            }

            if (!Directory.Exists(dstPath))
            {
                return new CapError(
                    CapError.ErrorTypes.FileNotFound,
                    string.Format("{0} directoty not found.", capPath)
                );
            }

            ZipFile.ExtractToDirectory(capPath, dstPath, overwriteFiles);
            return CapError.NoError();
        }
    }
}
