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
using FirstApp;

const int miliSecondsDelay = 5000;
const int maxRounds = 2;
const int maxcharsInALine = 80;
const bool nlAfterParanlAfterPara = true;

#region Configuration

var token = AiTools.GetToken();
if (string.IsNullOrWhiteSpace(token))
{
    return;
}

//// The endpoint for GitHub's AI Inference service
//// Documentation: https://docs.github.com/en/copilot/ai-inference
//var endpoint = AiTools.GetInferenceEndpoint();

//// This is a free model provided by GitHub for testing, development, and educational purposes
//var model = AiTools.GetModelName();

#endregion Configuration

#region Chat Client Setup
IChatClient chatClient = new ChatCompletionsClient(AiTools.GetInferenceEndpoint(), new AzureKeyCredential(token))
    .AsIChatClient(AiTools.GetModelName());

var sharedSessionDetails = AiTools.LoadPromptFiles(new List<string>{ "Therapy.md", "TV-Movie.md", "Madison-WI.md" });
string sharedVariables = "{ $MaxRounds: " + maxRounds.ToString() + " $MinRounds: " + (maxRounds - 2).ToString() + ", $HalfMaxRounds: " + (maxRounds / 2).ToString() + " }";
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

string clientDetails = await AiTools.MakeCharacter(chatClient, AiTools.GetPrompt("CharacterDesigner.md"));
string clientSystemPrompt = (
    sharedVariables + sharedSessionDetails + clientDetails + clientSessionDetails + 
    //clientSessionDetails + sharedSessionDetails + " " +
    //"Likes to call the therapist by their first name (in this case \"Kim\", you can live calling her with \"Kimberly\", but would never call her \"Doctor Smith\"), and often tries to flirt with them." +
    $" Reveal something new after $HalfMaxRounds response or so." + 
    "When the therapist wrap up, you say \"goodbye\"");
var clientHistory = new List<ChatMessage>
{
    new ChatMessage(AI.ChatRole.System, clientSystemPrompt)
};
        
#endregion Chat Client Setup

Console.WriteLine("Welcome to AI Therapy");


string therapyResponse = "Introduce yourself and briefly describe your biggest problem?";

string clientResponse = await AiTools.DoRespond(chatClient, clientHistory, therapyResponse, "\r\nClient:", maxcharsInALine, nlAfterParanlAfterPara);
int rounds = 0;
//await Task.Delay(2000);
while (true)
{
    therapyResponse = await AiTools.DoRespond(chatClient, therapistHistory, clientResponse, "\r\nTherapist:", maxcharsInALine, nlAfterParanlAfterPara);
    await Task.Delay(miliSecondsDelay);
    clientResponse = await AiTools.DoRespond(chatClient, clientHistory, therapyResponse, "\r\nClient:", maxcharsInALine, nlAfterParanlAfterPara);

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



// WrapText moved to AiTools; Program still had a local reference but uses AiTools.DoRespond which prints wrapped text.
