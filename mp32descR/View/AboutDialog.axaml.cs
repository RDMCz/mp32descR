using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace mp32descR.View;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();

        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        
        TextBlockVersion.Text = $"version {version}";
    }

    private void ButtonOkClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}