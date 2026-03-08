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
        var searchRoots = new[] {
            AppContext.BaseDirectory,
            Path.Combine(AppContext.BaseDirectory, "Prompts"),
            Environment.CurrentDirectory,
            Path.Combine(Environment.CurrentDirectory, "FirstApp"),
            Path.Combine(Environment.CurrentDirectory, "FirstApp", "Prompts"),
            Path.Combine(Environment.CurrentDirectory, "..", "FirstApp", "Prompts")
        };

        var sb = new StringBuilder();
        foreach (var fileName in fileNames)
        {
            var found = false;
            foreach (var root in searchRoots)
            {
                try
                {
                    var path = Path.Combine(root ?? string.Empty, fileName ?? string.Empty);
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
            var config = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();

            // try common keys
            var token = config["ai_token"] ?? config["token"];
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
        var newMessage = new ChatMessage(AI.ChatRole.User, inResponse);
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
            Console.WriteLine(WrapText(response, maxcharsInALine, nlAfterParanlAfterPara));
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

    static string WrapText(string text, int maxWidth = 80, bool nlAfterPara = true)
    {
        if (string.IsNullOrWhiteSpace(text) || maxWidth < 10)
            return text ?? string.Empty;

        // Preserve paragraph breaks: split on empty lines
        //var paragraphs = Regex.Split(text.Trim(), @"\r?\n\s*\r?\n");
        var paragraphs = Regex.Split(text.Trim(), "\n");
        var outSb = new StringBuilder();
        foreach (var para1 in paragraphs)
        {
            var para = para1.Replace("\r", "").Replace("\n", ""); // Normalize line breaks
            if (string.IsNullOrWhiteSpace(para))
            {
                continue;
            }
            var words = Regex.Split(para.Trim(), "\\s+").Where(w => w.Length > 0).ToArray();
            var lineSb = new StringBuilder();
            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i];
                if (lineSb.Length == 0)
                {
                    lineSb.Append(word);
                }
                else if (lineSb.Length + 1 + word.Length <= maxWidth)
                {
                    lineSb.Append(' ').Append(word);
                }
                else
                {
                    outSb.AppendLine(lineSb.ToString());
                    lineSb.Clear();
                    lineSb.Append(word);
                }
            }
            if (lineSb.Length > 0)
            {
                outSb.AppendLine(lineSb.ToString());
            }
            if (nlAfterPara)
            {
                outSb.AppendLine(); // paragraph separator
            }
            //
        }
        return outSb.ToString().TrimEnd();
    }

    public static async Task<string> MakeCharacter(IChatClient chatClient, string prompt)
    {
        var therapistHistory = new List<ChatMessage>
        {
            new ChatMessage(AI.ChatRole.System, prompt)
        };

        var therapyResponse = await AiTools.DoRespond(chatClient, therapistHistory, string.Empty, string.Empty, maxcharsInALine, nlAfterParanlAfterPara);
        return therapyResponse;
    }
}
