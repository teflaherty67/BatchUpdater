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

namespace BatchUpdater
{
    /// <summary>
    /// Interaction logic for frmProgressBar.xaml
    /// </summary>
    public partial class frmUpdateBar : Window
    {
        private int _totalFiles;
        private int _currentFileIndex;

        public frmUpdateBar(int totalFiles)
        {
            InitializeComponent();
            _totalFiles = totalFiles;
            _currentFileIndex = 0;
            UpdateProgressText();
        }

        public void UpdateProgress(string currentFileName)
        {
            _currentFileIndex++;

            Dispatcher.Invoke(() =>
            {
                txtCurrentFile.Text = $"Processing: {currentFileName}";
                progressBar.Value = (_currentFileIndex * 100.0) / _totalFiles;
                UpdateProgressText();
            });

            System.Windows.Forms.Application.DoEvents();
        }

        private void UpdateProgressText()
        {
            txtProgress.Text = $"{_currentFileIndex} of {_totalFiles} files processed";
        }
    }
}