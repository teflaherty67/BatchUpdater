using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BatchUpdater.Progress_Bar
{
    /// <summary>
    /// Interaction logic for frmProgressBar.xaml
    /// </summary>
    public partial class frmProgressBar : Window
    {
        public int Total;
        public bool CancelFlag = false;

        public frmProgressBar(int total)
        {
            InitializeComponent();
            Total = total;

            lblText.Text = $"Updating 0 of {Total} elements";

            pbProgress.Minimum = 0;
            pbProgress.Maximum = Total;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            CancelFlag = true;
        }
    }

    public class ProgressBarHelper
    {
        private frmProgressBar _progressBar;
        private System.Windows.Threading.Dispatcher _uiDispatcher;
        private Thread _uiThread;

        private readonly ManualResetEventSlim _windowReady = new ManualResetEventSlim(false);

        // thread-safe cancel flag
        private volatile bool _cancelled;

        public void ShowProgress(int totalOperations)
        {
            _cancelled = false;

            // If already running, just reset values on the UI thread
            if (_uiDispatcher != null && _progressBar != null)
            {
                _uiDispatcher.BeginInvoke(new Action(() =>
                {
                    _progressBar.Total = totalOperations;
                    _progressBar.pbProgress.Minimum = 0;
                    _progressBar.pbProgress.Maximum = totalOperations;
                    _progressBar.pbProgress.Value = 0;
                    _progressBar.lblText.Text = $"Updating 0 of {totalOperations} files";
                    _progressBar.CancelFlag = false;
                }));
                return;
            }

            _windowReady.Reset();

            _uiThread = new Thread(() =>
            {
                // Create a Dispatcher for this thread
                _uiDispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

                _progressBar = new frmProgressBar(totalOperations);

                // When user clicks cancel, propagate to helper flag too
                _progressBar.Closed += (_, __) =>
                {
                    try { System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvokeShutdown(System.Windows.Threading.DispatcherPriority.Background); }
                    catch { /* ignore */ }
                };

                // hook into your existing CancelFlag
                _progressBar.btnCancel.Click += (_, __) => _cancelled = true;

                // Owner = Revit main window (optional; keep if you want)
                var mainWindowHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                var helper = new System.Windows.Interop.WindowInteropHelper(_progressBar);
                helper.Owner = mainWindowHandle;

                _progressBar.Show();

                _windowReady.Set();

                // Start message loop for this UI thread
                System.Windows.Threading.Dispatcher.Run();
            });

            _uiThread.IsBackground = true;
            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.Start();

            // Wait until the window is created before returning
            _windowReady.Wait();
        }

        public void UpdateProgress(int currentOperation, string message = null)
        {
            if (_uiDispatcher == null || _progressBar == null) return;

            _uiDispatcher.BeginInvoke(new Action(() =>
            {
                _progressBar.pbProgress.Value = currentOperation;

                if (!string.IsNullOrWhiteSpace(message))
                    _progressBar.lblText.Text = message;
                else
                    _progressBar.lblText.Text = $"Updating {currentOperation} of {_progressBar.Total} files";
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        public void CloseProgress()
        {
            if (_uiDispatcher == null || _progressBar == null) return;

            try
            {
                _uiDispatcher.Invoke(new Action(() =>
                {
                    if (_progressBar.IsVisible)
                        _progressBar.Close();
                }));
            }
            catch
            {
                // ignore shutdown races
            }
            finally
            {
                _progressBar = null;
                _uiDispatcher = null;
                _uiThread = null;
            }
        }

        public bool IsCancelled()
        {
            // Use either your original CancelFlag or our volatile flag
            if (_cancelled) return true;
            return _progressBar?.CancelFlag ?? false;
        }
    }
}