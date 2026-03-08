using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using AI = Microsoft.Extensions.AI;

namespace FirstApp;

public static class SharedTools
{
    public static string WrapText(string text, int maxWidth = 80, bool nlAfterPara = true)
    {
        if (string.IsNullOrWhiteSpace(text) || maxWidth < 10)
            return text ?? string.Empty;

        // Preserve paragraph breaks: split on empty lines
        string[] paragraphs = Regex.Split(text.Trim(), "\n");
        StringBuilder outSb = new StringBuilder();
        foreach (string para1 in paragraphs)
        {
            string para = para1.Replace("\r", "").Replace("\n", ""); // Normalize line breaks
            if (string.IsNullOrWhiteSpace(para))
            {
                continue;
            }
            string[] words = Regex.Split(para.Trim(), "\\s+").Where(w => w.Length > 0).ToArray();
            StringBuilder lineSb = new StringBuilder();
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                if (lineSb.Length == 0)
                {
                    lineSb.Append(word);
                }
                else if (lineSb.Length + 1 + word.Length <= maxWidth)
                {
                    lineSb.Append(' ').Append(word);
                }
                else
                {
                    outSb.AppendLine(lineSb.ToString());
                    lineSb.Clear();
                    lineSb.Append(word);
                }
            }
            if (lineSb.Length > 0)
            {
                outSb.AppendLine(lineSb.ToString());
            }
            if (nlAfterPara)
            {
                outSb.AppendLine(); // paragraph separator
            }
            //
        }
        return outSb.ToString().TrimEnd();
    }
    public static List<ChatMessage> MakeDeepCopy(List<ChatMessage> assessmentHistory)
    {
        // Create a deep copy of assessmentHistory for evaluation use. We use
        // reflection to read the message text property (which may be named
        // "Content" or "Text" depending on the SDK) so the copy compiles
        // against different ChatMessage implementations.
        return assessmentHistory.Select(msg =>
        {
            try
            {
                Type t = msg.GetType();
                PropertyInfo? roleProp = t.GetProperty("Role");
                PropertyInfo? contentProp = t.GetProperty("Content") ?? t.GetProperty("Text") ?? t.GetProperty("Message");
                ChatRole role = roleProp != null ? (AI.ChatRole)roleProp.GetValue(msg) : AI.ChatRole.Assistant;
                string? content = contentProp != null ? contentProp.GetValue(msg) as string : msg.ToString();
                return new ChatMessage(role, content ?? string.Empty);
            }
            catch
            {
                return new ChatMessage(AI.ChatRole.Assistant, msg.ToString());
            }
        }).ToList();
    }

    public static void RecordGrade(string evaluation, string fileName)
    {
        string fullFileName = Path.Combine("c:\\temp", fileName);
        if (string.IsNullOrWhiteSpace(fullFileName))
            throw new ArgumentException("fileName must be provided", nameof(fullFileName));

        string therapist = ExtractLabel(evaluation, "Therapist");
        string client = ExtractLabel(evaluation, "Client");
        string grade = ExtractLabel(evaluation, "Grade");

        string header = "Therapist,Client,Grade,Timestamp";
        string timestamp = DateTime.UtcNow.ToString("o");
        string row = string.Join(",", new[] { CsvEscape(therapist), CsvEscape(client), CsvEscape(grade), CsvEscape(timestamp) });

        // Ensure directory exists
        try
        {
            string? dir = Path.GetDirectoryName(fullFileName);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            bool writeHeader = !File.Exists(fullFileName);
            using var sw = new StreamWriter(fullFileName, append: true, encoding: Encoding.UTF8);
            if (writeHeader)
            {
                sw.WriteLine(header);
            }
            sw.WriteLine(row);
        }
        catch (Exception ex)
        {
            // Log to console but do not throw in library method
            Console.WriteLine($"Failed to record grade to '{fullFileName}': {ex.Message}");
        }
    }

    // Prepare CSV-safe values (quote and escape quotes)
    public static string CsvEscape(string s)
    {
        if (s == null) return string.Empty;
        string escaped = s.Replace("\"", "\"\"");
        return '"' + escaped + '"';
    }


    // Extract the labelled sections; allow multi-line values until the next label
    public static string ExtractLabel(string input, string label)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        // Match the label followed by ':' and capture up to the end of the line only.
        // Do not allow the value to span multiple lines.
        string pattern = $@"{Regex.Escape(label)}\s*:\s*(.*?)(?:\r?\n|$)";
        Match m = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : string.Empty;
    }

}
