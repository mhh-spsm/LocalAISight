using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LocalAISight;

public class OllamaClient
{
    private readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
    private const string LocalOllamaUrl = "http://localhost:11434/api/generate";
    private const string DefaultQuestion = "Beskriv det du ser här";

    public async Task<string> GetDescriptionAsync(string img, string question = null, string model = null)
    {
        // Prefer values from active profile if present
        var active = ProfilesStore.Instance.ActiveProfile;
        var myPrompt = !string.IsNullOrEmpty(question) ? question : (active?.DefaultPrompt ?? string.Empty);
        var mySystemPrompt = active?.SystemPrompt ?? string.Empty;
        var myModel = model ?? active?.Model;
        var payload = new
        {
            model = myModel,
            system = mySystemPrompt,
            prompt = myPrompt,
            images = new[] { img },
            stream = false,
            options = new { temperature = 0 },
            keep_alive = -1
        };
        var myUrl = LocalOllamaUrl;
        if (Properties.Settings.Default.UseExternalServer && !string.IsNullOrEmpty(Properties.Settings.Default.ExternalIP))
        {
            myUrl = $"http://{Properties.Settings.Default.ExternalIP}:11434/api/generate";
        }
        try
        {
            var response = await _httpClient.PostAsJsonAsync(myUrl, payload);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("response").GetString();
        }
        catch (Exception ex)
        {
            return $"Fel vid kontakt med AI-modellen: {ex.ToString()}";
        }
    }

    /// <summary>
    /// Fetches available models from a local (or external) Ollama server.
    /// Tries to handle different possible JSON shapes returned by the server.
    /// </summary>
    /// <param name="externalIp">If non-null, will query that host instead of localhost.</param>
    /// <returns>List of model names (may be empty).</returns>
    public async Task<List<string>> GetModelsAsync(string externalIp = null)
    {
        var url = $"http://localhost:11434/api/tags";
        if (!string.IsNullOrWhiteSpace(externalIp))
        {
            url = $"http://{externalIp}:11434/api/models";
        }

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var results = new List<string>();

            if (json.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in json.EnumerateArray())
                {
                    if (el.ValueKind == JsonValueKind.String)
                    {
                        results.Add(el.GetString());
                    }
                    else if (el.ValueKind == JsonValueKind.Object)
                    {
                        if (el.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
                            results.Add(nameProp.GetString());
                        else if (el.TryGetProperty("model", out var modelProp) && modelProp.ValueKind == JsonValueKind.String)
                            results.Add(modelProp.GetString());
                        else
                            results.Add(el.ToString());
                    }
                    else
                    {
                        results.Add(el.ToString());
                    }
                }
            }
            else if (json.ValueKind == JsonValueKind.Object)
            {
                // Some implementations return an object with a "models" array
                if (json.TryGetProperty("models", out var modelsProp) && modelsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in modelsProp.EnumerateArray())
                    {
                        if (el.ValueKind == JsonValueKind.String)
                            results.Add(el.GetString());
                        else if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
                            results.Add(nameProp.GetString());
                        else
                            results.Add(el.ToString());
                    }
                }
            }

            return results;
        }
        catch
        {
            // On any error, return empty list — caller can fall back to saved value.
            return new List<string>();
        }
    }
}