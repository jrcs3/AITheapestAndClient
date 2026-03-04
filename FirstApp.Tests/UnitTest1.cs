using Microsoft.Extensions.AI;

namespace FirstApp.Tests
{
    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            string? token = AiTools.GetToken();
            Assert.NotNull(token);

            //string prompt = AiTools.GetPrompt("CharacterDesigner.md");
            //Assert.NotNull(prompt);

            IChatClient chatClient = AiTools.GetChatClient(token);
            Assert.NotNull(chatClient);

            string dosier = await AiTools.MakeCharacter(chatClient, AiTools.GetPrompt("CharacterDesigner.md"));
            Assert.NotNull(dosier);
        }
    }
}
