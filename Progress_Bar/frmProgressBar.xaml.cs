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
        private Dispatcher _uiDispatcher;
        private Thread _uiThread;

        private readonly ManualResetEventSlim _windowReady = new ManualResetEventSlim(false);
        private volatile bool _closing;
        private volatile bool _cancelled;

        public void ShowProgress(int totalOperations)
        {
            _cancelled = false;
            _closing = false;

            // If already running, just reset
            if (_uiDispatcher != null && _progressBar != null)
            {
                var pb = _progressBar;
                _uiDispatcher.BeginInvoke(new Action(() =>
                {
                    if (_closing || pb == null) return;

                    pb.Total = totalOperations;
                    pb.pbProgress.Minimum = 0;
                    pb.pbProgress.Maximum = totalOperations;
                    pb.pbProgress.Value = 0;
                    pb.lblText.Text = $"Updating 0 of {totalOperations} files";
                    pb.CancelFlag = false;
                }));
                return;
            }

            _windowReady.Reset();

            _uiThread = new Thread(() =>
            {
                _uiDispatcher = Dispatcher.CurrentDispatcher;

                _progressBar = new frmProgressBar(totalOperations);

                // Tie cancel button to helper flag (keep your existing CancelFlag too)
                _progressBar.btnCancel.Click += (_, __) => _cancelled = true;

                // If user closes the window, treat as closing
                _progressBar.Closed += (_, __) =>
                {
                    _closing = true;
                    try { Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.Background); }
                    catch { /* ignore */ }
                };

                // Owner = Revit main window (optional)
                var mainWindowHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                var helper = new System.Windows.Interop.WindowInteropHelper(_progressBar);
                helper.Owner = mainWindowHandle;

                _progressBar.Show();
                _windowReady.Set();

                Dispatcher.Run();
            });

            _uiThread.IsBackground = true;
            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.Start();

            _windowReady.Wait();
        }

        public void UpdateProgress(int currentOperation, string message = null)
        {
            // capture locals to prevent race with CloseProgress()
            var disp = _uiDispatcher;
            var pb = _progressBar;

            if (_closing || disp == null || pb == null) return;

            disp.BeginInvoke(new Action(() =>
            {
                // re-check inside callback too
                if (_closing || pb == null) return;
                if (!pb.IsVisible) return;

                pb.pbProgress.Value = currentOperation;

                if (!string.IsNullOrWhiteSpace(message))
                    pb.lblText.Text = message;
                else
                    pb.lblText.Text = $"Updating {currentOperation} of {pb.Total} files";

            }), DispatcherPriority.Background);
        }

        public void CloseProgress()
        {
            var disp = _uiDispatcher;
            var pb = _progressBar;

            if (disp == null || pb == null) return;

            _closing = true;

            try
            {
                // Close on UI thread
                disp.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (pb.IsVisible)
                            pb.Close();
                    }
                    catch { /* ignore */ }
                }), DispatcherPriority.Send);

                // Shutdown dispatcher loop
                disp.BeginInvokeShutdown(DispatcherPriority.Background);
            }
            catch { /* ignore */ }
            finally
            {
                _progressBar = null;
                _uiDispatcher = null;
                _uiThread = null;
            }
        }

        public bool IsCancelled()
        {
            if (_cancelled) return true;
            return _progressBar?.CancelFlag ?? false;
        }
    }
}