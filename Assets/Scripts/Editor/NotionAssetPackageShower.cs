using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.Networking;

public class NotionAssetPackageShower : EditorWindow
{
    private Vector2 scrollPosition;
    private List<AssetInfo> assets;

    private async Task OnEnable()
    {
        assets = await ReadAssetDataFromCSVAsync(); // Function to read asset data from CSV
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (AssetInfo asset in assets)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name:", GUILayout.Width(50));
            EditorGUILayout.TextField(asset.name);
            EditorGUILayout.LabelField("Asset Type:", GUILayout.Width(70));
            EditorGUILayout.TextField(asset.assetType);
            if (GUILayout.Button("Download"))
            {
                DownloadUnityPackage(asset.downloadLink);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private async void DownloadUnityPackage(string downloadLink)
    {
        Debug.Log("Downloading UnityPackage from: " + downloadLink);

        using (UnityWebRequest www = UnityWebRequest.Get(downloadLink))
        {
            TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

            www.SendWebRequest().completed += (operation) =>
            {
                if (www.result == UnityWebRequest.Result.Success)
                {
                    string tempFilePath = Path.Combine(Application.temporaryCachePath, "temp.unitypackage");
                    File.WriteAllBytes(tempFilePath, www.downloadHandler.data);

                    AssetDatabase.ImportPackage(tempFilePath, true);

                    Debug.Log("UnityPackage downloaded and imported successfully!");
                    taskCompletionSource.SetResult(true);
                }
                else
                {
                    Debug.LogError("Failed to download UnityPackage. Error: " + www.error);
                    if (www.downloadHandler != null)
                    {
                        Debug.LogError("Download Handler error: " + www.downloadHandler.error);
                    }

                    taskCompletionSource.SetResult(false);
                }

                www.Dispose();
            };

            bool success = await taskCompletionSource.Task;

            if (!success)
            {
                // Handle the failure case
            }
        }
    }


    // Function to read asset data from CSV
    private async Task<List<AssetInfo>> ReadAssetDataFromCSVAsync()
    {
        List<AssetInfo> assets = new List<AssetInfo>();

        try
        {
            string csvFilePath =
                AssetDatabase.GetAssetPath(
                    Resources.Load("Assets_Notion")); // Replace with the actual path to your CSV file

            using (StreamReader reader = new StreamReader(csvFilePath))
            {
                // Skip the header line if it exists
                await reader.ReadLineAsync();

                while (!reader.EndOfStream)
                {
                    string line = await reader.ReadLineAsync();
                    string[] csvLineValues = line.Split(',');

                    if (csvLineValues.Length >= 5)
                    {
                        AssetInfo asset = new AssetInfo();
                        asset.name = csvLineValues[0];
                        asset.assetType = csvLineValues[1];
                        asset.fileType = csvLineValues[2];
                        asset.group = csvLineValues[3];
                        asset.downloadLink = csvLineValues[4];

                        assets.Add(asset);
                    }
                    else
                    {
                        Debug.LogWarning("Invalid data format in CSV line: " + line);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to read asset data from CSV: " + ex.Message);
        }

        return assets;
    }

    // Add menu item to open the toolbar window
    [MenuItem("MaxedOutEntertainment/Notion Asset Package Shower")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(NotionAssetPackageShower));
    }
}

[System.Serializable]
public class AssetInfo
{
    public string name;
    public string assetType;
    public string fileType;
    public string group;
    public string downloadLink;
}