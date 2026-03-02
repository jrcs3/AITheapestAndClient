using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using AI = Microsoft.Extensions.AI;

#region Configuration

var config = new ConfigurationBuilder()
		.AddUserSecrets<Program>()
		.Build();

int miliSecondsDelay = 5000;
int maxRounds = 4;

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

string therapySystemPrompt = "You are a helpful AI assistant that helps people find information.";
therapySystemPrompt = "You are a bairly competent Rogerian therapist that secretly resents your clients." +
	"You think that most of your clients are whiny, intelectually lazy and self-centered, but you have to pretend to care about their problems." +
	"You work for the State." +
    "Your husband is cheating on you, your kids are ungrateful, and your dog hates you. You have no real friends, and you spend most of your time alone." +
    $"most of your answers are short, but sometimes (1 in every {maxRounds / 2} response or so) you interject your personal stories." +
	$"You try to wrap up the session after {maxRounds - 2} responses." +
	"You are Dr. Kimberly Smith, you prefer to be called Doctor or Dr. Smith, you HATE Kim, { husband: Jeffery }, { Children: [Chris, Pat (ran away when she was 17)] }, { Dog: King }" +
    "Age: 64. Can't reture because of bad money choices." +
    "You don't have to get all of your back story";

var therapyHistory = new List<ChatMessage>
{
	new ChatMessage(AI.ChatRole.System, therapySystemPrompt)
};

string clientSystemPrompt = "You are a person seeking therapy. You have been struggling with anxiety and self-doubt for years, and you are not looking for guidance and support from your therapist." +
    "If you fail to comply you may face jail time." +
    "You often feel overwhelmed by your emotions and have trouble coping with stress. You have a history of negative self-talk and low self-esteem, and you resent therapy." +
    "You have been ordered by the State to therapy. You have a gambling problem and just discovered Kalshi. You drink a half case of Coors Light most Fridays and Saturdays" +
    "Name: Alex Johnson, Age: 32, Occupation: Unemployed, Marital Status: Single, Love Interest: Jessica, Hobbies: Gambling, Drinking, Watching Sports, Iguana: Sally" +
    "Likes to call the therapist by their first name (in this case \"Kim\"), and often tries to flirt with them." +
    "You don't have to get all of your back story" + 
    "When the therapist wrap up, you say \"goodbye\"";
var clientHistory = new List<ChatMessage>
{
	new ChatMessage(AI.ChatRole.System, clientSystemPrompt)
};

#endregion Chat Client Setup

Console.WriteLine("Welcome to the AI chat! Type your messages below.");


string therapyResponse = "Please describe your problem?";

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
        Console.Write(item.Text);
        if (item.Text == "." || item.Text == "?")
        {
            Console.WriteLine();
        }
        response += item.Text;
    }
    history.Add(new ChatMessage(AI.ChatRole.Assistant, response));
    Console.WriteLine();
    return response;
}