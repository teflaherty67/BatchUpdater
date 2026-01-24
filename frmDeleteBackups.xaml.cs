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
using Path = System.IO.Path;

namespace BatchUpdater
{
    /// <summary>
    /// Interaction logic for frmDeleteBackups.xaml
    /// </summary>
    public partial class frmDeleteBackups : Window
    {
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
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;

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
                MessageBox.Show("Please select a folder.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Directory.Exists(tbxFolder.Text))
            {
                MessageBox.Show("Selected folder does not exist.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Process files
            try
            {
                ProcessBackups();
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProcessBackups()
        {
            string directory = tbxFolder.Text;
            bool includeSubfolders = cbxSubFolders.IsChecked ?? false;

            // Set variables
            int counter = 0;
            string logPath = "";

            // Create list for log file
            List<string> deletedFileLog = new List<string>();
            deletedFileLog.Add("The following backup files have been deleted:");

            // Get all files from selected folder
            SearchOption searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            string[] files = Directory.GetFiles(directory, "*.*", searchOption);

            // Loop through the files
            foreach (string file in files)
            {
                // Check if the file is a Revit file
                string extension = Path.GetExtension(file);
                if (extension == ".rvt" || extension == ".rfa")
                {
                    // Get the last 9 characters of file name to check if backup
                    if (file.Length >= 9)
                    {
                        string checkString = file.Substring(file.Length - 9, 9);
                        if (checkString.Contains(".0"))
                        {
                            // Add filename to list
                            deletedFileLog.Add(file);

                            // Delete the file
                            File.Delete(file);

                            // Increment the counter
                            counter++;
                        }
                    }
                }
            }

            // Output log file if files were deleted
            if (counter > 0)
            {
                logPath = WriteListToText(deletedFileLog, directory);

                // Show results with option to view log
                string results = $"Deleted {counter} backup files. Show log file?";
                MessageBoxResult result = MessageBox.Show(results, "Complete",
                    MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(logPath);
                }
            }
            else
            {
                MessageBox.Show("No backup files found.", "Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private string WriteListToText(List<string> stringList, string filePath)
        {
            string fileName = "_Deleted Backup Files.txt";
            string fullPath = Path.Combine(filePath, fileName);
            File.WriteAllLines(fullPath, stringList);
            return fullPath;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            string helpMessage = "Delete Backup Files\n\n" +
                "This tool deletes Revit backup files:\n\n" +
                "1. Select Folder: Choose the folder containing Revit files\n" +
                "2. Include Subfolders: Check to search all subfolders\n" +
                "3. Click OK to delete backup files\n\n" +
                "The tool will:\n" +
                "• Search for .rvt and .rfa backup files\n" +
                "• Delete files with backup naming (contains .0###)\n" +
                "• Create a log file of deleted files\n" +
                "• Offer to show the log file\n\n" +
                "Note: Backup files typically have names like:\n" +
                "Project.0001.rvt, Family.0002.rfa, etc.";

            MessageBox.Show(helpMessage, "Help", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
