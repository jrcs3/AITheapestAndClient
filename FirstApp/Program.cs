using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using AI = Microsoft.Extensions.AI;

#region Configuration

var config = new ConfigurationBuilder()
		.AddUserSecrets<Program>()
		.Build();

// Create a token in your GitHub account settings and store it in user secrets
// with the key "token"
// GitHub Personal Access Tokens can be created at https://github.com/settings/personal-access-tokens/new
var token = config["token"];
if (string.IsNullOrWhiteSpace(token))
{
	Console.WriteLine("Please set your GitHub token in user secrets with the key 'token'.");
	return;
}

// The endpoint for GitHub's AI Inference service
// Documentation: https://docs.github.com/en/copilot/ai-inference
var endpoint = new Uri("https://models.github.ai/inference");

// This is a free model provided by GitHub for testing, development, and educational purposes
var model = "openai/gpt-5-mini";


#endregion Configuration

#region Chat Client Setup
var chatClient = new ChatCompletionsClient(endpoint, new AzureKeyCredential(token))
	.AsIChatClient(model);

string systemPrompt = "You are a helpful AI assistant that helps people find information.";

var history = new List<ChatMessage>
{
	new ChatMessage(AI.ChatRole.System, systemPrompt)
};

#endregion Chat Client Setup

Console.WriteLine("Welcome to the AI chat! Type your messages below.");

while (true)
{
	// Get user prompt and add to chat history
	Console.WriteLine("Your prompt:");
	string? userPrompt = Console.ReadLine();

	if (string.IsNullOrWhiteSpace(userPrompt) || userPrompt.Equals("exit", StringComparison.OrdinalIgnoreCase))
	{
		Console.WriteLine("Exiting chat. Goodbye!");
		break;
	}

	history.Add(new ChatMessage(AI.ChatRole.User, userPrompt));

	// Stream the AI response and add to chat history
	Console.WriteLine("AI Response:");
	string response = "";
	await foreach (ChatResponseUpdate item in
			chatClient.GetStreamingResponseAsync(history))
	{
		Console.Write(item.Text);
		response += item.Text;
	}
	history.Add(new ChatMessage(AI.ChatRole.Assistant, response));
	Console.WriteLine();
}