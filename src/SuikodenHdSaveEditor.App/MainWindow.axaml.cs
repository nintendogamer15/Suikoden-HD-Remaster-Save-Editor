// SPDX-License-Identifier: 0BSD
using Avalonia.Controls;
using SuikodenHdSaveEditor.App.Services;
using SuikodenHdSaveEditor.App.ViewModels;

namespace SuikodenHdSaveEditor.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainWindowViewModel(new AvaloniaUserInteraction(this));
        DataContext = ViewModel;
    }

    public MainWindowViewModel ViewModel { get; }
}
