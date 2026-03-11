using Azure;
using Azure.AI.Inference;
using FirstApp;
using Microsoft.Extensions.AI;
using AI = Microsoft.Extensions.AI;

const int miliSecondsDelay = 5000;
const int maxRounds = 2;
const int maxcharsInALine = 80;
const bool nlAfterParanlAfterPara = true;
const string decade = "1980s";
const string TherapistName = "Dr. Maren Ellery";

#region Configuration

string? token = AiTools.GetToken();
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

string sharedSessionDetails = AiTools.LoadPromptFiles(new List<string>{ "Therapy.md", "TV-Movie.md", "Madison-WI.md" });
string sharedVariables = "{ $MaxRounds: " + maxRounds.ToString() + 
    " $MinRounds: " + (maxRounds - 2).ToString() + 
    ", $HalfMaxRounds: " + (maxRounds / 2).ToString() +
    ", $Decade: \"" + decade + "\"" +
    " }\r\n- You are living in " + decade + " and any references to it are considered modern, not nostalgic\r\n";
string therapistSessionDetails = AiTools.LoadPromptFiles(new List<string> { "Therapist.md", "MarenEllery.md" });
string clientSessionDetails = AiTools.LoadPromptFiles(new List<string> { "Client.md" });
if (string.IsNullOrWhiteSpace(sharedSessionDetails))
{
    Console.WriteLine("Warning: no prompt files found; continuing without shared session details.");
}

string therapistSystemPrompt = (
    sharedVariables + 
    therapistSessionDetails + sharedSessionDetails +
    "\r\n" +
    "You are Dr. Maren Ellery" +
    "");

List<ChatMessage> therapistHistory = new List<ChatMessage>
{
    new ChatMessage(AI.ChatRole.System, therapistSystemPrompt)
};
Console.WriteLine("# Client's Profile\r\n");
string characterDesignerPrompt = AiTools.GetPrompt("CharacterDesigner.md");
string clientDetails = await AiTools.MakeCharacter(chatClient, sharedVariables + characterDesignerPrompt, maxcharsInALine, false);
string clientSystemPrompt = (
    sharedVariables + "\r\nYou are to play the following character\r\n" + clientDetails + sharedSessionDetails + clientSessionDetails + 
    "- Reveal something new after $HalfMaxRounds response or so. " +
    "- When the therapist wrap up, you say \"goodbye\" ");
List<ChatMessage> clientHistory = new List<ChatMessage>
{
    new ChatMessage(AI.ChatRole.System, clientSystemPrompt)
};
        
#endregion Chat Client Setup

Console.WriteLine("\r\n# Welcome to AI Therapy");

List<ChatMessage> assessmentHistory = new List<ChatMessage>();
string therapyResponse = "Introduce yourself and briefly describe your biggest problem?";
assessmentHistory.Add(new ChatMessage(AI.ChatRole.User, therapyResponse));

string clientResponse = await AiTools.DoRespond(chatClient, clientHistory, $"Stage Direction: {therapyResponse}", "\r\nClient:", maxcharsInALine, nlAfterParanlAfterPara);
assessmentHistory.Add(new ChatMessage(AI.ChatRole.User, clientResponse));
int rounds = 0;
//await Task.Delay(2000);
while (true)
{
    therapyResponse = await AiTools.DoRespond(chatClient, therapistHistory, $"Client: {clientResponse}", "\r\nTherapist:", maxcharsInALine, nlAfterParanlAfterPara);
    assessmentHistory.Add(new ChatMessage(AI.ChatRole.User, therapyResponse));
    await Task.Delay(miliSecondsDelay);
    clientResponse = await AiTools.DoRespond(chatClient, clientHistory, $"Therapist: {therapyResponse}", "\r\nClient:", maxcharsInALine, nlAfterParanlAfterPara);
    assessmentHistory.Add(new ChatMessage(AI.ChatRole.User, clientResponse));

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
string assessmentRequest = AiTools.GetPrompt("AssessmentRequest.md");

List<ChatMessage> evaluationHistory = SharedTools.MakeDeepCopy(assessmentHistory);

string summary = await AiTools.DoRespond(chatClient, assessmentHistory, $"Epilogue: {assessmentRequest}", "\r\n## Assessment", maxcharsInALine, false);

string evaluationRequest = "{ TherapistName: " + TherapistName + " }\r\n" + AiTools.GetPrompt("TherapistEvaluator.md");
string evaluation = await AiTools.DoRespond(chatClient, evaluationHistory, $"Epilogue: {evaluationRequest}", "\r\n## Evaluation", maxcharsInALine, false);


SharedTools.RecordGrade(evaluation, "Maren.csv");
// WrapText moved to AiTools; Program still had a local reference but uses AiTools.DoRespond which prints wrapped text.
