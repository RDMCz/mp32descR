using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ATL;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using mp32descR.Model;
using mp32descR.Service;

namespace mp32descR.View;

public partial class MainWindow : Window
{
    // Used to store all tracks from currently selected directory (and its subdirectories)
    private List<Track> _audioFiles = [];

    public MainWindow()
    {
        InitializeComponent();

        // = Initialize state of the app =
        ButtonRevealInExplorer.IsEnabled = false; // There's no path on app's start
        ButtonCopy.IsEnabled = false; // Nothing to copy on app's start
        ButtonSave.IsEnabled = false; // Nothing to save on app's start
        ProgressBar.Value = 0;
        ProgressBar.ShowProgressText = false; // Show progress only when loading something
        GroupBoxDuplicateChecker.IsEnabled = false;

        // Default template
        TextBoxTemplate.Text = $"{{{TemplateField.Artist}}}" +
                               $" – {{{TemplateField.Year}}}" +
                               $" – {{{TemplateField.TrackNumber}}}" +
                               $" – {{{TemplateField.Title}}}";

        // = Set the TemplateHelper button context menu =
        var contextMenuItems = Enum.GetValues<TemplateField>()
            .Select(el => new MenuItem { Header = el, Tag = el })
            .ToList();

        foreach (var contextMenuItem in contextMenuItems)
        {
            contextMenuItem.Click += ContextMenuTemplateHelperItemClicked;
        }

        ContextMenuTemplateHelper.ItemsSource = contextMenuItems;
    }

