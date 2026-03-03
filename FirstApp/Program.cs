using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using AI = Microsoft.Extensions.AI;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

#region Configuration

var config = new ConfigurationBuilder()
		.AddUserSecrets<Program>()
		.Build();

const int miliSecondsDelay = 5000;
const int maxRounds = 5;
const int maxcharsInALine = 120;
const bool nlAfterParanlAfterPara = false;

// Create a token in your GitHub account settings and store it in user secrets
// with the key "token"
// GitHub Personal Access Tokens can be created at https://github.com/settings/personal-access-tokens/new
var token = config["ai_token"];
if (string.IsNullOrWhiteSpace(token))
{
	Console.WriteLine("Please set your GitHub token in user secrets with the key 'token'.");
	return;
}


// The endpoint for GitHub's AI Inference service
// Documentation: https://docs.github.com/en/copilot/ai-inference
var endpoint = new Uri("https://models.github.ai/inference");

// This is a free model provided by GitHub for testing, development, and educational purposes
var model = "openai/gpt-4.1-mini";


#endregion Configuration

#region Chat Client Setup
var chatClient = new ChatCompletionsClient(endpoint, new AzureKeyCredential(token))
    .AsIChatClient(model);

//var sharedSessionDetails = LoadPromptFiles(new List<string>{ "Therapy.md", "TV-Movie.md", "Madison-WI.md" });
var sharedSessionDetails = LoadPromptFiles(new List<string> { "Therapy.md", "Shakespear.md", "Madison-WI.md" });
var therapistSessionDetails = LoadPromptFiles(new List<string> { "Therapist.md", "KimberlySmith.md" });
var clientSessionDetails = LoadPromptFiles(new List<string> { "Client.md", "AlexJohnson.md" });
if (string.IsNullOrWhiteSpace(sharedSessionDetails))
{
    Console.WriteLine("Warning: no prompt files found; continuing without shared session details.");
}

string therapistSystemPrompt = (
    "{ $MaxRounds: " + maxRounds.ToString() + " $MinRounds: " + (maxRounds - 2).ToString() + ", $HalfMaxRounds: " + (maxRounds/2).ToString() + " }" + 
    therapistSessionDetails + sharedSessionDetails +
    "\r\n" +
    "You are Dr. Kimberly Smith, you prefer to be called Doctor or Doctor Smith, you HATE Kim." +
    "");

var therapistHistory = new List<ChatMessage>
{
    new ChatMessage(AI.ChatRole.System, therapistSystemPrompt)
};

string clientSystemPrompt = (
    "{ $MaxRounds: " + maxRounds.ToString() + " $MinRounds: " + (maxRounds - 2).ToString() + ", $HalfMaxRounds: " + (maxRounds / 2).ToString() + " }" +
    clientSessionDetails + sharedSessionDetails + " " +
    "Likes to call the therapist by their first name (in this case \"Kim\", you can live calling her with \"Kimberly\", but would never call her \"Doctor Smith\"), and often tries to flirt with them." +
    $" Reveal something new after $HalfMaxRounds response or so." + 
    "When the therapist wrap up, you say \"goodbye\"");
var clientHistory = new List<ChatMessage>
{
    new ChatMessage(AI.ChatRole.System, clientSystemPrompt)
};
        
#endregion Chat Client Setup

Console.WriteLine("Welcome to the AI chat! Type your messages below.");


string therapyResponse = "Please briefly describe your biggest problem?";

string clientResponse = await DoRespond(chatClient, clientHistory, therapyResponse, "\r\nClient:");
int rounds = 0;
//await Task.Delay(2000);
while (true)
{
    therapyResponse = await DoRespond(chatClient, therapistHistory, clientResponse, "\r\nTherapist:");
    await Task.Delay(miliSecondsDelay);
    clientResponse = await DoRespond(chatClient, clientHistory, therapyResponse, "\r\nClient:");

    if (rounds >= maxRounds)
    {
        Console.WriteLine("-- Maximum rounds reached --");
        break;
    }
    if (string.IsNullOrWhiteSpace(clientResponse) || clientResponse.ToLower().Contains("goodbye"))
    {
        Console.WriteLine("-- Session Over --");
        break;
    }
    await Task.Delay(miliSecondsDelay);
    rounds++;
}

static async Task<string> DoRespond(IChatClient chatClient, List<ChatMessage> history, string inResponse, string displayTitle)
{
    history.Add(new ChatMessage(AI.ChatRole.User, inResponse));

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

static string LoadPromptFiles(IEnumerable<string> fileNames)
{
    var searchRoots = new[] {
        AppContext.BaseDirectory,
        Path.Combine(AppContext.BaseDirectory, "Prompts"),
        Environment.CurrentDirectory,
        Path.Combine(Environment.CurrentDirectory, "FirstApp"),
        Path.Combine(Environment.CurrentDirectory, "FirstApp", "Prompts")
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
