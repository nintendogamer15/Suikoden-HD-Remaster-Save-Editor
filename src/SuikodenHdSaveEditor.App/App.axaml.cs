// SPDX-License-Identifier: 0BSD
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace SuikodenHdSaveEditor.App;

public partial class App : Application
{
    public static bool SmokeTestRequested { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            if (SmokeTestRequested)
            {
                DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(900) };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    desktop.Shutdown(0);
                };
                timer.Start();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
