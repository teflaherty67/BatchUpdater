using BatchUpdater.Common;
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
using MessageBox = System.Windows.MessageBox;

namespace BatchUpdater
{
    /// <summary>
    /// Interaction logic for frmBatchUpdater.xaml
    /// </summary>
    public partial class frmBatchUpdate : Window
    {
        // public properties to expose form values
        public string SourceFolder { get; private set; }
        public string TargetFolder { get; private set; }
        public bool IncludeSubfolders { get; private set; }

        public frmBatchUpdate()
        {
            InitializeComponent();
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Source Folder";
                dialog.ShowNewFolderButton = false;
                dialog.SelectedPath = @"S:\Shared Folders\Lifestyle USA Design";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    tbxFolder.Text = dialog.SelectedPath;
                }
            }
        }

        private void btnTargetSelect_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Target Folder";
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    tbxTargetFolder.Text = dialog.SelectedPath;
                }
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(tbxFolder.Text))
            {
                Utils.TaskDialogWarning("Validation Error", "Batch Update", "Please select a source folder.");
                return;
            }

            if (!Directory.Exists(tbxFolder.Text))
            {
                Utils.TaskDialogWarning("Validation Error", "Batch Update", "Source folder does not exist.");
                return;
            }

            if (string.IsNullOrWhiteSpace(tbxTargetFolder.Text))
            {
                Utils.TaskDialogWarning("Validation Error", "Batch Update", "Please select a target folder.");
                return;
            }

            // Set properties
            SourceFolder = tbxFolder.Text;
            TargetFolder = tbxTargetFolder.Text;
            IncludeSubfolders = cbxSubFolders.IsChecked ?? false;

            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://lifestyle-usa-design.atlassian.net/wiki/x/AYBOJ");
        }
    }
}