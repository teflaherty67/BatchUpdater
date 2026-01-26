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

        // Show your existing progress window
        public void ShowProgress(int totalOperations)
        {
            if (_progressBar == null)
            {
                // Create your WPF window only if it doesn't exist
                _progressBar = new frmProgressBar(totalOperations);
                // Make the window a child of Revit's main window
                var mainWindowHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                var helper = new System.Windows.Interop.WindowInteropHelper(_progressBar);
                helper.Owner = mainWindowHandle;
                // Show the window
                _progressBar.Show();
            }
            else
            {
                // Reset the existing window for new phase
                _progressBar.Total = totalOperations;
                _progressBar.pbProgress.Maximum = totalOperations;
                _progressBar.pbProgress.Value = 0;
                _progressBar.lblText.Text = $"Updating 0 of {totalOperations} elements";
                _progressBar.CancelFlag = false; // Reset cancel flag for new phase
            }
        }

        // Update the progress
        public void UpdateProgress(int currentOperation, string message = null)
        {
            if (_progressBar == null)
                return;

            // Update progress bar value
            _progressBar.pbProgress.Value = currentOperation;

            // Update status text if present
            if (message != null && _progressBar.lblText != null)
                _progressBar.lblText.Text = message;
            else
                _progressBar.lblText.Text = $"Updating {currentOperation} of {_progressBar.Total} elements";

            // Process any pending UI operations to ensure window is fully rendered
            // This prevents the window from appearing blank initially
            DoEvents();
        }

        // Close the progress window
        public void CloseProgress()
        {
            if (_progressBar != null)
            {
                _progressBar.Close();
                _progressBar = null;
            }
        }

        public bool IsCancelled()
        {
            return _progressBar?.CancelFlag ?? false;
        }

        // Helper method to process UI events
        private void DoEvents()
        {
            // Create a new temporary message processing loop (DispatcherFrame)
            // This frame will run until we explicitly tell it to stop
            DispatcherFrame frame = new DispatcherFrame();

            // Schedule a low-priority callback that will terminate the temporary loop
            // Background priority ensures UI updates happen before this callback executes
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(ExitFrame), frame);

            // Start processing messages in this temporary loop
            // This call blocks until the frame's Continue property is set to false
            // During this time, UI events are processed, allowing the display to update
            Dispatcher.PushFrame(frame);
        }

        private object ExitFrame(object frame)
        {
            // Set the Continue property to false, which will cause PushFrame to return
            // This terminates the temporary message loop created in DoEvents
            ((DispatcherFrame)frame).Continue = false;
            return null;
        }
    }
}

