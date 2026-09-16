using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ATL;
using mp32descR.Model;

namespace mp32descR.Service;

/// <summary>
/// Load audio files from a given folder and its subfolders.
/// </summary>
public static class AudioFileLoader
{
    /// <param name="folderPath">Path to folder with audio files</param>
    /// <param name="progress">Used to report progress back to the UI</param>
    /// <returns>Text with info about imported files and
    /// Collection of audio files from folder at <c>folderPath</c> and its subfolders</returns>
    public static FolderAudioFiles GetFolderAudioFilesWithProgress(
        string folderPath,
        IProgress<Tuple<int, int>> progress
    )
    {
        HashSet<string> allowedExtensions = [".mp3", ".m4a", ".flac", ".wav"];

        try
        {
            var files = Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories).Order().ToList();

            // If the directory is empty, inform the user in the UI and abort
            if (files.Count == 0) return new FolderAudioFiles("Nothing found", []);

            // To report progress and file stats
            var nTotalFiles = files.Count;
            var nProcessedFiles = 0;
            var nAudioFiles = 0;
            // (equivalent of Python's `allowedExtensionsCount = { k: 0 for k in allowedExtensions }`)
            var allowedExtensionsCount = allowedExtensions.ToDictionary(el => el, _ => 0);
            Dictionary<string, int> otherExtensionsCount = [];

            List<Track> resultCollection = [];

            foreach (var filePath in files)
            {
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                if (allowedExtensions.Contains(extension))
                {
                    resultCollection.Add(new Track(filePath));
                    nAudioFiles++;
                    allowedExtensionsCount[extension]++;
                }
                else
                {
                    if (string.IsNullOrEmpty(extension)) extension = "No extension";
                    otherExtensionsCount[extension] = otherExtensionsCount.GetValueOrDefault(extension) + 1;
                }

                // Update the progress bar after every processed file
                progress.Report(new Tuple<int, int>(++nProcessedFiles, nTotalFiles));
            }

            // Fill the string with info about imported files
            // (total files found, how many for each extension, how many un/supported)
            var nOtherFiles = nTotalFiles - nAudioFiles;

            StringBuilder resultTextBuilder = new();
            resultTextBuilder
                .Append($"Found {nTotalFiles} files:")
                .Append($"\n{nAudioFiles} supported audio files")
                .Append($"\n{nOtherFiles} other")
                .Append("\n\nSupported:");

            foreach (var pair in allowedExtensionsCount)
            {
                resultTextBuilder.Append($"\n{pair.Key.PadRight(5)} × {pair.Value}");
            }

            // ReSharper disable once InvertIf
            if (nOtherFiles > 0)
            {
                resultTextBuilder.Append("\n\nOther:");
                foreach (var pair in otherExtensionsCount)
                {
                    resultTextBuilder.Append($"\n{pair.Key} × {pair.Value}");
                }
            }

            return new FolderAudioFiles(resultTextBuilder.ToString(), resultCollection);
        }
        catch (Exception ex)
        {
            // Abort on error and inform user
            // This can happen e.g. on insufficient rights to browse the directory
            return new FolderAudioFiles(ex.Message, []);
        }
    }
}