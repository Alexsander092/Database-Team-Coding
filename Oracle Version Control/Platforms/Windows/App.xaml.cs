using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using Windows.ApplicationModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Oracle_Version_Control.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            
            // Register error handlers for Windows-specific navigation issues
            UnhandledException += App_UnhandledException;
        }

        /// <summary>
        /// Handle unhandled exceptions at the application level
        /// </summary>
        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // Mark the exception as handled to prevent crash
            e.Handled = true;
            
            // Log the exception
            Debug.WriteLine($"Unhandled exception: {e.Exception}");
            
            // If this is a navigation failed exception, handle it specially
            if (e.Exception.ToString().Contains("NavigationFailed"))
            {
                Debug.WriteLine("Navigation failed. This may be due to a page not being registered properly.");
            }
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
        
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);
            
            // WinUI doesn't have the same navigation event setup as UWP
            // We'll just log that the application has launched
            Debug.WriteLine("Application launched on Windows platform");
            
            try
            {
                // Add global exception handling
                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    if (e.ExceptionObject is Exception ex)
                    {
                        Debug.WriteLine($"AppDomain Unhandled Exception: {ex.Message}");
                        
                        // Check if it's a navigation-related exception
                        if (ex.ToString().Contains("NavigationFailed") || ex.ToString().Contains("Navigation"))
                        {
                            Debug.WriteLine("Navigation failed in AppDomain. This may be due to a page not being registered properly.");
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting up exception handling: {ex.Message}");
            }
        }
    }
}
