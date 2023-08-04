using System.Collections;
using System.Collections.Generic;
using System.IO;
using SQLite;
using UnityEngine;

namespace AssetInventory
{
    public sealed class ColorImporter : AssetImporter
    {
        public IEnumerator Index()
        {
            ResetState(false);
            int progressId = MetaProgress.Start("Extracting color information");

            string previewFolder = AssetInventory.GetPreviewFolder();

            TableQuery<AssetFile> query = DBAdapter.DB.Table<AssetFile>()
                .Where(a => (a.PreviewState == AssetFile.PreviewOptions.Custom || a.PreviewState == AssetFile.PreviewOptions.Supplied) && a.Hue < 0);

            // skip audio files per default
            if (!AssetInventory.Config.extractAudioColors)
            {
                foreach (string t in AssetInventory.TypeGroups["Audio"])
                {
                    query = query.Where(a => a.Type != t);
                }
            }

            List<AssetFile> files = query.ToList();

            SubCount = files.Count;
            for (int i = 0; i < files.Count; i++)
            {
                if (CancellationRequested) break;
                yield return Cooldown.DoCo();

                AssetFile file = files[i];
                MetaProgress.Report(progressId, i + 1, files.Count, file.FileName);
                CurrentSub = $"Color extraction from {file.FileName}";
                SubProgress = i + 1;

                string previewFile = file.GetPreviewFile(previewFolder);
                if (!File.Exists(previewFile))
                {
                    Debug.LogWarning($"Preview file for {file} does not exist anymore. Marking it missing for recreation.");
                    DBAdapter.DB.Execute("update AssetFile set PreviewState=? where Id=?", AssetFile.PreviewOptions.Redo, file.Id);
                    file.PreviewState = AssetFile.PreviewOptions.None;
                    continue;
                }

                yield return AssetUtils.LoadTexture(previewFile, result =>
                {
                    if (result == null) return;

                    file.Hue = ImageUtils.GetHue(result);
                    Persist(file);
                });
            }
            MetaProgress.Remove(progressId);
            ResetState(true);
        }
    }
}