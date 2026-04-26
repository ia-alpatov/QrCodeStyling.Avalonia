using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using QrCodeStyling.Avalonia.Test.ViewModels;
using QrCodeStyling.Avalonia.Test.Views;

namespace QrCodeStyling.Avalonia.Test;

public partial class App : Application {
    public override void Initialize() {
        AvaloniaXamlLoader.Load( this );
    }

    public override void OnFrameworkInitializationCompleted() {
        if ( ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ) {
            desktop.MainWindow = new MainWindow {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}