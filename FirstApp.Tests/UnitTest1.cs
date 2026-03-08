using Microsoft.Extensions.AI;
using System;
using System.IO;

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

        [Fact]
        public void RecordGrade_WritesCsvWithHeaderAndRow()
        {
            var tmp = Path.Combine(Path.GetTempPath(), $"recordgrade_test_{Guid.NewGuid():N}.csv");
            try
            {
                string evaluation = "Therapist: Dr. Test\r\nClient: Jane Doe\r\nGrade: A\r\nNotes: Well done.";
                SharedTools.RecordGrade(evaluation, tmp);
                Assert.True(File.Exists(tmp));
                var lines = File.ReadAllLines(tmp);
                Assert.True(lines.Length >= 2, "Expected at least header and one data row");
                Assert.Equal("Therapist,Client,Grade,Timestamp", lines[0]);
                Assert.Contains("Dr. Test", lines[1]);
                Assert.Contains("Jane Doe", lines[1]);
                Assert.Contains("A", lines[1]);
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        }
    }
}
