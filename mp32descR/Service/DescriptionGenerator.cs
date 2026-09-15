using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ATL;

namespace mp32descR.Service;

public static partial class DescriptionGenerator
{
    /// <param name="audioFiles">Collection of audio files to generate description from</param>
    /// <param name="template">Each audio file is converted to text using this template</param>
    /// <param name="isSubfolderPrintEnabled">Whether to include names of subfolders in the result</param>
    /// <param name="isTimestampPrefixEnabled">Whether to include YouTube-like timestamps in the result</param>
    /// <returns>String with description for tracks in <c>audioFiles</c> according to <c>template</c></returns>
    public static string GetAudioFilesDescription(
        List<Track> audioFiles,
        string template,
        bool isSubfolderPrintEnabled,
        bool isTimestampPrefixEnabled
    )
    {
        StringBuilder result = new();

        // Convert template once to a more suitable format that can be used in String.format
        var formatStringTemplate = TemplateToFormatString(template);

        // Used to remember last printed subfolder path, so we print only the unique ones (isSubfolderPrintEnabled)
        var lastFolderPath = "";

        // Used to generate timestamps before each track if desired (isTimestampPrefixEnabled)
        TimestampCounter timestampCounter = new();

        foreach (var audioFile in audioFiles)
        {
            if (isSubfolderPrintEnabled)
            {
                var thisFolderPath = RemoveLastItemFromPathAndUseFwdSlash(audioFile.Path);
                if (thisFolderPath != lastFolderPath)
                {
                    // Subfolder path has changed, print it to the output
                    // (newline before path except the first one)
                    if (!string.IsNullOrEmpty(lastFolderPath)) result.AppendLine();
                    result.Append($"{thisFolderPath}\n\n");
                    lastFolderPath = thisFolderPath;
                }
            }

            if (isTimestampPrefixEnabled)
            {
                result.Append($"{timestampCounter} ");
                timestampCounter.IncrementMs(audioFile.DurationMs);
            }

            result.Append(ApplyFormatString(audioFile, formatStringTemplate)).AppendLine();
        }

        return result.ToString();
    }

    // .: Replace {title}, {artist}, ... in `template` by {0}, {1}, ... :.
    // .:===============================================================:.

    [GeneratedRegex("{([^{}]*)}")]
    private static partial Regex TextInCurlyBracesRegex();

    private static string TemplateToFormatString(string userTemplate)
    {
        return TextInCurlyBracesRegex().Replace(
            userTemplate,
            m => TemplateFieldToFormatStringField(m.Groups[1].Value)
        );
    }

    private static string TemplateFieldToFormatStringField(string userField) => userField switch
    {
        // Numbers are in order of arguments in `ApplyFormatString`'s String.Format
        "title" => "{0}",
        "artist" => "{1}",
        "album" => "{2}",
        "year" => "{3}",
        "trackNumber" => "{4}",
        "discNumber" => "{5}",
        "genre" => "{6}",
        "comment" => "{7}",
        "duration" => "{8}",
        "bitrate" => "{9}",
        _ => $"{{{{{userField}}}}}", // Put in second pair of braces so it does not crash the String.Format
    };

    // .: Use converted template to get appropriate string for a track :.
    // .:==============================================================:.

    private static string ApplyFormatString(Track t, string stringFormatTemplate)
    {
        return string.Format(
            stringFormatTemplate,
            t.Title, t.Artist, t.Album, t.Year, t.TrackNumber, t.DiscNumber, t.Genre, t.Comment, t.Duration, t.Bitrate
        );
    }

    // .: Helper methods :.
    // .:================:.

    private static string RemoveLastItemFromPathAndUseFwdSlash(string input)
    {
        return string.Join("/", input.Split(Path.DirectorySeparatorChar)[..^1]);
    }
}