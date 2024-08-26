using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using B1Task.TableGenerator;

namespace B1Task;

public partial class TableGeneratorWindow : Window
{
    private readonly string _sourceFolderPath;
    private readonly string _destinationFolderPath;
    private int _totalRowsCount;
    private readonly string _connectionString;
    
    public TableGeneratorWindow(string connectionString)
    {
        InitializeComponent();
        
        var appBaseDir = AppDomain.CurrentDomain.BaseDirectory;
        _sourceFolderPath = Path.Combine(appBaseDir, "generated");
        _destinationFolderPath = Path.Combine(appBaseDir, "merged");
        Directory.CreateDirectory(_sourceFolderPath);
        Directory.CreateDirectory(_destinationFolderPath);
        
        _connectionString = connectionString;
    }

    private async void GenerateFilesButton_Click(object sender, RoutedEventArgs e)
    {
        var progressDialog = new ProgressDialog
        {
            Owner = this
        };
        var progress = new Progress<int>(value =>
        {
            progressDialog.UpdateProgress(value);
        });
        
        try
        {
            var generateFilesAsyncTask = FileGenerator.GenerateFilesAsync(_sourceFolderPath, 100, 100000, progress);
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
        var progressDialog = new ProgressDialog
        {
            Owner = this,
        };
        var progress = new Progress<int>(value =>
        {
            progressDialog.UpdateProgress(value);
        });
        try
        {
            var filter = FilterTextBox.Text;
            var merger = new FileMerger();

            var destinationFilePath = Path.Combine(_destinationFolderPath, "merged.txt");
            
            var stopwatch = Stopwatch.StartNew();
            
            var removedCountTask = merger.MergeFiles(_sourceFolderPath, destinationFilePath, filter, progress);
            progressDialog.ShowDialog();
            var removedCount = await removedCountTask;
            
            stopwatch.Stop();
                
            _totalRowsCount = 10_000_000 - removedCount;

            MessageBox.Show($"Файлы успешно объединены! Удалено строк: {removedCount}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка объединения файлов: {ex.Message}");
        }
        finally{progressDialog.Close();}
    }

    private async void ImportToDatabaseButton_Click(object sender, RoutedEventArgs e)
    {
        var filePath = Path.Combine(_destinationFolderPath, "merged.txt");
        if (_totalRowsCount == 0)
        {
            _totalRowsCount = FileRowsCounter(filePath);
        }
        var progressDialog = new ProgressDialog(_totalRowsCount)
        {
            Owner = this
        };
        var progress = new Progress<int>((value) => { progressDialog.UpdateProgressWithText(value); });

        try
        {
            var importer = new DatabaseImporter(_connectionString);

            var importDataAsyncTask = importer.ImportDataAsync(filePath, progress);
            
            var stopwatch = Stopwatch.StartNew();
            
            progressDialog.ShowDialog();
            await importDataAsyncTask;
                
            stopwatch.Stop();
            MessageBox.Show($"Импорт успешно завершён");
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

    private static int FileRowsCounter(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None, 1024 * 1024);

        var lineCount = 0;
        var buffer = new byte[1024 * 1024];
        int bytesRead;

        do
        {
            bytesRead = fs.Read(buffer, 0, buffer.Length);
            for (var i = 0; i < bytesRead; i++)
                if (buffer[i] == '\n')
                    lineCount++;
        }
        while (bytesRead > 0);
        
        return lineCount;
    }
}