using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ATL;
using mp32descR.Model;

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


    private static string TemplateFieldToFormatStringField(string userField)
    {
        return Enum.TryParse<TemplateField>(userField, ignoreCase: true, out var templateField)
            ? $"{{{(int)templateField}}}" // e.g. {0}
            : $"{{{{{userField}}}}}"; // Put in second pair of braces so it does not crash the String.Format
    }

    // .: Use converted template to get appropriate string for a track :.
    // .:==============================================================:.

    private static readonly Dictionary<TemplateField, Func<Track, object?>> TemplateFieldToTrackGetter = new()
    {
        [TemplateField.Album] = t => t.Album,
        [TemplateField.Artist] = t => t.Artist,
        [TemplateField.Bitrate] = t => t.Bitrate,
        [TemplateField.Comment] = t => t.Comment,
        [TemplateField.DiscNumber] = t => t.DiscNumber,
        [TemplateField.Duration] = t => t.Duration,
        [TemplateField.Genre] = t => t.Genre,
        [TemplateField.Title] = t => t.Title,
        [TemplateField.TrackNumber] = t => t.TrackNumber,
        [TemplateField.Year] = t => t.Year,
    };

    private static string ApplyFormatString(Track track, string stringFormatTemplate)
    {
        var values = Enum.GetValues<TemplateField>().Select(templateField =>
        {
            if (TemplateFieldToTrackGetter.TryGetValue(templateField, out var getter))
            {
                return getter(track);
            }

            return "UndefinedTemplateFieldGetterError";
        }).ToArray();

        return string.Format(stringFormatTemplate, values);
    }

    // .: Helper methods :.
    // .:================:.

    private static string RemoveLastItemFromPathAndUseFwdSlash(string input)
    {
        return string.Join("/", input.Split(Path.DirectorySeparatorChar)[..^1]);
    }
}