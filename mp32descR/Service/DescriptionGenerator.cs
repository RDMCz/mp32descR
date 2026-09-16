using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ATL;
using mp32descR.Model;

namespace mp32descR.Service;

/// <summary>
/// Generate description for given audio files.
/// </summary>
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

    /// <summary>
    /// Converts template string from the GUI to string that can be used in string.Format.
    /// </summary>
    /// <param name="userTemplate">Template string from user, that contains {Title}, {Artist} etc.</param>
    /// <returns><c>userTemplate</c> with {Title}, {Artist} etc. converted to {0}, {1} etc.</returns>
    private static string TemplateToFormatString(string userTemplate)
    {
        return TextInCurlyBracesRegex().Replace(
            userTemplate,
            m => TemplateFieldToFormatStringField(m.Groups[1].Value)
        );
    }

    /// <summary>
    /// Regex used in <c>TemplateToFormatString</c> to replace {Title}, {Artist} etc. by {0}, {1} etc.
    /// so it can be used in String.Format.
    /// </summary>
    [GeneratedRegex("{([^{}]*)}")]
    private static partial Regex TextInCurlyBracesRegex();

    /// <param name="userField">One field from the template string, like "Title" or "Artist"</param>
    /// <returns>Substring like "{0}", that will be replaced in string.Format</returns>
    private static string TemplateFieldToFormatStringField(string userField)
    {
        return Enum.TryParse<TemplateField>(userField, ignoreCase: true, out var templateField)
            ? $"{{{(int)templateField}}}" // e.g. {0}
            : $"{{{userField}}}"; // Do not modify unknown field
    }

    /// <summary>
    /// Converts <c>TemplateField</c> to function that gets appropriate attribute from ATL's <c>Track</c>.
    /// </summary>
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

    /// <summary>
    /// Creates final string that will be used in the GUI output
    /// for <c>track</c> according to <c>stringFormatTemplate</c>.
    /// </summary>
    private static string ApplyFormatString(Track track, string stringFormatTemplate)
    {
        // Create array of values that will be used to replace {0}, {1} etc. for current Track
        var values = Enum.GetValues<TemplateField>().Select(templateField =>
        {
            if (TemplateFieldToTrackGetter.TryGetValue(templateField, out var getter))
            {
                return getter(track);
            }

            return "UndefinedTemplateFieldGetterError";
        }).ToArray();

        try
        {
            return string.Format(stringFormatTemplate, values);
        }
        catch (Exception ex)
        {
            return $"{ex.Message}\n\n(Use double curly braces to escape them: {{{{something}}}} → {{something}}).\n";
        }
    }

    /// <summary>
    /// Used to get a path string for given file's (parent) folder. Always uses forward slashes for consistency.
    /// </summary>
    private static string RemoveLastItemFromPathAndUseFwdSlash(string input)
    {
        return string.Join("/", input.Split(Path.DirectorySeparatorChar)[..^1]);
    }
}