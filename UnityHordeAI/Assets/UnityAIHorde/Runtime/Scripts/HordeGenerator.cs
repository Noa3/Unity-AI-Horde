using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;

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
        //public string style = "";
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

    [System.Serializable]
    public class TextGenerationResponse
    {
        public string id;
        public float kudos;
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

            // Manuelles Erstellen der JSON-Nutzlast
            //string jsonPayload = $@"
            //{{
            //    ""prompt"": ""{parameters.prompt}"",
            //    ""params"": {{
            //        ""sampler_name"": ""{parameters.@params.sampler_name}"",
            //        ""cfg_scale"": {parameters.@params.cfg_scale},
            //        ""denoising_strength"": {parameters.@params.denoising_strength},
            //        ""hires_fix_denoising_strength"": {parameters.@params.hires_fix_denoising_strength},
            //        ""height"": {parameters.@params.height},
            //        ""width"": {parameters.@params.width},
            //        ""post_processing"": [{string.Join(",", parameters.@params.post_processing.Select(p => $"\"{p}\""))}],
            //        ""karras"": {parameters.@params.karras.ToString().ToLower()},
            //        ""tiling"": {parameters.@params.tiling.ToString().ToLower()},
            //        ""hires_fix"": {parameters.@params.hires_fix.ToString().ToLower()},
            //        ""clip_skip"": {parameters.@params.clip_skip},
            //        ""facefixer_strength"": {parameters.@params.facefixer_strength},
            //        ""loras"": [{string.Join(",", parameters.@params.loras.Select(l => $"{{\"name\":\"{l.name}\",\"model\":{l.model},\"clip\":{l.clip},\"inject_trigger\":\"{l.inject_trigger}\",\"is_version\":{l.is_version.ToString().ToLower()}}}"))}],
            //        ""tis"": [{string.Join(",", parameters.@params.tis.Select(t => $"{{\"name\":\"{t.name}\",\"inject_ti\":\"{t.inject_ti}\",\"strength\":{t.strength}}}"))}],
            //        ""special"": {{{string.Join(",", parameters.@params.special.Select(kv => $"\"{kv.Key}\":\"{kv.Value}\""))}}},
            //        ""workflow"": ""{parameters.@params.workflow}"",
            //        ""transparent"": {parameters.@params.transparent.ToString().ToLower()},
            //        ""seed"": ""{parameters.@params.seed}"",
            //        ""seed_variation"": {parameters.@params.seed_variation},
            //        ""control_type"": ""{parameters.@params.control_type}"",
            //        ""image_is_control"": {parameters.@params.image_is_control.ToString().ToLower()},
            //        ""return_control_map"": {parameters.@params.return_control_map.ToString().ToLower()},
            //        ""extra_texts"": [{string.Join(",", parameters.@params.extra_texts.Select(et => $"{{\"text\":\"{et.text}\",\"reference\":\"{et.reference}\"}}"))}],
            //        ""steps"": {parameters.@params.steps},
            //        ""n"": {parameters.@params.n}
            //    }},
            //    ""nsfw"": {parameters.nsfw.ToString().ToLower()},
            //    ""trusted_workers"": {parameters.trusted_workers.ToString().ToLower()},
            //    ""validated_backends"": {parameters.validated_backends.ToString().ToLower()},
            //    ""slow_workers"": {parameters.slow_workers.ToString().ToLower()},
            //    ""extra_slow_workers"": {parameters.extra_slow_workers.ToString().ToLower()},
            //    ""censor_nsfw"": {parameters.censor_nsfw.ToString().ToLower()},
            //    ""workers"": [{string.Join(",", parameters.workers.Select(w => $"\"{w}\""))}],
            //    ""worker_blacklist"": {parameters.worker_blacklist.ToString().ToLower()},
            //    ""models"": [{string.Join(",", parameters.models.Select(m => $"\"{m}\""))}],
            //    ""source_image"": ""{parameters.source_image}"",
            //    ""source_processing"": ""{parameters.source_processing}"",
            //    ""source_mask"": ""{parameters.source_mask}"",
            //    ""extra_source_images"": [{string.Join(",", parameters.extra_source_images.Select(esi => $"{{\"image\":\"{esi.image}\",\"strength\":{esi.strength}}}"))}],
            //    ""r2"": {parameters.r2.ToString().ToLower()},
            //    ""shared"": {parameters.shared.ToString().ToLower()},
            //    ""replacement_filter"": {parameters.replacement_filter.ToString().ToLower()},
            //    ""dry_run"": {parameters.dry_run.ToString().ToLower()},
            //    ""proxied_account"": ""{parameters.proxied_account}"",
            //    ""disable_batching"": {parameters.disable_batching.ToString().ToLower()},
            //    ""allow_downgrade"": {parameters.allow_downgrade.ToString().ToLower()},
            //    ""webhook"": ""{parameters.webhook}""
            //}}";
            string jsonPayload = $@"
            {{
                ""prompt"": ""{parameters.prompt}""
            }}";

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
                    Debug.LogError($"Response: {webRequest.downloadHandler.text}");
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

        public static async Task<string> GenerateSimpleImageAsync(string prompt = "a magical forest", int width = 256, int height = 256, bool saveImage = false)
        {
            if (string.IsNullOrEmpty(prompt))
            {
                Debug.LogError("Prompt is required.");
                return null;
            }

            string url = $"{HordeBase.baseApiUrl}generate/async";

            // Construct the payload
            var payload = new
            {
                prompt = prompt,
                @params = new
                {
                    width = width,
                    height = height,
                    cfg_scale = 7, // Adjust for creativity/precision balance
                    sampler_name = "k_euler_a", // Sampling method
                    steps = 50 // Number of steps for generation
                },
                post_processing = new[] { "RealESRGAN_x4plus" } // Optional post-processing
            };

            // Serialize payload to JSON
            string jsonPayload = JsonUtility.ToJson(payload);

            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("apikey", "<Your API Key Here>"); // Replace with your actual API key

                Debug.Log($"Sending request to {url} with payload: {jsonPayload}");

                await webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Request error: {webRequest.error}");
                    Debug.LogError($"Response details: {webRequest.downloadHandler.text}");
                    return null;
                }

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("Request successful!");
                    string jsonResponse = webRequest.downloadHandler.text;
                    Debug.Log($"Response: {jsonResponse}");

                    // Process the JSON response to extract the image URL
                    string imageUrl = ExtractImageUrlFromResponse(jsonResponse);
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        Debug.Log($"Image URL: {imageUrl}");
                        await LoadImageToRenderer(imageUrl, saveImage); // Display or save the image
                    }

                    return jsonResponse;
                }
                else
                {
                    Debug.LogError($"Unexpected result: {webRequest.result}");
                    Debug.LogError($"Response: {webRequest.downloadHandler.text}");
                    return null;
                }
            }
        }


        // Save image to disk
        private static void SaveImageToDisk(byte[] imageData)
        {
            string path = Path.Combine(Application.persistentDataPath, "generated_image.png");
            File.WriteAllBytes(path, imageData);
            Debug.Log($"Image saved to: {path}");
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
        public static List<TextGenerationResponse> textGenerationResponses = new List<TextGenerationResponse>();
        public static string currentTextStyles;

        // Methode zum asynchronen Generieren von Text
        public static async Task<string> GenerateTextAsync(string prompt)
        {
            string url = $"{HordeBase.baseApiUrl}generate/text/async";

            // Manuelles Erstellen der JSON-Nutzlast
            string jsonPayload = $@"
            {{
                ""prompt"": ""{prompt}""
            }}";

            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("apikey", HordeBase.apiKey);
                webRequest.SetRequestHeader("Client-Agent", "unknown:0:unknown");

                await webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error: {webRequest.error}");
                    Debug.LogError($"Response: {webRequest.downloadHandler.text}");
                    return null;
                }
                else
                {
                    lastGeneratedText = webRequest.downloadHandler.text;
                    Debug.Log($"Response: {lastGeneratedText}");

                    // Deserialisieren der Antwort und Speichern in der Liste
                    TextGenerationResponse response = JsonUtility.FromJson<TextGenerationResponse>(lastGeneratedText);
                    textGenerationResponses.Add(response);

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
                    var response = textGenerationResponses.FirstOrDefault(r => r.id == id);
                    if (response != null)
                    {
                        textGenerationResponses.Remove(response);
                    }
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
            foreach (var requestId in textGenerationResponses)
            {
                await CancelTextGeneration(requestId.id);
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

