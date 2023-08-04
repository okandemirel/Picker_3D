using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace AssetInventory
{
    public sealed class PreviewImporter : AssetImporter
    {
        private const int MAX_REQUESTS = 2;
        private const int OPEN_REQUESTS = 0;

        public async Task<int> RecreatePreviews(Asset asset)
        {
            int created = 0;

            ResetState(false);
            int progressId = MetaProgress.Start("Recreating previews");

            string query = "select *, AssetFile.Id as Id from AssetFile inner join Asset on Asset.Id = AssetFile.AssetId where Asset.Exclude = false and AssetFile.PreviewState=? " + (asset != null ? " and Asset.Id=" + asset.Id : "") + " order by Asset.Id";
            List<AssetInfo> files = DBAdapter.DB.Query<AssetInfo>(query, AssetFile.PreviewOptions.Redo).ToList();

            SubCount = files.Count;

            PreviewGenerator.Init(files.Count);
            string previewPath = AssetInventory.GetPreviewFolder();
            string tempPath = null;
            foreach (AssetInfo af in files)
            {
                SubProgress++;
                CurrentSub = $"Creating preview for {af.FileName}";
                MetaProgress.Report(progressId, SubProgress, SubCount, string.Empty);
                if (CancellationRequested) break;
                await Cooldown.Do();
                if (SubProgress % 5000 == 0) await Task.Yield(); // let editor breath

                // do not recreate for files with dependencies as that will throw lots of errors, need a full import for that
                if (!PreviewGenerator.IsPreviewable(af.FileName, true))
                {
                    DBAdapter.DB.Execute("update AssetFile set PreviewState=? where Id=?", AssetFile.PreviewOptions.None, af.Id);
                    continue;
                }

                string previewFile = af.GetPreviewFile(previewPath);
                string sourcePath = await AssetInventory.EnsureMaterializedAsset(af);
                if (sourcePath == null)
                {
                    DBAdapter.DB.Execute("update AssetFile set PreviewState=? where Id=?", AssetFile.PreviewOptions.Error, af.Id);
                    continue;
                }

                // clean up old materialization
                if (AssetInventory.GetMaterializedAssetPath(af.ToAsset()) != tempPath)
                {
                    if (tempPath != null)
                    {
                        string toDelete = tempPath;
                        _ = Task.Run(() => Directory.Delete(toDelete, true));
                    }
                    tempPath = AssetInventory.GetMaterializedAssetPath(af.ToAsset());
                }

                await Task.Yield(); // let editor breath

                if (AssetInventory.ScanDependencies.Contains(af.Type))
                {
                    if (af.DependencyState == AssetInfo.DependencyStateOptions.Unknown) await AssetInventory.CalculateDependencies(af);
                    if (af.Dependencies.Count > 0) sourcePath = await AssetInventory.CopyTo(af, PreviewGenerator.GetPreviewWorkFolder(), true);
                }

                PreviewGenerator.RegisterPreviewRequest(af.Id, sourcePath, previewFile, req =>
                {
                    StorePreviewResult(req);
                    DBAdapter.DB.Execute("update AssetFile set PreviewState=? where Id=?", req.Icon != null ? AssetFile.PreviewOptions.Custom : AssetFile.PreviewOptions.Error, af.Id);
                    if (req.Icon != null) created++;
                }, af.Dependencies?.Count > 0);

                PreviewGenerator.EnsureProgress();
                if (PreviewGenerator.ActiveRequestCount() > MAX_REQUESTS) await PreviewGenerator.ExportPreviews(OPEN_REQUESTS);
            }
            await PreviewGenerator.ExportPreviews();
            PreviewGenerator.Clear();

            // remove files again, no need to wait
            if (tempPath != null) _ = Task.Run(() => Directory.Delete(tempPath, true));

            MetaProgress.Remove(progressId);
            ResetState(true);

            return created;
        }

        public static void ScheduleRecreatePreviews(Asset asset, bool missingOnly, bool retryErroneous)
        {
            List<string> wheres = new List<string>();
            List<object> args = new List<object>();
            args.Add(AssetFile.PreviewOptions.Redo);

            if (retryErroneous)
            {
                if (asset != null)
                {
                    DBAdapter.DB.Execute("update AssetFile set PreviewState = ? where PreviewState = ? and AssetId = ?", AssetFile.PreviewOptions.None, AssetFile.PreviewOptions.Error, asset.Id);
                }
                else
                {
                    DBAdapter.DB.Execute("update AssetFile set PreviewState = ? where PreviewState = ?", AssetFile.PreviewOptions.None, AssetFile.PreviewOptions.Error);
                }
            }

            // sqlite does not support binding lists, parameters must be spelled out
            List<string> paramCount = new List<string>();
            foreach (string t in AssetInventory.TypeGroups["Audio"].Union(AssetInventory.TypeGroups["Images"]).Union(AssetInventory.TypeGroups["Models"]).Union(AssetInventory.TypeGroups["Prefabs"]).Union(AssetInventory.TypeGroups["Materials"]))
            {
                paramCount.Add("?");
                args.Add(t);
            }
            wheres.Add("AssetFile.Type in (" + string.Join(",", paramCount) + ")");

            if (missingOnly)
            {
                wheres.Add("PreviewState = ?");
                args.Add(AssetFile.PreviewOptions.None);

                if (asset != null)
                {
                    wheres.Add("AssetId=?");
                    args.Add(asset.Id);
                }
            }
            else
            {
                if (asset == null)
                {
                    // base query only
                    Debug.LogError("This is not supported yet as it would require reparsing unity packages as well.");
                    return;
                }
                wheres.Add("AssetId=?");
                args.Add(asset.Id);
            }

            string where = wheres.Count > 0 ? "where " + string.Join(" and ", wheres) : "";
            DBAdapter.DB.Execute($"update AssetFile set PreviewState=? {where}", args.ToArray());
        }

        public static async Task<bool> RecreatePreview(AssetInfo info)
        {
            bool result = false;
            string sourcePath = await AssetInventory.EnsureMaterializedAsset(info);
            string previewFile = info.GetPreviewFile(AssetInventory.GetPreviewFolder());

            if (AssetInventory.ScanDependencies.Contains(info.Type))
            {
                if (info.DependencyState == AssetInfo.DependencyStateOptions.Unknown) await AssetInventory.CalculateDependencies(info);
                if (info.Dependencies.Count > 0) sourcePath = await AssetInventory.CopyTo(info, PreviewGenerator.GetPreviewWorkFolder(), true);
            }

            PreviewGenerator.RegisterPreviewRequest(info.Id, sourcePath, previewFile, req =>
            {
                result = req.Icon != null;
                StorePreviewResult(req);
            }, info.Dependencies?.Count > 0);
            PreviewGenerator.EnsureProgress();
            await PreviewGenerator.ExportPreviews();
            PreviewGenerator.Clear();

            return result;
        }

        public static void StorePreviewResult(PreviewRequest req)
        {
            if (!File.Exists(req.DestinationFile)) return;
            AssetFile paf = DBAdapter.DB.Find<AssetFile>(req.Id);
            if (paf == null) return;

            if (req.Obj != null)
            {
                if (req.Obj is Texture2D tex)
                {
                    paf.Width = tex.width;
                    paf.Height = tex.height;
                }
                if (req.Obj is AudioClip clip)
                {
                    paf.Length = clip.length;
                }
            }

            paf.PreviewState = AssetFile.PreviewOptions.Custom;
            paf.Hue = -1;

            DBAdapter.DB.Update(paf);
        }
    }
}