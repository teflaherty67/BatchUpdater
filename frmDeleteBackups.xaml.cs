using BatchUpdater.Common;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// Interaction logic for frmDeleteBackups.xaml
    /// </summary>
    public partial class frmDeleteBackups : Window
    {
        // public properties to expose form values
        public string SelectedFolder { get; set; }
        public bool IncludeSubfolders { get; set; }

        public frmDeleteBackups()
        {
            InitializeComponent();
        }
        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Folder";
                dialog.ShowNewFolderButton = false;
                dialog.SelectedPath = @"S:\Shared Folders\Lifestyle USA Design";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    tbxFolder.Text = dialog.SelectedPath;
                }
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(tbxFolder.Text))
            {
                Utils.TaskDialogWarning("Validation Error", "Delete Backups", "Please select a folder.");
                return;
            }

            if (!Directory.Exists(tbxFolder.Text))
            {
                Utils.TaskDialogWarning("Validation Error", "Delete Backups", "Selected folder does not exist.");
                return;
            }

            // Set properties
            SelectedFolder = tbxFolder.Text;
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
