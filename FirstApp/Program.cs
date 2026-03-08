using Azure;
using Azure.AI.Inference;
using FirstApp;
using Microsoft.Extensions.AI;
using AI = Microsoft.Extensions.AI;

const int miliSecondsDelay = 5000;
const int maxRounds = 4;
const int maxcharsInALine = 80;
const bool nlAfterParanlAfterPara = true;
const string decade = "Gay Nineties";

#region Configuration

var token = AiTools.GetToken();
if (string.IsNullOrWhiteSpace(token))
{
    Console.WriteLine("Error: AI inference token not found.");
    return;
}

//// The endpoint for GitHub's AI Inference service
//// Documentation: https://docs.github.com/en/copilot/ai-inference

#endregion Configuration

#region Chat Client Setup
IChatClient chatClient = new ChatCompletionsClient(AiTools.GetInferenceEndpoint(), new AzureKeyCredential(token))
    .AsIChatClient(AiTools.GetModelName());

var sharedSessionDetails = AiTools.LoadPromptFiles(new List<string>{ "Therapy.md", "TV-Movie.md", "Madison-WI.md" });
string sharedVariables = "{ $MaxRounds: " + maxRounds.ToString() + 
    " $MinRounds: " + (maxRounds - 2).ToString() + 
    ", $HalfMaxRounds: " + (maxRounds / 2).ToString() +
    ", $Decade: \"" + decade + "\"" +
    " }\r\n- You are living in " + decade + " and any references to it are considered modern, not nostalgic\r\n";
//var sharedSessionDetails = AiTools.LoadPromptFiles(new List<string> { "Therapy.md", "Shakespear.md", "DarkForest.md" });
var therapistSessionDetails = AiTools.LoadPromptFiles(new List<string> { "Therapist.md", "KimberlySmith.md" });
//var clientSessionDetails = AiTools.LoadPromptFiles(new List<string> { "Client.md", "AlexJohnson.md" });
var clientSessionDetails = AiTools.LoadPromptFiles(new List<string> { "Client.md" });
if (string.IsNullOrWhiteSpace(sharedSessionDetails))
{
    Console.WriteLine("Warning: no prompt files found; continuing without shared session details.");
}

string therapistSystemPrompt = (
    sharedVariables + 
    therapistSessionDetails + sharedSessionDetails +
    "\r\n" +
    "You are Dr. Kimberly Smith, you prefer to be called Doctor or Doctor Smith, you HATE Kim." +
    "");

var therapistHistory = new List<ChatMessage>
{
    new ChatMessage(AI.ChatRole.System, therapistSystemPrompt)
};
Console.WriteLine("Today's client:\r\n");
string clientDetails = await AiTools.MakeCharacter(chatClient, sharedVariables + AiTools.GetPrompt("CharacterDesigner.md"));
string clientSystemPrompt = (
    sharedVariables + "\r\nYou are to play the following character\r\n" + clientDetails + sharedSessionDetails + clientSessionDetails + 
    "- Reveal something new after $HalfMaxRounds response or so. " +
    "- When the therapist wrap up, you say \"goodbye\" ");
var clientHistory = new List<ChatMessage>
{
    new ChatMessage(AI.ChatRole.System, clientSystemPrompt)
};
        
#endregion Chat Client Setup

Console.WriteLine("\r\n# Welcome to AI Therapy");


string therapyResponse = "Introduce yourself and briefly describe your biggest problem?";

string clientResponse = await AiTools.DoRespond(chatClient, clientHistory, $"Stage Direction: {therapyResponse}", "\r\nClient:", maxcharsInALine, nlAfterParanlAfterPara);
int rounds = 0;
//await Task.Delay(2000);
while (true)
{
    therapyResponse = await AiTools.DoRespond(chatClient, therapistHistory, $"Client: {clientResponse}", "\r\nTherapist:", maxcharsInALine, nlAfterParanlAfterPara);
    await Task.Delay(miliSecondsDelay);
    clientResponse = await AiTools.DoRespond(chatClient, clientHistory, $"Therapist: {therapyResponse}", "\r\nClient:", maxcharsInALine, nlAfterParanlAfterPara);

    if (string.IsNullOrWhiteSpace(clientResponse) || clientResponse.ToLower().Contains("goodbye"))
    {
        Console.WriteLine("\r\n-- Session Over --");
        break;
    }
    if (rounds >= maxRounds)
    {
        Console.WriteLine("\r\n-- Maximum rounds reached --");
        break;
    }
    await Task.Delay(miliSecondsDelay);
    rounds++;
}

// Load assessment request from prompt file instead of inline string
var assessmentRequest = AiTools.GetPrompt("AssessmentRequest.md");
therapyResponse = await AiTools.DoRespond(chatClient, therapistHistory, $"Epilogue: {assessmentRequest}", "\r\nTherapist:", maxcharsInALine, false);



// WrapText moved to AiTools; Program still had a local reference but uses AiTools.DoRespond which prints wrapped text.
