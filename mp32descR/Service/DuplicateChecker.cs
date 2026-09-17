using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using ATL;

namespace mp32descR.Service;

/// <summary>
/// Check if there are multiple tracks with the same title for given audio files.
/// </summary>
public static partial class DuplicateChecker
{
    /// <param name="audioFiles">Collection of audio files to check for duplicates</param>
    /// <param name="doIgnoreBracketsInfo">Ignore text in parentheses? Used for feat./remix/live/...</param>
    /// <returns>String with info about files from <c>audioFiles</c>, that don't have unique title</returns>
    public static string GetDuplicatesInfo(List<Track> audioFiles, bool doIgnoreBracketsInfo)
    {
        Dictionary<string, List<string>> titleToTrackPaths = []; // Paths will be shown in the output...
        HashSet<string> duplicateKeys = []; // ...only paths to those files, that have the same track title

        foreach (var audioFile in audioFiles)
        {
            var title = NormalizeTrackTitle(audioFile.Title, doIgnoreBracketsInfo);
            var path = audioFile.Path;

            if (titleToTrackPaths.TryGetValue(title, out var value))
            {
                value.Add(path);
                duplicateKeys.Add(title);
            }
            else
            {
                titleToTrackPaths.Add(title, [path]);
            }
        }

        var nDupeTracks = duplicateKeys.Count;

        if (nDupeTracks == 0) return "No duplicates found.";

        StringBuilder result = new($"Found {nDupeTracks} titles that are in multiple tracks.\n");

        foreach (var key in duplicateKeys)
        {
            result.Append($"\n{key}:\n");

            foreach (var path in titleToTrackPaths[key])
            {
                result.Append($"\n{path}");
            }

            result.AppendLine();
        }

        return result.ToString();
    }

    /// <summary>
    /// Lowers, trims, and optionally removes text in parentheses in the input string.
    /// </summary>
    /// <param name="title">Input string (track title)</param>
    /// <param name="doIgnoreBracketsInfo">Remove text in parentheses? Used for feat./remix/live/...</param>
    /// <returns>New string</returns>
    private static string NormalizeTrackTitle(string title, bool doIgnoreBracketsInfo)
    {
        if (string.IsNullOrEmpty(title))
        {
            return title;
        }

        if (doIgnoreBracketsInfo)
        {
            title = TextInBracketsRegex().Replace(title, "");
        }

        return title.ToLowerInvariant().Trim();
    }

    /// <summary>
    /// Detects text in brackets and whitespace before that.
    /// Whitespace check is there so when you apply remove to "a (b) c (d)", you'll get "a c" instead of "a  c ".
    /// This is simple but insufficient for nested brackets.
    /// </summary>
    [GeneratedRegex(@"\s*\([^)]*\)")]
    private static partial Regex TextInBracketsRegex();
}