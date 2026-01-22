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
using Path = System.IO.Path;

namespace BatchUpdater
{
    /// <summary>
    /// Interaction logic for frmBatchUpdater.xaml
    /// </summary>
    public partial class frmBatchUpdate : Window
    {
        private UIApplication _uiApp;

        public frmBatchUpdate(UIApplication uiApp)
        {
            InitializeComponent();

            _uiApp = uiApp;
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            // Determine which button was clicked
            System.Windows.Controls.Button clickedButton = sender as System.Windows.Controls.Button;
            bool isSourceButton = (clickedButton.Name == "btnSelect");

            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = isSourceButton ? "Select Source Folder" : "Select Target Folder";
                dialog.ShowNewFolderButton = !isSourceButton;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (isSourceButton)
                    {
                        tbxFolder.Text = dialog.SelectedPath;
                    }
                    else
                    {
                        tbxTargetFolder.Text = dialog.SelectedPath;
                    }
                }
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(tbxFolder.Text))
            {
                MessageBox.Show("Please select a source folder.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Directory.Exists(tbxFolder.Text))
            {
                MessageBox.Show("Source folder does not exist.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(tbxTargetFolder.Text))
            {
                MessageBox.Show("Please select a target folder.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Process files
            try
            {
                ProcessFiles();
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during processing:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProcessFiles()
        {
            string sourceFolder = tbxFolder.Text;
            string targetFolder = tbxTargetFolder.Text;
            bool includeSubfolders = cbxSubFolders.IsChecked ?? false;

            // Get all Revit files
            SearchOption searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            string[] rvtFiles = Directory.GetFiles(sourceFolder, "*.rvt", searchOption);

            if (rvtFiles.Length == 0)
            {
                MessageBox.Show("No Revit files found in the source folder.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Create progress form
            frmUpdateBar progressForm = new frmUpdateBar(rvtFiles.Length);
            progressForm.Show();

            int successCount = 0;
            int failCount = 0;
            List<string> failedFiles = new List<string>();

            foreach (string sourceFile in rvtFiles)
            {
                try
                {
                    // Update progress
                    string fileName = Path.GetFileName(sourceFile);
                    progressForm.UpdateProgress(fileName);

                    // Calculate relative path from source folder
                    string relativePath = GetRelativePath(sourceFolder, sourceFile);
                    string targetFile = Path.Combine(targetFolder, relativePath);

                    // Create target directory if it doesn't exist
                    string targetDir = Path.GetDirectoryName(targetFile);
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    // Open and save the Revit file
                    Document doc = _uiApp.Application.OpenDocumentFile(sourceFile);

                    if (doc != null)
                    {
                        // Save to target location
                        SaveAsOptions saveOptions = new SaveAsOptions();
                        saveOptions.OverwriteExistingFile = true;

                        doc.SaveAs(targetFile, saveOptions);
                        doc.Close(false);

                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    failedFiles.Add($"{Path.GetFileName(sourceFile)}: {ex.Message}");
                }
            }

            progressForm.Close();

            // Show results
            string resultMessage = $"Processing complete!\n\n" +
                                  $"Successful: {successCount}\n" +
                                  $"Failed: {failCount}";

            if (failedFiles.Count > 0)
            {
                resultMessage += "\n\nFailed files:\n" + string.Join("\n", failedFiles.Take(10));
                if (failedFiles.Count > 10)
                {
                    resultMessage += $"\n... and {failedFiles.Count - 10} more";
                }
            }

            MessageBox.Show(resultMessage, "Processing Complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string GetRelativePath(string fromPath, string toPath)
        {
            Uri fromUri = new Uri(AppendDirectorySeparator(fromPath));
            Uri toUri = new Uri(toPath);

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }

        private string AppendDirectorySeparator(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                return path + Path.DirectorySeparatorChar;
            }
            return path;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            string helpMessage = "Batch Update Revit Files\n\n" +
                "This tool allows you to batch process Revit files:\n\n" +
                "1. Select Source Folder: Choose the folder containing Revit files to process\n" +
                "2. Select Target Folder: Choose where to save the processed files\n" +
                "3. Include Subfolders: Check to process files in all subfolders\n" +
                "4. Click OK to begin processing\n\n" +
                "The tool will:\n" +
                "• Open each Revit file\n" +
                "• Save it to the target folder\n" +
                "• Maintain the folder structure if processing subfolders\n" +
                "• Create target folders as needed\n\n" +
                "Note: This is useful for upgrading files to a newer Revit version " +
                "or applying project standards across multiple files.";

            MessageBox.Show(helpMessage, "Help", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}