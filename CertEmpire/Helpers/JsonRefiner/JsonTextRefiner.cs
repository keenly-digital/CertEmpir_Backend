using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class JsonTextRefiner
{
    /// <summary>
    /// Parses the input JSON, refines every string leaf according to the rules,
    /// and returns the resulting JSON (indented).
    /// </summary>
    public static string RefineJson(string json)
    {
        var root = JToken.Parse(json);
        WalkAndRefine(root);
        return root.ToString(Formatting.Indented);
    }

    // Recursively traverse objects/arrays and refine each string leaf
    private static void WalkAndRefine(JToken token)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                foreach (var prop in token.Children<JProperty>())
                    WalkAndRefine(prop.Value);
                break;
            case JTokenType.Array:
                foreach (var item in token.Children())
                    WalkAndRefine(item);
                break;
            case JTokenType.String:
                var original = token.Value<string>();
                ((JValue)token).Value = RefineText(original);
                break;
        }
    }

    // Collapse only “in-sentence” newlines into spaces, preserve the rest
    private static string RefineText(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // 1) Normalize all newlines to '\n'
        input = input.Replace("\r\n", "\n").Replace("\r", "\n");

        // 2) GENERIC PRE-PASS: collapse any newline between alphanumeric chars
        input = Regex.Replace(input, @"(?<=[A-Za-z0-9])\n(?=[A-Za-z0-9])", " ");

        var lines = input.Split('\n');
        var sb = new StringBuilder();

        for (int i = 0; i < lines.Length; i++)
        {
            string raw = lines[i];
            string trimmed = raw.Trim();

            bool keepNL = true;
            if (i < lines.Length - 1)
            {
                string nextRaw = lines[i + 1];
                string nextTrimmed = nextRaw.Trim();

                // a) Ends with sentence punctuation?
                bool endsSentence = Regex.IsMatch(trimmed, @"[\.!\?:]\s*$");

                // b) Next line blank?
                bool nextBlank = string.IsNullOrWhiteSpace(nextRaw);

                // c) Next line is image or URL?
                bool nextIsImageOrUrl = nextTrimmed.StartsWith("/static/images/", StringComparison.OrdinalIgnoreCase)
                                     || Regex.IsMatch(nextTrimmed, @"^https?://", RegexOptions.IgnoreCase);

                // d) Short 1–4 char lines
                bool thisShort = trimmed.Length >= 1 && trimmed.Length <= 4;
                bool nextShort = nextTrimmed.Length >= 1 && nextTrimmed.Length <= 4;

                // e) Heading pattern: *exactly two* Title-cased words
                bool thisHeading = Regex.IsMatch(trimmed, @"^[A-Z][a-z]+ [A-Z][a-z]+$");
                bool nextHeading = Regex.IsMatch(nextTrimmed, @"^[A-Z][a-z]+ [A-Z][a-z]+$");

                // If none of the “keep” conditions apply, collapse into a space
                if (!endsSentence
                 && !nextBlank
                 && !nextIsImageOrUrl
                 && !thisShort
                 && !nextShort
                 && !thisHeading
                 && !nextHeading)
                {
                    keepNL = false;
                }
            }

            sb.Append(raw);
            if (i < lines.Length - 1)
                sb.Append(keepNL ? "\n" : " ");
        }

        var result = sb.ToString();

        // 3) Collapse 3+ blank lines to exactly 2
        result = Regex.Replace(result, @"\n{3,}", "\n\n");

        // 4) Trim trailing spaces on each line
        return string.Join("\n",
            result.Split('\n').Select(line => line.TrimEnd()));
    }
}