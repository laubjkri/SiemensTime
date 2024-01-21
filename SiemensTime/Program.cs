using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

string[] testCasesShouldWork =
{
    "t#1d2h3m4s5ms",
    "TIME#1d2h3m4s5ms",
    "TIME#1d_2h3m4s_5ms",
    "T#1d2h3m4s5ms",
    "T#1D5MS",
    "t#1d2h3m4s5ms_",
    "t#1d_2h_3m_4s_5ms",
    "t#5ms",
    "t#5ms + \"something else\"",
    "TIME#-1d_2h3m4s_5ms",
    "t#-1d-fgdhj",
    "TIME#-45h",
    "TIME#-45m",
    "TIME#-45s",
    "TIME#-45ms",
    "TIME#-45m_345s",
};

string[] testCasesShouldNotWork =
{
    "t#",
    "time#",
    "time#invalidContent",
    "t#1d_1d",
    "t#1h_1d + + \"something else\"",
    "t#1m_1d",
    "t#1s_1d",
    "t#1ms_1d",
    "t#1f",
    "t#25d1h + \"something else\"", // Overflow
    "t#1d-1h",
    "t#-1d-1h"
};



Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("==============================Should work=============================");
foreach (string testCase in testCasesShouldWork)
{
    RegexTest.CheckString(testCase);
}

Console.ForegroundColor = ConsoleColor.Red;
Console.WriteLine("============================Should not work===========================");
foreach (string testCase in testCasesShouldNotWork)
{
    RegexTest.CheckString(testCase);
}

while (true)
{
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.Write("Enter siemens time: ");
    string input = Console.ReadLine() ?? "";
    
    RegexTest.CheckString(input);
}

class RegexTest
{
    private static string startPattern =
        @"^(T#|TIME#)"; // Find T# or #TIME in the start of the input

    private static string contentAvailabePattern =
        @"(^-?\d+(d|h|m(?!s)|s|ms)_?)" + // Input must start with this
        @"(-?\d+(d|h|m(?!s)|s|ms)_?)*";  // Followed by zero or more of these (we use this later to check where the time input ends)
        // '-' is allowed in more than the first value above because we want to consume all wrong syntax which is intended for time

    private static string contentOrderPattern =
        @"(?=^-?\d+(?:d|h|m(?!s)|s|ms))" + // "Positive look-ahead assertion (?=...)": We must encounter one of these. Necessary since all groups below are optional.
        @"(-)?" + // The negative symbol must only be before the first value
        @"(?:(\d+)d_?)?" + // One or more digits followed by an optional underscore
        @"(?:(\d+)h_?)?" +
        @"(?:(\d+)(?:m(?!s))_?)?" + // The m must not be followed by s for this group to pass    
        @"(?:(\d+)s_?)?" +
        @"(?:(\d+)(?:ms)_?)?" + // (?:...) non-capturing group
        @"(?!_?-?\d+(?:d|h|m(?!s)|s|ms))"; // Negative look-ahead assertion (?!...): The expression must not be followed by any more entries


    public static void CheckString(string input)
    {
        int index = 0;

        // Get the time identifier
        var startMatch = Regex.Match(input, startPattern, RegexOptions.IgnoreCase);
        if (!startMatch.Success)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Input '{input}': T# or TIME# syntax not found");
            return; // No token or error message created return for further evaluation
        }        
        index += startMatch.Length;

        string content = input.Substring(index);
        var contentAvailableMatch = Regex.Match(content, contentAvailabePattern, RegexOptions.IgnoreCase);
        if (!contentAvailableMatch.Success)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Input '{input}': Syntax error: Time information missing at pos. {index}: '{startMatch.Value + SubstringSafe(input, index, index + 5)}...')");
            return; // And add token with error information
        }

        index += contentAvailableMatch.Length; // We consume any pattern that matches the time syntax

        var contentOrderMatch = Regex.Match(content, contentOrderPattern, RegexOptions.IgnoreCase);
        if (!contentOrderMatch.Success)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Input '{input}' => Parsed: {startMatch.Value + contentAvailableMatch.Value}: Syntax error: The time format is invalid");
            return; // And add token with error information
        }

        // All syntax good
        bool isNegative  = contentOrderMatch.Groups[1].Success;
        int days         = contentOrderMatch.Groups[2].Success ? int.Parse(contentOrderMatch.Groups[2].Value) : 0;
        int hours        = contentOrderMatch.Groups[3].Success ? int.Parse(contentOrderMatch.Groups[3].Value) : 0;
        int minutes      = contentOrderMatch.Groups[4].Success ? int.Parse(contentOrderMatch.Groups[4].Value) : 0;
        int seconds      = contentOrderMatch.Groups[5].Success ? int.Parse(contentOrderMatch.Groups[5].Value) : 0;
        int milliseconds = contentOrderMatch.Groups[6].Success ? int.Parse(contentOrderMatch.Groups[6].Value) : 0;

        

        try
        {
            checked
            {
                int totalMilliseconds = (days * 24 * 60 * 60 * 1000) +
                                        (hours * 60 * 60 * 1000) +
                                        (minutes * 60 * 1000) +
                                        (seconds * 1000) +
                                        milliseconds;

                if (isNegative)
                {
                    totalMilliseconds = totalMilliseconds * -1;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Input: '{input}' => Parsed: '{startMatch.Value + contentOrderMatch.Value}': Success! Total duration in milliseconds: {totalMilliseconds}");
                
            }
        }
        catch (OverflowException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Input: '{input}' => Parsed: '{startMatch.Value + contentOrderMatch.Value}': Parsed, but overflow!");
        }
    }

    private static string SubstringSafe(string input, int indexBegin, int indexEnd)
    {
        if (input == null) return "";

        int stringEndIndex = input.Length - 1;
        if (stringEndIndex < 0)
            stringEndIndex = 0;

        if (indexBegin > stringEndIndex)
            return "";

        if (indexBegin < 0)
            indexBegin = 0;

        if (indexEnd > stringEndIndex)
            indexEnd = stringEndIndex;
        if (indexEnd < 0)
            return "";

        int length = indexEnd - indexBegin + 1; // +1 to include the endindex
        int maxLength = stringEndIndex - indexBegin + 1;

        if (length > maxLength)
            length = maxLength;

        if (length < 0)
            length = 0;

        // For debugging
        string substring = input.Substring(indexBegin, length);
        return substring;
    }
}