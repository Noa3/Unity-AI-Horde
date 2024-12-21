using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ExampleScript : MonoBehaviour
{
    public string apiKey = "0000000000";
    public Renderer targetRenderer;
    public Text statusText;

    private void Start()
    {
        // Set the API key
        HordeImageGenerator.HordeBase.apiKey = apiKey;
    }

    public async void SetApiKey(string newApiKey)
    {
        HordeImageGenerator.HordeBase.apiKey = newApiKey;
        statusText.text = "API key set.";
    }

    public async void ValidateApiKey()
    {
        await HordeImageGenerator.User.ValidateApiKey();
    }

    public async void GetCurrentStylesAndModels()
    {
        string imageStyles = await HordeImageGenerator.ImageGenerator.GetCurrentImageStyles();
        string textStyles = await HordeImageGenerator.TextGenerator.GetCurrentTextStyles();
        string models = await HordeImageGenerator.Status.GetCurrentModels();

        statusText.text = $"Image Styles: {imageStyles}\nText Styles: {textStyles}\nModels: {models}";
    }

    public async void GenerateImage()
    {
        string imageResponse = await HordeImageGenerator.ImageGenerator.GenerateSimpleImageAsync("A magical skyline", true);
        if (imageResponse != null)
        {
            targetRenderer.material.mainTexture = HordeImageGenerator.ImageGenerator.lastGeneratedImage;
            statusText.text = "Image generated successfully.";
        }
        else
        {
            statusText.text = "Failed to generate image.";
        }
    }

    private async void OnApplicationQuit()
    {
        await HordeImageGenerator.ImageGenerator.CancelAllImageRequests();
        await HordeImageGenerator.TextGenerator.CancelAllTextRequests();
    }
}


