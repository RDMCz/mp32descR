using System.Collections.Generic;
using ATL;

namespace mp32descR.Model;

/// <summary>
/// Crate to transport result of reading a folder with audio files.
/// </summary>
/// <param name="Info">String with information about reading a folder. How many files of each type were found.</param>
/// <param name="Collection">List of ATL audio file wrappers, from which the tags can be read.</param>
public record FolderAudioFiles(string Info, List<Track> Collection);