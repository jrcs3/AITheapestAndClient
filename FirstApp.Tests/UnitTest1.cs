using Microsoft.Extensions.AI;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

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
        public void ExtractLabel_SingleLine_ReturnsValue()
        {
            string input = "Therapist: Dr. Test\r\nClient: Jane Doe\r\nGrade: A";
            var result = SharedTools.ExtractLabel(input, "Therapist");
            Assert.Equal("Dr. Test", result);
        }

        [Fact]
        public void ExtractLabel_MultiLine_ReturnsMultiLineValue()
        {
            string input = "Intro\r\nTherapist: Dr. Multi\r\nLine continues\r\nClient: Someone";
            var result = SharedTools.ExtractLabel(input, "Therapist");
            Assert.Equal("Dr. Multi", result);
        }

        [Fact]
        public void ExtractLabel_CaseInsensitive_MatchesLabel()
        {
            string input = "therapist: lowercase name\r\nClient: X";
            var result = SharedTools.ExtractLabel(input, "Therapist");
            Assert.Equal("lowercase name", result);
        }

        [Fact]
        public void ExtractLabel_MissingLabel_ReturnsEmpty()
        {
            string input = "No relevant labels here";
            var result = SharedTools.ExtractLabel(input, "Therapist");
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void RecordGrade_WritesCsvWithHeaderAndRow()
        {
            var tmp = Path.Combine($"recordgrade_test_{Guid.NewGuid():N}.csv");
            string fullPath = Path.Combine("c:\\temp", tmp);
            try
            {
                string evaluation = "#title\r\nStuff before\r\nTherapist: Dr. Test\r\nClient: Jane Doe\r\nGrade: A\r\nNotes: Well done.";
                SharedTools.RecordGrade(evaluation, tmp);
                Assert.True(File.Exists(fullPath));
                var lines = File.ReadAllLines(fullPath);
                Assert.True(lines.Length >= 2, "Expected at least header and one data row");
                Assert.Equal("Therapist,Client,Grade,Timestamp", lines[0]);
                string[] columns = lines[1].Split(',');
                Assert.Equal("\"Dr. Test\"", columns[0]);
                Assert.Equal("\"Jane Doe\"", columns[1]);
                Assert.Equal("\"A\"", columns[2]);
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(fullPath);
            }
        }
    }
}
