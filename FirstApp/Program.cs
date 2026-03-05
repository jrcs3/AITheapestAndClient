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
const string decade = "1980s";

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
string sharedVariables = "{ $MaxRounds: " + maxRounds.ToString() + 
    " $MinRounds: " + (maxRounds - 2).ToString() + 
    ", $HalfMaxRounds: " + (maxRounds / 2).ToString() +
    ", $Decade: \"" + decade + "\"" +
    " }\r\n";
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

string clientDetails = await AiTools.MakeCharacter(chatClient, sharedVariables + AiTools.GetPrompt("CharacterDesigner.md"));
string clientSystemPrompt = (
    sharedVariables + "\r\nYou are to play the following character\r\n" + clientDetails + sharedSessionDetails + clientSessionDetails + 
    //clientSessionDetails + sharedSessionDetails + " " +
    //"Likes to call the Therapist by their first name (in this case \"Kim\", you can live calling her with \"Kimberly\", but would never call her \"Doctor Smith\"), and often tries to flirt with them." +
    "- Reveal something new after $HalfMaxRounds response or so. " +
    //"- The Therapist's (not you) name is Dr. Kimberly (Kim) Smith " +
    "- When the therapist wrap up, you say \"goodbye\" ");
var clientHistory = new List<ChatMessage>
{
    new ChatMessage(AI.ChatRole.System, clientSystemPrompt)
};
        
#endregion Chat Client Setup

Console.WriteLine("Welcome to AI Therapy");


string therapyResponse = "Introduce yourself and briefly describe your biggest problem?";

string clientResponse = await AiTools.DoRespond(chatClient, clientHistory, $"Stage Direction: {therapyResponse}", "\r\nClient:", maxcharsInALine, nlAfterParanlAfterPara);
int rounds = 0;
//await Task.Delay(2000);
while (true)
{
    therapyResponse = await AiTools.DoRespond(chatClient, therapistHistory, $"Client: {clientResponse}", "\r\nTherapist:", maxcharsInALine, nlAfterParanlAfterPara);
    await Task.Delay(miliSecondsDelay);
    clientResponse = await AiTools.DoRespond(chatClient, clientHistory, $"nTherapist: {therapyResponse}", "\r\nClient:", maxcharsInALine, nlAfterParanlAfterPara);

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

clientResponse = "Doctor, could you give your assessment of the Client's case?" +
    "- Include name, relationships, problem, aproximate age, your diagnosis, and any biographical details you have picked up." +
    "- Include your thoughts on the client's problem, and what you think is the root cause." +
    "- Use markdown formatting to make the response easier to read, for example use headings, bullet points, and bold text where appropriate.";
therapyResponse = await AiTools.DoRespond(chatClient, therapistHistory, $"Stage Direction: {clientResponse}", "\r\nTherapist:", maxcharsInALine, false);



// WrapText moved to AiTools; Program still had a local reference but uses AiTools.DoRespond which prints wrapped text.
