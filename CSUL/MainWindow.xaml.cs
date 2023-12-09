using CSUL.Models;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WPFCustomMessageBox;

namespace CSUL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Closing += (sender, e) =>
            {
                e.Cancel = true;
                ExitProgram(0);
            };

            #region ---异常处理---

            //全局异常捕获
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                StringBuilder builder = new();
                builder.AppendLine(LanguageManager.GetString("ExceptionNotCatch"));
                builder.AppendLine($"{LanguageManager.GetString("ExceptionType")}: {e.ExceptionObject.GetType().Name}");
                builder.AppendLine($"{LanguageManager.GetString("AppStatus")}: {(e.IsTerminating ? LanguageManager.GetString("AppStatus_Quit") : LanguageManager.GetString("AppStatus_Normal"))}");
                builder.AppendLine($"{LanguageManager.GetString("ExceptionObject")}: {sender?.GetType().FullName ?? LanguageManager.GetString("Unknown")}");
                builder.AppendLine($"{LanguageManager.GetString("ExceptionAssembly")}: {sender?.GetType().AssemblyQualifiedName ?? LanguageManager.GetString("Unknown")}");
                LanguageManager.MessageBox(
                    ExceptionManager.GetExMeg(e.ExceptionObject as Exception, builder.ToString()),
                    LanguageManager.GetString("ExceptionNotCatch"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            };

            //Task线程异常捕获
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                StringBuilder builder = new();
                builder.AppendLine(LanguageManager.GetString("TaskExceptionNotCatch"));
                builder.AppendLine($"{LanguageManager.GetString("ExceptionType")}: {e.Exception.GetType().Name}");
                builder.AppendLine($"{LanguageManager.GetString("ExceptionObject")}: {sender?.GetType().FullName}");
                builder.AppendLine($"{LanguageManager.GetString("ExceptionAssembly")}: {sender?.GetType().AssemblyQualifiedName}");
                LanguageManager.MessageBox(
                    ExceptionManager.GetExMeg(e.Exception, builder.ToString()),
                    LanguageManager.GetString("TaskExceptionNotCatch"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            };

            Dispatcher.UnhandledException += (sender, e) =>
            {
                LanguageManager.MessageBox(
                    ExceptionManager.GetExMeg(e.Exception),
                    LanguageManager.GetString("DispatcherExceptionNotCatch"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                e.Handled = true;
            };

            Dispatcher.UnhandledExceptionFilter += (sender, e) =>
            {
                LanguageManager.MessageBox(
                    ExceptionManager.GetExMeg(e.Exception),
                    LanguageManager.GetString("FilterExceptionNotCatch"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                e.RequestCatch = false;
            };

            #endregion ---异常处理---
        }

        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
            => ExitProgram(0);

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// 退出程序
        /// </summary>
        private static void ExitProgram(int exitCode)
        {
            MessageBoxResult ret = LanguageManager.MessageBox(
                LanguageManager.GetString("Msg_ExitConfirm"),
                LanguageManager.GetString("Msg_Caution"),
                MessageBoxButton.OKCancel);
            if (ret == MessageBoxResult.OK)
            {
                FileManager.Instance.Dispose();
                GameManager.Instance.Dispose();
                LanguageManager.Instance.Dispose();
                Environment.Exit(exitCode);
            }
        }
    }
}