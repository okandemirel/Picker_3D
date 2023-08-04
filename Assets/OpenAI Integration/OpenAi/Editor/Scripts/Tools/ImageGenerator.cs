using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using OpenAi.Api.V1;
using OpenAi.Unity.V1;

namespace OpenAi.Examples{
public class ImageGenerator : EditorWindow
{
    private OpenAiCompleterV1 completer;
    private string imageDescription = "white cobblestone seamlessly tileable texture for use in a video game";
    private string[] sizes = { "256x256", "512x512", "1024x1024" };
    private int selectedSizeIndex = 2;
    private int numberOfImages = 1;
    private List<Texture2D> generatedTextures = new List<Texture2D>();
    private bool isGenerating = false;

    [MenuItem("Tools/OpenAi/Image Generator")]
    public static void ShowWindow()
    {
        GetWindow<ImageGenerator>("Dall-E 2 Image Generator");
    }

    private void OnGUI()
    {
        DisplayGeneratedImages();

        GUILayout.Label("Describe the image you want to generate in specific detail", EditorStyles.boldLabel);
        imageDescription = EditorGUILayout.TextArea(imageDescription, GUILayout.Height(75));

        GUILayout.Label("Image Size", EditorStyles.boldLabel);
        selectedSizeIndex = EditorGUILayout.Popup(selectedSizeIndex, sizes);

        GUILayout.Label("Number of Images (1-10)", EditorStyles.boldLabel);
        numberOfImages = EditorGUILayout.IntSlider(numberOfImages, 1, 10);

        if (isGenerating)
        {
            GUILayout.Label("Please wait...", EditorStyles.boldLabel);
        }

        if (GUILayout.Button("Generate"))
        {
            GenerateImages();
        }
    }

    private async void GenerateImages()
    {
        isGenerating = true;
        generatedTextures.Clear();
        Repaint();

        var result = await GenerateImageRequest(imageDescription, sizes[selectedSizeIndex], numberOfImages);

        if (result.IsSuccess)
        {
            foreach (var data in result.Result.data)
            {
                byte[] imageBytes = Convert.FromBase64String(data.b64_json);
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(imageBytes);
                generatedTextures.Add(tex);
            }
        }
        else
        {
            Debug.LogError($"ERROR: StatusCode: {result.HttpResponse.responseCode} - {result.HttpResponse.error}");
        }

        isGenerating = false;
        Repaint();
    }

    private void DisplayGeneratedImages()
    {
        if (generatedTextures.Count > 0)
        {
            GUILayout.Label("Generated Images", EditorStyles.boldLabel);
            for (int i = 0; i < generatedTextures.Count; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(generatedTextures[i]), GUILayout.Width(64), GUILayout.Height(64));

                if (GUILayout.Button("View", GUILayout.Width(50)))
                {
                    PreviewImage(generatedTextures[i]);
                }

                if (GUILayout.Button("Save", GUILayout.Width(50)))
                {
                    SaveImage(generatedTextures[i]);
                }

                GUILayout.EndHorizontal();
            }
        }
    }

    private void PreviewImage(Texture2D texture)
    {
        var previewWindow = EditorWindow.GetWindow<PreviewImageWindow>();
        previewWindow.SetTexture(texture);
        previewWindow.minSize = new Vector2(texture.width, texture.height);
        previewWindow.maxSize = new Vector2(texture.width, texture.height);
        previewWindow.Show();
    }

    private void SaveImage(Texture2D texture)
    {
        string path = EditorUtility.SaveFilePanel("Save Image", "", "generated_image", "png");

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
    }

    private async Task<ApiResult<GenerationsV1>> GenerateImageRequest(string imageDescription, string size, int n)
    {
        SOAuthArgsV1 auth = null;

            string assetName = "DefaultAuthArgsV1";
            string[] guids = AssetDatabase.FindAssets($"t:SOAuthArgsV1 {assetName}");
            if (guids.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                auth = AssetDatabase.LoadAssetAtPath<SOAuthArgsV1>(assetPath);
            }
            else
            {
                Debug.LogError($"Could not find asset with name '{assetName}' and type 'SOAuthArgsV1'");
            }
            OpenAiApiV1 api = new OpenAiApiV1(auth.ResolveAuth());

        ApiResult<GenerationsV1> comp = await api.Images.Generations.SendRequestAsync(
            new GenerationsRequestV1()
            {
                prompt = imageDescription,
                response_format = "b64_json",
                n = n,
                size = size,
                user = "test-user"
            });

        return comp;
    }
}

public class PreviewImageWindow : EditorWindow
{
    private Texture2D texture;

    public void SetTexture(Texture2D texture)
    {
        this.texture = texture;
    }

    private void OnGUI()
    {
        if (texture != null)
        {
            GUILayout.Label(new GUIContent(texture), GUILayout.Width(texture.width), GUILayout.Height(texture.height));
        }
    }
}
}