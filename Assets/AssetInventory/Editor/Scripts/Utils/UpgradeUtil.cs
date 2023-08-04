using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AssetInventory
{
    public static class UpgradeUtil
    {
        public static bool UpgradeRequired { get; private set; }

        public static void PerformUpgrades()
        {
            // filename was introduced in version 2
            AppProperty dbVersion = DBAdapter.DB.Find<AppProperty>("Version");
            int oldVersion;

            AppProperty requireUpgrade = DBAdapter.DB.Find<AppProperty>("UpgradeRequired");
            UpgradeRequired = requireUpgrade != null && requireUpgrade.Value.ToLowerInvariant() == "true";

            if (dbVersion == null)
            {
                // Upgrade from Initial to v2
                // add filenames to DB
                List<AssetFile> assetFiles = DBAdapter.DB.Table<AssetFile>().ToList();
                foreach (AssetFile assetFile in assetFiles)
                {
                    assetFile.FileName = Path.GetFileName(assetFile.Path);
                }
                DBAdapter.DB.UpdateAll(assetFiles);
                oldVersion = 5;
            }
            else
            {
                oldVersion = int.Parse(dbVersion.Value);
            }
            if (oldVersion < 5)
            {
                // force re-fetching of asset details to get state
                DBAdapter.DB.Execute("update Asset set ETag=null, LastOnlineRefresh=0");
                UpgradeRequired = true;

                // change how colors are indexed
                if (DBAdapter.ColumnExists("AssetFile", "DominantColor")) DBAdapter.DB.Execute("alter table AssetFile drop column DominantColor");
                if (DBAdapter.ColumnExists("AssetFile", "DominantColorGroup")) DBAdapter.DB.Execute("alter table AssetFile drop column DominantColorGroup");

                requireUpgrade = new AppProperty("UpgradeRequired", "true");
                DBAdapter.DB.InsertOrReplace(requireUpgrade);
            }
            if (oldVersion < 6)
            {
                DBAdapter.DB.Execute("update AssetFile set Hue=-1");
            }
            if (oldVersion < 7)
            {
                if (DBAdapter.ColumnExists("AssetFile", "PreviewFile"))
                {
                    DBAdapter.DB.Execute("alter table AssetFile drop column PreviewFile");
                    DBAdapter.DB.Execute("update AssetFile set PreviewState=99 where PreviewState=0");
                    DBAdapter.DB.Execute("update AssetFile set PreviewState=0 where PreviewState=1");
                    DBAdapter.DB.Execute("update AssetFile set PreviewState=1 where PreviewState=99");
                }
            }
            if (oldVersion < 8)
            {
                if (DBAdapter.ColumnExists("AssetFile", "PreviewImage"))
                {
                    DBAdapter.DB.Execute("alter table AssetFile drop column PreviewImage");
                }
            }
            if (oldVersion < 8 && !UpgradeRequired)
            {
                AppProperty newVersion = new AppProperty("Version", "8");
                DBAdapter.DB.InsertOrReplace(newVersion);
            }
        }

        private static void StartLongRunningUpgrade()
        {
            UpgradePreviewImageStructure();

            DBAdapter.DB.Delete<AppProperty>("UpgradeRequired");
            AppProperty newVersion = new AppProperty("Version", "8");
            DBAdapter.DB.InsertOrReplace(newVersion);

            UpgradeRequired = false;
        }

        private static async void UpgradePreviewImageStructure()
        {
            AssetInventory.CurrentMain = "Upgrading preview images structure...";

            string previewFolder = AssetInventory.GetPreviewFolder();
            IEnumerable<string> files = IOUtils.GetFiles(previewFolder, new[] {"*.png"});
            AssetInventory.MainCount = files.Count();
            AssetInventory.MainProgress = 0;

            int cleanedFiles = 0;
            foreach (string file in files)
            {
                AssetInventory.MainProgress++;
                AssetInventory.CurrentMainItem = file;
                if (AssetInventory.MainProgress % 1000 == 0) await Task.Yield();

                string[] arr = Path.GetFileNameWithoutExtension(file).Split('-');

                string assetId;
                switch (arr[0])
                {
                    case "a":
                        assetId = arr[1];
                        break;

                    case "af":
                        int fileId = int.Parse(arr[1]);
                        AssetFile af = DBAdapter.DB.Find<AssetFile>(fileId);
                        if (af == null)
                        {
                            // legacy, can be removed
                            cleanedFiles++;
                            File.Delete(file);
                            continue;
                        }
                        assetId = af.AssetId.ToString();
                        break;

                    default:
                        Debug.LogError($"Unknown preview type: {file}");
                        continue;
                }

                // move file from root into new sub-structure
                string targetDir = Path.Combine(previewFolder, assetId);
                string targetFile = Path.Combine(targetDir, Path.GetFileName(file));
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                if (File.Exists(targetFile)) File.Delete(targetFile);
                File.Move(file, targetFile);
            }
            Debug.Log($"Cleaned up orphaned preview files: {cleanedFiles}");

            AssetInventory.CurrentMain = null;
        }

        public static void DrawUpgradeRequired()
        {
            EditorGUILayout.Space(30);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Box(BasicEditorUI.Logo, EditorStyles.centeredGreyMiniLabel, GUILayout.MaxWidth(300), GUILayout.MaxHeight(300));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("A longer running upgrade is required for this version.", UIStyles.whiteCenter);

            EditorGUILayout.Space(30);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(!string.IsNullOrEmpty(AssetInventory.CurrentMain));
            if (GUILayout.Button("Start Upgrade Process", GUILayout.Height(50))) StartLongRunningUpgrade();
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(AssetInventory.CurrentMain))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(AssetInventory.CurrentMain, UIStyles.whiteCenter);
                EditorGUILayout.Space();
                UIStyles.DrawProgressBar(AssetInventory.MainProgress / (float) AssetInventory.MainCount, AssetInventory.CurrentMainItem);
            }
        }
    }
}