    /// <summary>
    /// Opens a file explorer with path present in <c>TextBoxPath</c>.
    /// </summary>
    private void ButtonRevealInExplorerClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel is null) return;

        var pathToOpen = TextBoxPath.Text;
        if (string.IsNullOrEmpty(pathToOpen)) return;

        var launcher = topLevel.Launcher;
        launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(pathToOpen));
        // Not awaiting async because no need for result?
    }

    /// <summary>
    /// Shows dialog to select a folder. Fills <c>_audioFiles</c> with tracks from selected folder
    /// and shows info in <c>TextBlockFilesInfo</c>.
    /// </summary>
    private async void ButtonChangeFolderClicked(object? sender, RoutedEventArgs e)
    {
        try // Exceptions in async void will not propagate to a caller, hence the Pokémon catch 
        {
            var topLevel = GetTopLevel(this);
            if (topLevel is null) return;

            // Make user select a folder and abort if no folder selected
            var storage = topLevel.StorageProvider;
            var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false });
            if (folders.Count <= 0) return;

            // Importing files may take a while, so disable GUI and reset ProgressBar
            ButtonRevealInExplorer.IsEnabled = false;
            ButtonChangeDirectory.IsEnabled = false;
            ButtonSave.IsEnabled = false;
            ButtonCopy.IsEnabled = false;
            ButtonGenerate.IsEnabled = false;
            TextBoxResult.Text = ""; // User probably doesn't need the old result anymore
            ProgressBar.Value = 0;
            ProgressBar.ShowProgressText = true;
            GroupBoxDuplicateChecker.IsEnabled = false;

            // Get the path from dialog and start importing files
            var folderPath = folders[0].Path.LocalPath;
            TextBoxPath.Text = folderPath;
            // Run async and update ProgressBar
            Progress<Tuple<int, int>> progress = new(tup =>
            {
                ProgressBar.Value = tup.Item1;
                ProgressBar.Maximum = tup.Item2;
            });

            var result = await Task.Run(() => AudioFileLoader.GetFolderAudioFilesWithProgress(folderPath, progress));
            _audioFiles = result.Collection;
            TextBlockFilesInfo.Text = result.Info;

            // Import done. Enable previously disabled buttons except for Copy and Save, these will be enabled
            // when user generates something (no need to copy/save empty string) and reset progress bar.
            ButtonRevealInExplorer.IsEnabled = true;
            ButtonChangeDirectory.IsEnabled = true;
            ButtonGenerate.IsEnabled = true;
            ProgressBar.Value = 0;
            ProgressBar.ShowProgressText = false;
            GroupBoxDuplicateChecker.IsEnabled = true;
        }
        catch (Exception ex)
        {
            TextBlockFilesInfo.Text = ex.Message;
        }
    }

    /// <summary>
    /// Fills <c>TextBoxResult</c> with description for files in <c>_audioFiles</c> that is determined by
    /// user pattern in <c>TextBoxTemplate</c>. Enables copy and save buttons.
    /// </summary>
    private void ButtonGenerateClicked(object? sender, RoutedEventArgs e)
    {
        if (_audioFiles.Count == 0)
        {
            // No need to run through empty collection (happens when directory has no supported files in it)
            TextBoxResult.Text = "Choose a directory with some audio files first.";
            return;
        }

        TextBoxResult.Text = DescriptionGenerator.GetAudioFilesDescription(
            _audioFiles,
            TextBoxTemplate.Text ?? "",
            CheckBoxSubdirectories.IsChecked ?? false,
            CheckBoxTimestamps.IsChecked ?? false
        );

        ButtonSave.IsEnabled = true;
        ButtonCopy.IsEnabled = true;
    }

    /// <summary>
    /// Inserts content of <c>TextBoxResult</c> into user's clipboard
    /// </summary>
    private void ButtonCopyClicked(object? sender, RoutedEventArgs e)
    {
        var clipboard = GetTopLevel(this)?.Clipboard;
        clipboard?.SetTextAsync(TextBoxResult.Text);
        // Not awaiting async because no need for result?
    }

    /// <summary>
    /// Opens a dialog where user can save <c>TextBoxResult</c> content into a .txt file
    /// </summary>
    private async void ButtonSaveClicked(object? sender, RoutedEventArgs e)
    {
        try // Pokémon catch for async void
        {
            var topLevel = GetTopLevel(this);
            if (topLevel is null) return;

            var storage = topLevel.StorageProvider;
            var suggestedFolder = await storage.TryGetFolderFromPathAsync(TextBoxPath.Text ?? "");

            var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                DefaultExtension = ".txt",
                ShowOverwritePrompt = true,
                SuggestedFileName = "description.txt",
                SuggestedStartLocation = suggestedFolder,
            });

            if (file is not null)
            {
                await File.WriteAllTextAsync(file.Path.LocalPath, TextBoxResult.Text);
            }
        }
        catch (Exception ex)
        {
            TextBlockFilesInfo.Text = ex.Message;
        }
    }

    /// <summary>
    /// Shows button's context menu. 
    /// </summary>
    private void ButtonTemplateHelperClicked(object? sender, RoutedEventArgs e)
    {
        ContextMenuTemplateHelper.Open();
    }

    /// <summary>
    /// Pastes selected user template field with curly braces into <c>TextBoxTemplate</c>
    /// </summary>
    private void ContextMenuTemplateHelperItemClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: TemplateField templateField })
        {
            TextBoxTemplate.Text += $"{{{templateField}}}";
        }
    }

    /// <summary>
    /// Fills <c>TextBoxResult</c> with info about files in <c>_audioFiles</c> that don't have unique title.
    /// </summary>
    private void ButtonDuplicateCheckerClicked(object? sender, RoutedEventArgs e)
    {
        if (_audioFiles.Count == 0)
        {
            TextBoxResult.Text = "Choose a directory with some audio files first.";
            return;
        }

        TextBoxResult.Text = DuplicateChecker.GetDuplicatesInfo(
            _audioFiles,
            CheckBoxDuplicateCheckerBracketsIgnore.IsChecked ?? false
        );
    }

    /// <summary>
    /// Shows a dialog with basic info about this program.
    /// </summary>
    private void MenuItemAboutClicked(object? sender, RoutedEventArgs e)
    {
        var dialog = new AboutDialog();
        dialog.ShowDialog(this);
        // Not awaiting async because no need for result?
    }
}