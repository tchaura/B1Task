using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;

namespace B1Task
{
    public partial class MainWindow : Window
    {
        private string _folderPath;
        private string _sourceFolderPath;
        private string _destinationFolderPath;


        public MainWindow()
        {
            InitializeComponent();
            var appBaseDir = System.AppDomain.CurrentDomain.BaseDirectory;
            _folderPath = Path.Combine(appBaseDir, "generated");
            _sourceFolderPath = _folderPath;
            _destinationFolderPath = Path.Combine(appBaseDir, "merged");
            Directory.CreateDirectory(_sourceFolderPath);
            Directory.CreateDirectory(_destinationFolderPath);
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
          
        }

        private void SelectSourceFolderButton_Click(object sender, RoutedEventArgs e)
        {
      
        }
        private void SelectDestinationFolderButton_Click(object sender, RoutedEventArgs e)
        {
   
        }

        private async void GenerateFilesButton_Click(object sender, RoutedEventArgs e)
        {
            var progressDialog = new ProgressDialog();
            try
            {
                var progress = new Progress<int>(value =>
                {
                    progressDialog.UpdateProgress(value);
                });

                progressDialog.Owner = this;
                var generateFilesAsyncTask = FileGenerator.GenerateFilesAsync(_folderPath, 100, 100000, progress);
                progressDialog.ShowDialog();
                await generateFilesAsyncTask;

                MessageBox.Show("Файлы успешно сгенерированы!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации файлов: {ex.Message}");
            }
            finally{progressDialog.Close();}
        }


        private async void MergeFilesButton_Click(object sender, RoutedEventArgs e)
        {
            var progressDialog = new ProgressDialog();
            try
            {
                var filter = FilterTextBox.Text;
                var merger = new FileMerger();

                var progress = new Progress<int>(value =>
                {
                    progressDialog.UpdateProgress(value);
                });

                progressDialog.Owner = this;

                var destinationFilePath = Path.Combine(_destinationFolderPath, "merged.txt");
                var stopwatch = Stopwatch.StartNew();
                var removedCountTask = merger.MergeFiles(_sourceFolderPath, destinationFilePath, filter, progress);
                progressDialog.ShowDialog();
                var removedCount = await removedCountTask;
                stopwatch.Stop();

                MessageBox.Show($"Файлы успешно объединены! Удалено строк: {removedCount} in {stopwatch.Elapsed}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка объединения файлов: {ex.Message}");
            }
            finally{progressDialog.Close();}
        }

        private async void MergeFilesAsyncButton_Click(object sender, RoutedEventArgs e)
        {
            var progressDialog = new ProgressDialog();
            try
            {
                var filter = FilterTextBox.Text;
                var merger = new FileMerger();

                var progress = new Progress<int>(value =>
                {
                    progressDialog.UpdateProgress(value);
                });

                progressDialog.Owner = this;

                var destinationFilePath = Path.Combine(_destinationFolderPath, "merged.txt");
                var mergeFilesAsyncTask = merger.MergeFilesAsync(_sourceFolderPath, destinationFilePath, filter, progress);
                var stopwatch = Stopwatch.StartNew();
                progressDialog.ShowDialog();
                
                var removedCount = await mergeFilesAsyncTask;
                stopwatch.Stop();

                MessageBox.Show($"Файлы успешно объединены! Удалено строк: {removedCount} in {stopwatch.Elapsed}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка объединения файлов: {ex.Message}");
            }
            finally{progressDialog.Close();}
        }



        private async void ImportToDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            var progressDialog = new ProgressDialog(10_000_000);
            try
            {
                var importer =
                    new DatabaseImporter(
                        "Server=127.0.0.1;Port=5432;Database=postgres;UserId=postgres;Password=admin;");
                var filePath = Path.Combine(_destinationFolderPath, "merged.txt");

                var progress = new Progress<int>((value) => { progressDialog.UpdateProgressWithText(value); });
                progressDialog.Owner = this;
                var importDataAsyncTask = importer.ImportDataAsync(filePath, progress);
                var stopwatch = Stopwatch.StartNew();
                progressDialog.ShowDialog();
                await importDataAsyncTask;
                
                stopwatch.Stop();
                MessageBox.Show($"Импорт успешно завершён in {stopwatch.Elapsed}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка импорта в БД: {ex.Message}");
            }
            finally
            {
                progressDialog.Close();
            }
        }
    }
}
