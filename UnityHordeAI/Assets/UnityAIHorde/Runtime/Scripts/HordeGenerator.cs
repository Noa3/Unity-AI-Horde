using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace HordeImageGenerator
{
    public static class HordeBase
    {
        public static string apiKey = "0000000000"; // Your API key
        public static string baseApiUrl = "https://stablehorde.net/api/v2/";
        public static string currentModels;
    }

    [System.Serializable]
    public class ImageGenerationParameters
    {
        public string prompt;
        public GenerationParams @params = new GenerationParams();
        public bool nsfw = false;
        public bool trusted_workers = false;
        public bool validated_backends = true;
        public bool slow_workers = true;
        public bool extra_slow_workers = false;
        public bool censor_nsfw = false;
        public List<string> workers = new List<string>();
        public bool worker_blacklist = false;
        public List<string> models = new List<string>();
        public string source_image = "";
        public string source_processing = "";
        public string source_mask = "";
        public List<ExtraSourceImage> extra_source_images = new List<ExtraSourceImage>();
        public bool r2 = true;
        public bool shared = false;
        public bool replacement_filter = true;
        public bool dry_run = false;
        public string proxied_account = "";
        public bool disable_batching = false;
        public bool allow_downgrade = false;
        public string webhook = "";
        public string style = "";
    }

    [System.Serializable]
    public class GenerationParams
    {
        public string sampler_name = "k_heun";
        public float cfg_scale = 7.5f;
        public float denoising_strength = 0.75f;
        public float hires_fix_denoising_strength = 0.75f;
        public int height = 512;
        public int width = 512;
        public List<string> post_processing = new List<string> { "GFPGAN" };
        public bool karras = false;
        public bool tiling = false;
        public bool hires_fix = false;
        public int clip_skip = 1;
        public float facefixer_strength = 0.75f;
        public List<Lora> loras = new List<Lora>();
        public List<Ti> tis = new List<Ti>();
        public Dictionary<string, object> special = new Dictionary<string, object>();
        public string workflow = "qr_code";
        public bool transparent = false;
        public string seed = "The little seed that could";
        public int seed_variation = 1;
        public string control_type = "canny";
        public bool image_is_control = false;
        public bool return_control_map = false;
        public List<ExtraText> extra_texts = new List<ExtraText>();
        public int steps = 30;
        public int n = 1;
    }

    [System.Serializable]
    public class Lora
    {
        public string name;
        public int model;
        public int clip;
        public string inject_trigger;
        public bool is_version;
    }

    [System.Serializable]
    public class Ti
    {
        public string name;
        public string inject_ti;
        public float strength;
    }

    [System.Serializable]
    public class ExtraText
    {
        public string text;
        public string reference;
    }

    [System.Serializable]
    public class ExtraSourceImage
    {
        public string image;
        public float strength;
    }

    public static class ImageGenerator
    {
        public static Texture2D lastGeneratedImage;
        public static List<string> currentImageRequests = new List<string>();
        public static string currentImageStyles;

        // Method to generate image asynchronously with full parameters
        public static async Task<string> GenerateImageAsync(ImageGenerationParameters parameters, bool saveImage = false)
        {
            string url = $"{HordeBase.baseApiUrl}generate/async";
            string jsonPayload = JsonUtility.ToJson(parameters);

            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("apikey", HordeBase.apiKey);

                await webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError(webRequest.error);
                    return null;
                }
                else if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = webRequest.downloadHandler.text;
                    // Assuming the response contains a URL to the generated image
                    string imageUrl = ExtractImageUrlFromResponse(jsonResponse);
                    await LoadImageToRenderer(imageUrl, saveImage);
                    return jsonResponse;
                }
                else
                {
                    Debug.LogError($"Unexpected response: {webRequest.downloadHandler.text}");
                    return null;
                }
            }
        }

        public static async Task<string> GenerateSimpleImageAsync(string prompt, bool saveImage = false)
        {
            string url = $"{HordeBase.baseApiUrl}generate/async";
            WWWForm form = new WWWForm();
            form.AddField("prompt", prompt);
            //form.AddField("height", 256);
            //form.AddField("width", 256);

            using (UnityWebRequest webRequest = UnityWebRequest.Post(url, form))
            {
                webRequest.SetRequestHeader("apikey", HordeBase.apiKey);
                await webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError(webRequest.error);
                    return null;
                }
                else if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = webRequest.downloadHandler.text;
                    // Assuming the response contains a URL to the generated image
                    string imageUrl = ExtractImageUrlFromResponse(jsonResponse);
                    await LoadImageToRenderer(imageUrl, saveImage);
                    return jsonResponse;
                }
                else
                {
                    Debug.LogError($"Unexpected response: {webRequest.downloadHandler.text}");
                    return null;
                }
            }
        }

        // Overloaded method to generate image with basic parameters
        public static async Task<string> GenerateImageAsync(string prompt, bool saveImage = false)
        {
            var parameters = new ImageGenerationParameters { prompt = prompt };
            return await GenerateImageAsync(parameters, saveImage);
        }

        // Overloaded method to generate image with prompt and dimensions
        public static async Task<string> GenerateImageAsync(string prompt, int height, int width, bool saveImage = false)
        {
            var parameters = new ImageGenerationParameters { prompt = prompt };
            parameters.@params.height = height;
            parameters.@params.width = width;
            return await GenerateImageAsync(parameters, saveImage);
        }

        private static async Task LoadImageToRenderer(string imageUrl, bool saveImage)
        {
            using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(imageUrl))
            {
                await webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError(webRequest.error);
                }
                else
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
                    if (saveImage)
                    {
                        lastGeneratedImage = texture;
                    }
                }
            }
        }

        private static string ExtractImageUrlFromResponse(string jsonResponse)
        {
            // Implement JSON parsing to extract the image URL from the response
            // This is a placeholder implementation
            return "https://example.com/generated_image.png";
        }

        public static void SaveLastGeneratedImage(string filePath)
        {
            if (lastGeneratedImage != null)
            {
                byte[] bytes = lastGeneratedImage.EncodeToPNG();
                File.WriteAllBytes(filePath, bytes);
                Debug.Log($"Image saved to {filePath}");
            }
            else
            {
                Debug.LogError("No image to save.");
            }
        }

        // Method to cancel an unfinished image request
        public static async Task<string> CancelImageGeneration(string id)
        {
            string url = $"{HordeBase.baseApiUrl}generate/status/{id}";
            using (UnityWebRequest webRequest = UnityWebRequest.Delete(url))
            {
                webRequest.SetRequestHeader("apikey", HordeBase.apiKey);
                await webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError(webRequest.error);
                    return null;
                }
                else
                {
                    currentImageRequests.Remove(id);
                    return webRequest.downloadHandler.text;
                }
            }
        }

        // Method to get all current image styles
        public static async Task<string> GetCurrentImageStyles()
        {
            string url = $"{HordeBase.baseApiUrl}styles/image";
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.SetRequestHeader("apikey", HordeBase.apiKey);
                await webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError(webRequest.error);
                    return null;
                }
                else
                {
                    currentImageStyles = webRequest.downloadHandler.text;
                    return currentImageStyles;
                }
            }
        }

        // Method to cancel all image requests
        public static async Task CancelAllImageRequests()
        {
            foreach (var requestId in currentImageRequests)
            {
                await CancelImageGeneration(requestId);
            }
        }
    }

    public static class TextGenerator
    {
        public static string lastGeneratedText;
        public static List<string> currentTextRequests = new List<string>();
        public static string currentTextStyles;

        // Method to generate text asynchronously
        public static async Task<string> GenerateTextAsync(string prompt)
        {
            string url = $"{HordeBase.baseApiUrl}generate/text/async";
            WWWForm form = new WWWForm();
            form.AddField("prompt", prompt);

            using (UnityWebRequest webRequest = UnityWebRequest.Post(url, form))
            {
                webRequest.SetRequestHeader("apikey", HordeBase.apiKey);
                await webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError(webRequest.error);
                    return null;
                }
                else
                {
                    lastGeneratedText = webRequest.downloadHandler.text;
                    return lastGeneratedText;
                }
            }
        }

        // Method to cancel an unfinished text request
        public static async Task<string> CancelTextGeneration(string id)
        {
            string url = $"{HordeBase.baseApiUrl}generate/text/status/{id}";
            using (UnityWebRequest webRequest = UnityWebRequest.Delete(url))
            {
                webRequest.SetRequestHeader("apikey", HordeBase.apiKey);
                await webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError(webRequest.error);
                    return null;
                }
                else
                {
                    currentTextRequests.Remove(id);
                    return webRequest.downloadHandler.text;
                }
            }
        }

        // Method to get all current text styles
        public static async Task<string> GetCurrentTextStyles()
        {
            string url = $"{HordeBase.baseApiUrl}styles/text";
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.SetRequestHeader("apikey", HordeBase.apiKey);
                await webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError(webRequest.error);
                    return null;
                }
                else
                {
                    currentTextStyles = webRequest.downloadHandler.text;
                    return currentTextStyles;
                }
            }
        }

        // Method to cancel all text requests
        public static async Task CancelAllTextRequests()
        {
            foreach (var requestId in currentTextRequests)
            {
                await CancelTextGeneration(requestId);
            }
        }
    }

    public static class User
    {
        // Method to find user
        public static async Task<string> FindUser()
        {
            string url = $"{HordeBase.baseApiUrl}find_user";
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.SetRequestHeader("apikey", HordeBase.apiKey);
                Debug.Log("Sending request to find user...");
                await webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error finding user: {webRequest.error}");
                    return null;
                }
                else
                {
                    Debug.Log("User found successfully.");
                    return webRequest.downloadHandler.text;
                }
            }
        }

        // Method to get kudos
        public static async Task<string> GetKudos()
        {
            string url = $"{HordeBase.baseApiUrl}kudos";
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.SetRequestHeader("apikey", HordeBase.apiKey);
                await webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError(webRequest.error);
                    return null;
                }
                else
                {
                    return webRequest.downloadHandler.text;
                }
            }
        }

        // Method to validate API key
        public static async Task<string> ValidateApiKey()
        {
            Debug.Log("Starting API key validation...");
            string userInfo = await FindUser();
            if (userInfo != null)
            {
                Debug.Log($"API key is valid. User info: {userInfo}");
                return userInfo;
            }
            else
            {
                Debug.LogError("API key is invalid or there was an error with the request.");
                return null;
            }
        }
    }

    public static class Status
    {
        // Method to get status
        public static async Task<string> GetStatus()
        {
            string url = $"{HordeBase.baseApiUrl}status";
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.SetRequestHeader("apikey", HordeBase.apiKey);
                await webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError(webRequest.error);
                    return null;
                }
                else
                {
                    return webRequest.downloadHandler.text;
                }
            }
        }

        // Method to get models
        public static async Task<string> GetModels()
        {
            string url = $"{HordeBase.baseApiUrl}models";
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.SetRequestHeader("apikey", HordeBase.apiKey);
                await webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError(webRequest.error);
                    return null;
                }
                else
                {
                    return webRequest.downloadHandler.text;
                }
            }
        }

        // Method to get all current models
        public static async Task<string> GetCurrentModels()
        {
            string url = $"{HordeBase.baseApiUrl}status/models";
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.SetRequestHeader("apikey", HordeBase.apiKey);
                await webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError(webRequest.error);
                    return null;
                }
                else
                {
                    HordeBase.currentModels = webRequest.downloadHandler.text;
                    return HordeBase.currentModels;
                }
            }
        }
    }

    public static class Collections
    {
        // Method to get collections
        public static async Task<string> GetCollections()
        {
            string url = $"{HordeBase.baseApiUrl}collections";
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.SetRequestHeader("apikey", HordeBase.apiKey);
                await webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError(webRequest.error);
                    return null;
                }
                else
                {
                    return webRequest.downloadHandler.text;
                }
            }
        }

        // Method to create a collection
        public static async Task<string> CreateCollection(string collectionName)
        {
            string url = $"{HordeBase.baseApiUrl}collections";
            WWWForm form = new WWWForm();
            form.AddField("name", collectionName);

            using (UnityWebRequest webRequest = UnityWebRequest.Post(url, form))
            {
                webRequest.SetRequestHeader("apikey", HordeBase.apiKey);
                await webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError(webRequest.error);
                    return null;
                }
                else
                {
                    return webRequest.downloadHandler.text;
                }
            }
        }

        // Method to delete a collection
        public static async Task<string> DeleteCollection(string collectionId)
        {
            string url = $"{HordeBase.baseApiUrl}collections/{collectionId}";
            using (UnityWebRequest webRequest = UnityWebRequest.Delete(url))
            {
                webRequest.SetRequestHeader("apikey", HordeBase.apiKey);
                await webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError(webRequest.error);
                    return null;
                }
                else
                {
                    return webRequest.downloadHandler.text;
                }
            }
        }
    }
}

