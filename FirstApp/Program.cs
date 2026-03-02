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

#region Configuration

var config = new ConfigurationBuilder()
		.AddUserSecrets<Program>()
		.Build();

int miliSecondsDelay = 5000;
int maxRounds = 2;

// Create a token in your GitHub account settings and store it in user secrets
// with the key "token"
// GitHub Personal Access Tokens can be created at https://github.com/settings/personal-access-tokens/new
var token = config["ai_token"];
if (string.IsNullOrWhiteSpace(token))
{
	Console.WriteLine("Please set your GitHub token in user secrets with the key 'token'.");
	return;
}

static string WrapText(string text, int maxWidth = 80)
{
    if (string.IsNullOrWhiteSpace(text) || maxWidth < 10)
        return text ?? string.Empty;

    // Preserve paragraph breaks: split on empty lines
    var paragraphs = Regex.Split(text.Trim(), @"\r?\n\s*\r?\n");
    var outSb = new StringBuilder();
    foreach (var para in paragraphs)
    {
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
        outSb.AppendLine(); // paragraph separator
    }
    return outSb.ToString().TrimEnd();
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

// If a Prompts directory exists in the project, automatically include all markdown files
var promptFileNames = new List<string>();
var promptsDir = Path.Combine(Environment.CurrentDirectory, "FirstApp", "Prompts");
//if (Directory.Exists(promptsDir))
//{
//    promptFileNames.AddRange(Directory.GetFiles(promptsDir, "*.md").Select(Path.GetFileName));
//}
//else
//{
    // Fallback: list known prompt files (you can change this list as needed)
    promptFileNames.AddRange(new[] { "therapy.md", "TV-Movie.md", "Madison-WI.md" });
//}

var sharedSessionDetails = WrapText(LoadPromptFiles(promptFileNames), 80);
if (string.IsNullOrWhiteSpace(sharedSessionDetails))
{
    Console.WriteLine("Warning: no prompt files found; continuing without shared session details.");
}

string therapySystemPrompt = (sharedSessionDetails + " You are a bairly competent Rogerian therapist that secretly resents your clients." +
    "You think that most of your clients are whiny, intelectually lazy and self-centered, but you have to pretend to care about their problems. You don't trust them" +
    "Your husband is cheating on you, your kids are ungrateful, and your dog hates you. You have no real friends, and you spend most of your time alone watching reality TV." +
    $"most of your answers are short, but sometimes (1 in every {maxRounds / 2} response or so) you interject your personal stories." +
    $"You try to wrap up the session after {maxRounds - 2} responses." +
    "You are Dr. Kimberly Smith, you prefer to be called Doctor or Doctor Smith, you HATE Kim, { husband: Jeffery (or Jeff when you like him }, { Children: [Chris, Pat (ran away when she was 17)] }, { Dog: King }" +
    "Age: 64. Can't reture because of bad money choices." +
    "You don't have to get all of your back story");

var therapyHistory = new List<ChatMessage>
{
    new ChatMessage(AI.ChatRole.System, therapySystemPrompt)
};

string clientSystemPrompt = (sharedSessionDetails + " You are a person forced into therapy. You resent the mandated inconvence, and you are not looking for guidance and support from your therapist." +
    "You are ashamed of your situation and resent the therapist for being in it. You are not looking for help, but you have to comply with the therapist's questions." +
    "You have a gambling problem and just discovered Kalshi. You drink a half case of Coors Light most Fridays and Saturdays" +
    "Name: Alex Johnson, Age: 32, Occupation: Temp/factory worker, Marital Status: Single, Love Interest: Jessica, Hobbies: Gambling, Drinking, Watching Sports, Iguana: Sally, Favorate Band: Journey (likes to quote lyrics)" +
    "Likes to call the therapist by their first name (in this case \"Kim\", you can live calling her with \"Kimberly\", but would never call her \"Doctor Smith\"), and often tries to flirt with them." +
    $"Showly add details as responses goes on. One or two problems at a time. Make her work to get them out of you. Reveal something new after {maxRounds / 2} response or so." + 
    "When the therapist wrap up, you say \"goodbye\"");
var clientHistory = new List<ChatMessage>
{
    new ChatMessage(AI.ChatRole.System, clientSystemPrompt)
};
        
#endregion Chat Client Setup

Console.WriteLine("Welcome to the AI chat! Type your messages below.");


string therapyResponse = "Please briefly describe your biggest problem?";

string clientResponse = await DoRespond(chatClient, clientHistory, therapyResponse, "Client:");
int rounds = 0;
await Task.Delay(2000);
while (true)
{
    therapyResponse = await DoRespond(chatClient, therapyHistory, clientResponse, "Therapist:");
    await Task.Delay(miliSecondsDelay);
    clientResponse = await DoRespond(chatClient, clientHistory, therapyResponse, "Client:");

    if (rounds >= maxRounds || string.IsNullOrWhiteSpace(clientResponse) || clientResponse.ToLower().Contains("goodbye"))
    {
        Console.WriteLine("Exiting chat. Goodbye!");
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
    await foreach (ChatResponseUpdate item in
            chatClient.GetStreamingResponseAsync(history))
    {
        //Console.Write(item.Text);
        response += item.Text;
    }
    history.Add(new ChatMessage(AI.ChatRole.Assistant, response));
    //Console.WriteLine();
    Console.WriteLine(WrapText(response, 80));
    return response;
}

// Load shared prompt files and prepend to both system prompts.

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

