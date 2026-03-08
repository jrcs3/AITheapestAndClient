using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using AI = Microsoft.Extensions.AI;

namespace FirstApp;

public static class AiTools
{
    const int maxcharsInALine = 120;
    const bool nlAfterParanlAfterPara = false;

    public static string GetPrompt(string v) => LoadPromptFiles(new List<string> { v });

    public static string LoadPromptFiles(IEnumerable<string> fileNames)
    {
        string[] searchRoots = new[] {
            AppContext.BaseDirectory,
            Path.Combine(AppContext.BaseDirectory, "Prompts"),
            Environment.CurrentDirectory,
            Path.Combine(Environment.CurrentDirectory, "FirstApp"),
            Path.Combine(Environment.CurrentDirectory, "FirstApp", "Prompts"),
            Path.Combine(Environment.CurrentDirectory, "..", "FirstApp", "Prompts")
        };

        StringBuilder sb = new StringBuilder();
        foreach (string fileName in fileNames)
        {
            bool found = false;
            foreach (string root in searchRoots)
            {
                try
                {
                    string path = Path.Combine(root ?? string.Empty, fileName ?? string.Empty);
                    if (File.Exists(path))
                    {
                        sb.AppendLine(File.ReadAllText(path));
                        found = true;
                        break;
                    }
                }
                catch { /* ignore inaccessible paths */ }
            }
            if (!found)
            {
                Console.WriteLine($"Warning: prompt file '{fileName}' not found in known locations.");
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Returns the default AI inference endpoint.
    /// </summary>
    public static Uri GetInferenceEndpoint()
    {
        // Hard-coded for now; change to configuration if needed later
        return new Uri("https://models.github.ai/inference");
    }

    public static string GetModelName()
    {
        // Hard-coded for now; change to configuration if needed later
        return "openai/gpt-4.1-mini";
    }

    /// <summary>
    /// Load the AI token from user secrets (key: "ai_token" or "token").
    /// Writes a message and returns null when not found.
    /// </summary>
    public static string? GetToken()
    {
        try
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();

            // try common keys
            string? token = config["ai_token"] ?? config["token"];
            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("Please set your GitHub token in user secrets with the key 'ai_token' or 'token'.");
                return null;
            }
            return token;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading user secrets for AI token: {ex.Message}");
            return null;
        }
    }

    public static IChatClient GetChatClient(string token)
    {
        IChatClient chatClient = new ChatCompletionsClient(AiTools.GetInferenceEndpoint(), new AzureKeyCredential(token))
            .AsIChatClient(AiTools.GetModelName());
        return chatClient;
    }

    public static async Task<string> DoRespond(IChatClient chatClient, List<ChatMessage> history, string inResponse, string displayTitle, int maxcharsInALine = 80, bool nlAfterParanlAfterPara = true)
    {
        ChatMessage newMessage = new ChatMessage(AI.ChatRole.User, inResponse);
        try
        {
            history.Add(newMessage);

            // Stream the AI response and add to chat history
            Console.WriteLine(displayTitle);
            string response = "";
            await foreach (ChatResponseUpdate item in chatClient.GetStreamingResponseAsync(history))
            {
                response += item.Text;
            }
            history.Add(new ChatMessage(AI.ChatRole.Assistant, response));
            Console.WriteLine(SharedTools.WrapText(response, maxcharsInALine, nlAfterParanlAfterPara));
            return response;
        }
        catch (Exception ex)
        {
            string longLine = "=============================================================";
            Console.WriteLine($"\r\n{longLine}\r\nError: {ex.Message}\r\n{longLine}\r\n");
            history.Remove(newMessage);
            return string.Empty;
        }
    }


    public static async Task<string> MakeCharacter(IChatClient chatClient, string prompt, int maxcharsInALine = maxcharsInALine, bool nlAfterParanlAfterPara = nlAfterParanlAfterPara)
    {
        List<ChatMessage> therapistHistory = new List<ChatMessage>
        {
            new ChatMessage(AI.ChatRole.System, prompt)
        };

        string therapyResponse = await AiTools.DoRespond(chatClient, therapistHistory, string.Empty, string.Empty, maxcharsInALine, nlAfterParanlAfterPara);
        return therapyResponse;
    }
}
