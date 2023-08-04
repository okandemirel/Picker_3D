// For more details, see the official documentation: https://platform.openai.com/docs/guides/images/introduction
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenAi.Api.V1;
using OpenAi.Unity.V1;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace OpenAI.DemoScript{
public class OpenAiImageGenerator : MonoBehaviour
{
    public Image output;
    public OpenAiCompleterV1 completer;

    public String promptInstructions = "Generate a panoramic skybox for a video game with realistic sky colors. The skybox should have a natural appearance and be suitable for use in a video game environment. Here's how it should look: "; // Use this to set a consistent style in the images generated. In this example, we want only skyboxes to be generated regardless of what the user types in.

    [SerializeField] private TMP_InputField input;
    // Start is called before the first frame update
    void Start()
    {
    }

    public void GenerateImageFromInput()
    {
        if (input.IsUnityNull())
        {
            Debug.LogError("No input set for Image Generation");
            return;
        }

        GenerateImage(input.text);
    }

    public async void GenerateImage(string imageDescription)
{
    var transcript = await GenerateImageRequest(imageDescription);
    if (transcript.IsSuccess)
    {
        var base64Image = transcript.Result.data[0].b64_json;
        byte[] imageBytes = Convert.FromBase64String(base64Image);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(imageBytes);
        Debug.Log($"Image texture loaded with size {tex.width}x{tex.height}");
        Sprite sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f),
            100.0f);
        output.sprite = sprite;

        // Set the skybox material with the generated image

        Material skyboxMaterial = new Material(Shader.Find("Skybox/Panoramic"));
        skyboxMaterial.SetTexture("_MainTex", tex);
        RenderSettings.skybox = skyboxMaterial;
        skyboxMaterial.SetTexture("_MainTex", tex);
        Debug.Log($"Skybox material set to {skyboxMaterial.name}");
        RenderSettings.skybox = skyboxMaterial;
    }
    else
    {
        Debug.LogError(
            $"ERROR: StatusCode: {transcript.HttpResponse.responseCode} - {transcript.HttpResponse.error}");
    }
}

    public async Task<ApiResult<GenerationsV1>> GenerateImageRequest(string imageDescription)
    {
        SOAuthArgsV1 auth = completer.Auth;
        OpenAiApiV1 api = new OpenAiApiV1(auth.ResolveAuth());


        ApiResult<GenerationsV1> comp = await api.Images.Generations.SendRequestAsync(
            new GenerationsRequestV1()
            {
                prompt = promptInstructions + imageDescription, // The more detailed the description, the more likely you are to get the result that you or your end user want.
                response_format = "b64_json", // Each image can be returned as either a URL or Base64 data, using the response_format parameter. URLs will expire after an hour.
                n = 1, // You can request 1-10 images at a time using the n parameter.
                size = "1024x1024", // 256x256, 512x512, or 1024x1024 pixels. Smaller sizes are faster to generate.
                user = "test-user"
            });

        return comp;
    }
}
}