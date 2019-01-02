using DbDarwin.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace DbDarwin.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Directory.CreateDirectory(ConstantData.WorkingDir);
            Directory.CreateDirectory(ConstantData.LogDir);

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }



        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            if (e.Exception != null)
                ShowAndLogError(e.Exception);
        }


        private void TaskScheduler_UnobservedTaskException(object sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            if (e.Exception != null)
                ShowAndLogError(e.Exception.InnerException ?? e.Exception);
        }


        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject != null)
                ShowAndLogError((Exception)e.ExceptionObject);
        }



        public static void ShowAndLogError(Exception ex)
        {

            LogService.Error(ex);
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
            {
                if (MessageBox.Show(Application.Current.MainWindow, ex.Message + $"\r\n for resolve this error please compress and send these folders to developer \r\n{ConstantData.LogDir} \r\n{ConstantData.WorkingDir} \r\n would you like to open log path ?", "Unhandled Error", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                    Process.Start(ConstantData.LogDir);
            }));
        }
    }
}
