using BatchUpdater.Common;
using BatchUpdater.Progress_Bar;

namespace BatchUpdater
{
    [Transaction(TransactionMode.Manual)]
    public class cmdBatchUpdate : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Revit application and document variables
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                frmBatchUpdate curForm = new frmBatchUpdate();
                bool? result = curForm.ShowDialog();

                if (result != true)
                    return Result.Cancelled;

                // Get values from form
                string sourceFolder = curForm.SourceFolder;
                string targetFolder = curForm.TargetFolder;
                bool includeSubfolders = curForm.IncludeSubfolders;

                // process the files
                ProcessFiles(uiapp, sourceFolder, targetFolder, includeSubfolders);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private void ProcessFiles(UIApplication uiApp, string sourceFolder, string targetFolder, bool includeSubfolders)
        {
            // Get all Revit files (projects, families, and templates)
            SearchOption searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            List<string> allFiles = new List<string>();
            allFiles.AddRange(Directory.GetFiles(sourceFolder, "*.rvt", searchOption));
            allFiles.AddRange(Directory.GetFiles(sourceFolder, "*.rfa", searchOption));
            allFiles.AddRange(Directory.GetFiles(sourceFolder, "*.rte", searchOption));

            string[] rvtFiles = allFiles.ToArray();

            if (rvtFiles.Length == 0)
            {
                Utils.TaskDialogInformation("No Files", "Batch Update", "No Revit files found in the source folder.");
                return;
            }

            // Initialize progress bar
            ProgressBarHelper progressHelper = new ProgressBarHelper();
            progressHelper.ShowProgress(rvtFiles.Length);

            int successCount = 0;
            int failCount = 0;
            List<string> failedFiles = new List<string>();

            try
            {
                for (int i = 0; i < rvtFiles.Length; i++)
                {
                    string sourceFile = rvtFiles[i];

                    // Check for user cancellation
                    if (progressHelper.IsCancelled())
                    {
                        progressHelper.CloseProgress();
                        Utils.TaskDialogInformation("Cancelled", "Batch Update", "Operation cancelled by user.");
                        return;
                    }

                    try
                    {
                        // Update progress FIRST
                        string fileName = Path.GetFileName(sourceFile);
                        progressHelper.UpdateProgress(i + 1, $"Upgrading: {fileName}");

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
                        Document doc = uiApp.Application.OpenDocumentFile(sourceFile);

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
            }
            finally
            {
                // Always close progress bar
                progressHelper.CloseProgress();
            }

            // Show results
            string resultMessage = $"Successful: {successCount}\nFailed: {failCount}";

            if (failedFiles.Count > 0)
            {
                string failedList = string.Join("\n", failedFiles.Take(10));
                if (failedFiles.Count > 10)
                {
                    failedList += $"\n... and {failedFiles.Count - 10} more";
                }

                TaskDialog tdResults = new TaskDialog("Processing Complete");
                tdResults.MainIcon = Icon.TaskDialogIconInformation;
                tdResults.Title = "Batch Update";
                tdResults.TitleAutoPrefix = false;
                tdResults.MainContent = resultMessage;
                tdResults.ExpandedContent = $"Failed files:\n{failedList}";
                tdResults.CommonButtons = TaskDialogCommonButtons.Close;
                tdResults.Show();
            }
            else
            {
                Utils.TaskDialogInformation("Complete", "Batch Update", resultMessage);
            }
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

        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btnCommand1";
            string buttonTitle = "Button 1";

            Common.ButtonDataClass myButtonData = new Common.ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This is a tooltip for Button 1");

            return myButtonData.Data;
        }
    }
}
