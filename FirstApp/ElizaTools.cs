using Azure;
using ELIZA.NET;
using FirstApp;
using Microsoft.Extensions.AI;

public class ElizaTools
{
    private ELIZALib _elizaLib;
    private const string scriptFile = "DOCTOR\\DOCTOR.json";
    public ElizaTools()
    {
        _elizaLib = new ELIZALib(File.ReadAllText(scriptFile));
    }
    public string DoRespond(IChatClient chatClient, List<ChatMessage> history, string inResponse, string displayTitle, int maxcharsInALine = 80, bool nlAfterParanlAfterPara = true)
    {
        string response = _elizaLib.GetResponse(inResponse);
        Console.WriteLine("\r\n" + SharedTools.WrapText($"{displayTitle}{response}", maxcharsInALine, nlAfterParanlAfterPara));
        return response;
    }
    public string Start(string displayTitle = "\r\nTherapist: ", int maxcharsInALine = 80, bool nlAfterParanlAfterPara = true)
    {
        string response = _elizaLib.Session.GetGreeting();
        Console.WriteLine("\r\n" + SharedTools.WrapText($"{displayTitle}{response}", maxcharsInALine, nlAfterParanlAfterPara));
        return response;

    }
    public string Stop(string displayTitle = "\r\n\r\nTherapist: ", int maxcharsInALine = 80, bool nlAfterParanlAfterPara = true)
    {
        string response = _elizaLib.Session.GetGoodbye();
        Console.WriteLine("\r\n" + SharedTools.WrapText($"{displayTitle}{response}", maxcharsInALine, nlAfterParanlAfterPara));
        return response;
    }

}