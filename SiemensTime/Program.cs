using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

string[] testCasesShouldWork =
{
    "t#1d1h1m1s1ms",
    "t#1d_1h_1m_1s_1ms",
    "t#1d_1h_1m_1s_1ms_"
};

string[] testCasesShouldNotWork =
{
    "t#1d_1d",
    "t#1h_1d",
    "t#1m_1d",
    "t#1s_1d",
    "t#1ms_1d",
};

string pattern =
    @"(T#|TIME#)" + // "Non-capturing group (?:...)" means it will not be included in the match groups
    @"(?=\d+(?:d|h|m(?!s)|s|ms))" + // "Positive look-ahead assertion (?=...)": We say the T# must be followed by this
    @"(?:(\d+)d_?)?" + // "Optional non-capturing group (?:...)? with one or more digits followed by an optional underscore
    @"(?:(\d+)h_?)?" +    
    @"(?:(\d+)(?:m(?!s))_?)?" + // The m must not be followed by s for this group to pass    
    @"(?:(\d+)s_?)?" +    
    @"(?:(\d+)(?:ms)_?)?" +
    @"(?!_?\d+(?:d|h|m(?!s)|s|ms))"; // Negative look-ahead assertion (?!...): The milliseconds must not be followed by a bigger unit or another ms or just another number

Console.WriteLine("Should work:");
foreach (string testCase in testCasesShouldWork)
{
    RegexTest.CheckString(testCase);
}

Console.WriteLine("Should not work:");
foreach (string testCase in testCasesShouldNotWork)
{
    RegexTest.CheckString(testCase);
}


while (true)
{
    Console.Write("Enter siemens time: ");
    string input = Console.ReadLine() ?? "";

    var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
    RegexTest.CheckString(input);

}


class RegexTest
{
    private static string timeStartPattern =
        @"^(T#|TIME#)"; // Find T# or #TIME in the start of the input

    private static string timeContentPattern =        
        @"(?=^\d+(?:d|h|m(?!s)|s|ms))" + // "Positive look-ahead assertion (?=...)": We must encounter one of these
        @"(?:(\d+)d_?)?" + // "Optional non-capturing group (?:...)? with one or more digits followed by an optional underscore
        @"(?:(\d+)h_?)?" +
        @"(?:(\d+)(?:m(?!s))_?)?" + // The m must not be followed by s for this group to pass    
        @"(?:(\d+)s_?)?" +
        @"(?:(\d+)(?:ms)_?)?" +
        @"(?!_?\d+(?:d|h|m(?!s)|s|ms))"; // Negative look-ahead assertion (?!...): The milliseconds must not be followed by a bigger unit or another ms or just another number


    public static void CheckString(string input)
    {

        var startMatch = Regex.Match(input, timeStartPattern, RegexOptions.IgnoreCase);

        if (!startMatch.Success)
        {
            Console.WriteLine($"'{input}': T# or TIME# syntax not found");
            return;
        }

        int index = 0;
        index += startMatch.Length;

        string content = input.Substring(index);       

        var match = Regex.Match(content, timeContentPattern, RegexOptions.IgnoreCase);

        int groupNumber = 0;
        foreach ( var group in match.Groups.Values)
        {
            if (group.Success)
            {
                Console.WriteLine($"Group matched: {groupNumber}");
            }
            groupNumber++;
        }


        if (match.Success)
        {
            int days = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
            int hours = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
            int minutes = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
            int seconds = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;
            int milliseconds = match.Groups[5].Success ? int.Parse(match.Groups[5].Value) : 0;

            try
            {
                checked
                {
                    int totalMilliseconds = (days * 24 * 60 * 60 * 1000) +
                                            (hours * 60 * 60 * 1000) +
                                            (minutes * 60 * 1000) +
                                            (seconds * 1000) +
                                            milliseconds;
                    Console.WriteLine($"'{input}' total duration in milliseconds: {totalMilliseconds}");
                }
            }
            catch (OverflowException)
            {
                Console.WriteLine("The total time duration exceeds the limit of a 32-bit integer.");
            }
        }
        else
        {
            Console.WriteLine($"'{input}' the input string does not match the expected format.");
        }
    }
}