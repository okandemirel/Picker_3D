using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace AssetInventory
{
    public sealed class UnityPackageImporter : AssetImporter
    {
        private const int BREAK_INTERVAL = 10;

        public async Task IndexRoughLocal(FolderSpec spec, bool fromAssetStore, bool force = false)
        {
            ResetState(false);

            int progressId = MetaProgress.Start("Updating asset inventory index");
            string[] packages = Directory.GetFiles(spec.GetLocation(true), "*.unitypackage", SearchOption.AllDirectories);
            MainCount = packages.Length;
            for (int i = 0; i < packages.Length; i++)
            {
                if (CancellationRequested) break;

                string package = packages[i];
                MetaProgress.Report(progressId, i + 1, packages.Length, package);
                if (i % 50 == 0) await Task.Yield(); // let editor breath

                Asset asset = HandlePackage(fromAssetStore, package, i, force);
                if (asset == null) continue;

                if (spec.assignTag && !string.IsNullOrWhiteSpace(spec.tag))
                {
                    AssetInventory.AddTagAssignment(asset.Id, spec.tag, TagAssignment.Target.Package, fromAssetStore);
                }
            }
            MetaProgress.Remove(progressId);
            ResetState(true);
        }

        public IEnumerator IndexRoughOnline(List<AssetInfo> packages, Action callback)
        {
            ResetState(false);

            int progressId = MetaProgress.Start("Downloading and indexing assets");

            MainCount = packages.Count;
            for (int i = 0; i < packages.Count; i++)
            {
                if (CancellationRequested) break;

                AssetInfo info = packages[i];
                MetaProgress.Report(progressId, i + 1, packages.Count, info.GetDisplayName());

                if (info.PackageDownloader == null) info.PackageDownloader = new AssetDownloader(info);
                if (!info.PackageDownloader.IsDownloadSupported()) continue;

                CurrentMain = "Downloading package...";
                MainProgress = i + 1;
                CurrentSub = info.GetDisplayName();
                SubCount = 0;
                SubProgress = 0;

                info.PackageDownloader.Download();
                do
                {
                    if (CancellationRequested) break; // download will finish in that case and not be removed

                    AssetDownloadState state = info.PackageDownloader.GetState();
                    SubCount = Mathf.RoundToInt(state.bytesTotal / 1024f / 1024f);
                    SubProgress = Mathf.RoundToInt(state.bytesDownloaded / 1024f / 1024f);
                    yield return null;
                } while (info.IsDownloading());
                info.Refresh();
                if (!info.Downloaded)
                {
                    Debug.LogError($"Downloading {info} failed. Continuing with next package.");
                    continue;
                }

                HandlePackage(true, info.Location, i);
                Task task = IndexDetails(info.AssetId);
                yield return new WaitWhile(() => !task.IsCompleted);

                // remove again
                if (!AssetInventory.Config.keepAutoDownloads)
                {
                    // perform backup before deleting, as otherwise the file would not be considered
                    if (AssetInventory.Config.createBackups)
                    {
                        AssetBackup backup = new AssetBackup();
                        Task task2 = backup.Backup(info.AssetId);
                        yield return new WaitWhile(() => !task2.IsCompleted);
                    }

                    File.Delete(info.GetLocation(true));
                    info.Location = null;
                    DBAdapter.DB.Execute("update Asset set Location=null where Id=?", info.AssetId);
                }

                info.Refresh();
            }

            MetaProgress.Remove(progressId);
            ResetState(true);

            callback?.Invoke();
        }

        private Asset HandlePackage(bool fromAssetStore, string package, int currentIndex, bool force = false)
        {
            string relPackage = AssetInventory.MakeRelative(package);

            // create asset and add additional information from file system
            Asset asset = new Asset();
            asset.Location = relPackage;
            asset.SafeName = Path.GetFileNameWithoutExtension(package);
            if (fromAssetStore)
            {
                asset.AssetSource = Asset.Source.AssetStorePackage;
                DirectoryInfo dirInfo = new DirectoryInfo(Path.GetDirectoryName(package));
                asset.SafeCategory = dirInfo.Name;
                asset.SafePublisher = dirInfo.Parent.Name;
                if (string.IsNullOrEmpty(asset.DisplayCategory))
                {
                    asset.DisplayCategory = System.Text.RegularExpressions.Regex.Replace(asset.SafeCategory, "([a-z])([A-Z])", "$1/$2", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
                }
            }
            else
            {
                asset.AssetSource = Asset.Source.CustomPackage;
            }

            // try to read contained upload details
            AssetHeader header = ReadHeader(package);
            if (header != null)
            {
                if (int.TryParse(header.id, out int id))
                {
                    asset.ForeignId = id;
                }
            }

            // skip unchanged 
            Asset existing = Fetch(asset);
            long size; // determine late for performance, especially with many exclusions
            if (existing != null)
            {
                if (existing.Exclude) return null;

                size = new FileInfo(package).Length;
                if (!force && existing.CurrentState == Asset.State.Done && existing.PackageSize == size && existing.Location == relPackage) return existing;

                if (string.IsNullOrEmpty(existing.SafeCategory)) existing.SafeCategory = asset.SafeCategory;
                if (string.IsNullOrEmpty(existing.DisplayCategory)) existing.DisplayCategory = asset.DisplayCategory;
                if (string.IsNullOrEmpty(existing.SafePublisher)) existing.SafePublisher = asset.SafePublisher;
                if (string.IsNullOrEmpty(existing.SafeName)) existing.SafeName = asset.SafeName;

                asset = existing;
            }
            else
            {
                size = new FileInfo(package).Length;
                if (AssetInventory.Config.excludeByDefault) asset.Exclude = true;
                if (AssetInventory.Config.backupByDefault) asset.Backup = true;
            }

            // update progress only if really doing work to save refresh time in UI
            CurrentMain = package;
            MainProgress = currentIndex + 1;

            ApplyHeader(header, asset);

            Asset.State previousState = asset.CurrentState;
            if (!force) asset.CurrentState = Asset.State.InProcess;
            asset.Location = relPackage;
            asset.PackageSize = size;
            if (previousState != asset.CurrentState) asset.ETag = null; // force rechecking of download metadata
            Persist(asset);

            return asset;
        }

        public static void ApplyHeader(AssetHeader header, Asset asset)
        {
            if (header == null) return;

            if (!string.IsNullOrWhiteSpace(header.version)) asset.Version = header.version;
            if (!string.IsNullOrWhiteSpace(header.title)) asset.DisplayName = header.title;
            if (!string.IsNullOrWhiteSpace(header.description)) asset.Description = header.description;
            if (header.publisher != null) asset.DisplayPublisher = header.publisher.label;
            if (header.category != null) asset.DisplayCategory = header.category.label;

            if (int.TryParse(header.id, out int id))
            {
                asset.ForeignId = id;
            }
        }

        public async Task IndexDetails(int assetId = 0)
        {
            ResetState(false);
            int progressId = MetaProgress.Start("Indexing package contents");
            List<Asset> assets;
            if (assetId == 0)
            {
                assets = DBAdapter.DB.Table<Asset>().Where(asset => !asset.Exclude && asset.CurrentState == Asset.State.InProcess && asset.AssetSource != Asset.Source.Package).ToList();
            }
            else
            {
                assets = DBAdapter.DB.Table<Asset>().Where(asset => asset.Id == assetId && asset.AssetSource != Asset.Source.Package).ToList();
            }
            MainCount = assets.Count;
            for (int i = 0; i < assets.Count; i++)
            {
                if (CancellationRequested) break;

                MetaProgress.Report(progressId, i + 1, assets.Count, assets[i].GetLocation(true));
                CurrentMain = assets[i].GetLocation(true) + " (" + EditorUtility.FormatBytes(assets[i].PackageSize) + ")";
                MainProgress = i + 1;

                await IndexPackage(assets[i], progressId);
                await Task.Yield(); // let editor breath
                if (CancellationRequested) break;

                // reread asset from DB in case of intermittent changes by online refresh 
                Asset asset = DBAdapter.DB.Find<Asset>(assets[i].Id);
                if (asset == null)
                {
                    Debug.LogWarning($"{assets[i]} disappeared while indexing.");
                    continue;
                }
                assets[i] = asset;

                AssetHeader header = ReadHeader(assets[i].GetLocation(true));
                ApplyHeader(header, assets[i]);

                assets[i].CurrentState = Asset.State.Done;
                Persist(assets[i]);
            }
            MetaProgress.Remove(progressId);
            ResetState(true);
        }

        private async Task IndexPackage(Asset asset, int progressId)
        {
            if (string.IsNullOrEmpty(asset.Location)) return;

            int subProgressId = MetaProgress.Start("Indexing package", null, progressId);
            string previewPath = AssetInventory.GetPreviewFolder();

            await RemovePersistentCacheEntry(asset);
            string tempPath = await AssetInventory.ExtractAsset(asset);
            if (string.IsNullOrEmpty(tempPath))
            {
                Debug.LogError($"{asset} could not be indexed due to issues extracting it: {asset.GetLocation(true)}");
                return;
            }
            string assetPreviewFile = Path.Combine(tempPath, ".icon.png");
            if (File.Exists(assetPreviewFile))
            {
                string targetDir = Path.Combine(previewPath, asset.Id.ToString());
                string targetFile = Path.Combine(targetDir, "a-" + asset.Id + Path.GetExtension(assetPreviewFile));
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                File.Copy(assetPreviewFile, targetFile, true);
                asset.PreviewImage = Path.GetFileName(targetFile);
            }

            string[] assets = Directory.GetFiles(tempPath, "pathname", SearchOption.AllDirectories);
            SubCount = assets.Length;
            for (int i = 0; i < assets.Length; i++)
            {
                string packagePath = assets[i];
                string dir = Path.GetDirectoryName(packagePath);
                string fileName = File.ReadLines(packagePath).FirstOrDefault();
                string metaFile = Path.Combine(dir, "asset.meta");
                string assetFile = Path.Combine(dir, "asset");
                string previewFile = Path.Combine(dir, "preview.png");

                if (IOUtils.PathContainsInvalidChars(fileName))
                {
                    Debug.LogError($"Skipping entry in '{packagePath}' since path contains invalid characters: {fileName}");
                    continue;
                }

                CurrentSub = fileName;
                SubProgress = i + 1;
                MetaProgress.Report(subProgressId, i + 1, assets.Length, fileName);

                // skip folders
                if (!File.Exists(assetFile)) continue;

                if (i % BREAK_INTERVAL == 0) await Task.Yield(); // let editor breath

                string guid = null;
                if (File.Exists(metaFile))
                {
                    guid = AssetUtils.ExtractGuidFromFile(metaFile);
                    if (guid == null) continue;
                }

                // remaining info from file data (creation date is not original date anymore, ignore)
                FileInfo assetInfo = new FileInfo(assetFile);
                long size = assetInfo.Length;

                AssetFile af = new AssetFile();
                af.Guid = guid;
                af.AssetId = asset.Id;
                af.Path = fileName;
                af.SourcePath = assetFile.Substring(tempPath.Length + 1);
                af.FileName = Path.GetFileName(af.Path);
                af.Size = size;
                af.Type = Path.GetExtension(fileName).Replace(".", string.Empty).ToLowerInvariant();
                if (AssetInventory.Config.gatherExtendedMetadata)
                {
                    await ProcessMediaAttributes(assetFile, af, asset); // must be run on main thread
                }
                Persist(af);

                // update preview 
                if (AssetInventory.Config.extractPreviews && File.Exists(previewFile))
                {
                    string targetFile = af.GetPreviewFile(previewPath);
                    string targetDir = Path.GetDirectoryName(targetFile);
                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                    File.Copy(previewFile, targetFile, true);
                    af.PreviewState = AssetFile.PreviewOptions.Supplied;
                    af.Hue = -1;
                    Persist(af);
                }
                if (CancellationRequested) break;
                await Cooldown.Do();
            }
            RemoveWorkFolder(asset, tempPath);

            CurrentSub = null;
            SubCount = 0;
            SubProgress = 0;
            MetaProgress.Remove(subProgressId);
        }

        public static AssetHeader ReadHeader(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (!File.Exists(path)) return null;

            AssetHeader result = null;
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                try
                {
                    byte[] header = new byte[17];
                    stream.Read(header, 0, 17);

                    // check if really a JSON object
                    if (header[16] == '{')
                    {
                        stream.Seek(14, SeekOrigin.Begin);
                        byte[] lengthData = new byte[2];
                        stream.Read(lengthData, 0, 2);
                        int length = BitConverter.ToInt16(lengthData, 0);

                        stream.Seek(16, SeekOrigin.Begin);
                        byte[] data = new byte[length];
                        stream.Read(data, 0, length);
                        string headerData = Encoding.ASCII.GetString(data);
                        result = JsonConvert.DeserializeObject<AssetHeader>(headerData);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Could not parse package {path}: {e.Message}");
                }
            }

            return result;
        }
    }
}