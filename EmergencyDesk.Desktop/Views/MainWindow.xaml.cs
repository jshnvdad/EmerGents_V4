using System.Windows;
using EmergencyDesk.ViewModels;

namespace EmergencyDesk.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = new MainViewModel();
        DataContext = vm;

        Closed += (_, _) =>
        {
            vm.Shutdown();   // stop polling and close any open alert popup
            // The app uses explicit shutdown; closing the active main window (not a sign-out swap) ends it.
            if (Application.Current.MainWindow == this) Application.Current.Shutdown();
        };
    }
}
