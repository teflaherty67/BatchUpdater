using BatchUpdater.Common;
using BatchUpdater.Progress_Bar;
using System.IO;
using System.Windows.Shapes;

namespace BatchUpdater
{
    [Transaction(TransactionMode.Manual)]
    public class cmdDeleteBackups : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Revit application and document variables
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // show form to get user input
                frmDeleteBackups curForm = new frmDeleteBackups();
                bool? result = curForm.ShowDialog();

                if (result != true)
                    return Result.Cancelled;

                // get user input from form
                string targetFolder = curForm.SelectedFolder;
                bool includeSubFolders = curForm.IncludeSubfolders;

                // process deletion of backup files
                ProcessBackups(targetFolder, includeSubFolders);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private void ProcessBackups(string targetFolder, bool includeSubfolders)
        {
            // Set variables
            int counter = 0;
            string logPath = "";

            // Create list for log file
            List<string> deletedFileLog = new List<string>();
            deletedFileLog.Add("The following backup files have been deleted:");

            // Get all files from selected folder
            SearchOption searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            string[] files = Directory.GetFiles(targetFolder, "*.*", searchOption);

            if (files.Length == 0)
            {
                Utils.TaskDialogInformation("No Files", "Delete Backups", "No files found in the selected folder.");
                return;
            }

            // Initialize progress bar
            ProgressBarHelper progressHelper = new ProgressBarHelper();
            progressHelper.ShowProgress(files.Length);

            try
            {
                // Loop through the files
                for (int i = 0; i < files.Length; i++)
                {
                    string file = files[i];

                    // Check for user cancellation
                    if (progressHelper.IsCancelled())
                    {
                        progressHelper.CloseProgress();
                        Utils.TaskDialogInformation("Cancelled", "Delete Backups", "Operation cancelled by user.");
                        return;
                    }

                    // Update progress
                    string fileName = Path.GetFileName(file);
                    progressHelper.UpdateProgress(i + 1, $"Checking: {fileName}");

                    // Check if the file is a Revit file
                    string extension = Path.GetExtension(file);
                    if (extension == ".rvt" || extension == ".rfa" || extension == ".rte")
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
            }

            finally
            {
                // Always close progress bar
                progressHelper.CloseProgress();
            }

            // Output log file if files were deleted
            if (counter > 0)
            {
                logPath = WriteListToText(deletedFileLog, targetFolder);

                // Show results with option to view log
                TaskDialog td = new TaskDialog("Complete");
                td.MainIcon = Icon.TaskDialogIconInformation;
                td.Title = "Delete Backups";
                td.TitleAutoPrefix = false;
                td.MainContent = $"Deleted {counter} backup files.";
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Click to view log file");
                td.CommonButtons = TaskDialogCommonButtons.Ok;

                TaskDialogResult tdResult = td.Show();

                if (tdResult == TaskDialogResult.CommandLink1)
                {
                    Process.Start(logPath);
                }
            }

            else
            {
                Utils.TaskDialogInformation("Complete", "Delete Backups", "No backup files found.");
            }
        }

        private string WriteListToText(List<string> stringList, string filePath)
        {
            string fileName = "_Deleted Backup Files.txt";
            string fullPath = Path.Combine(filePath, fileName);
            File.WriteAllLines(fullPath, stringList);
            return fullPath;
        }

        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btnCommand2";
            string buttonTitle = "Button 2";

            Common.ButtonDataClass myButtonData = new Common.ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This is a tooltip for Button 2");

            return myButtonData.Data;
        }
    }
}
