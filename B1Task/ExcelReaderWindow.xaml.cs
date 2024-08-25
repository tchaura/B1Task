using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using B1Task.ExcelReader;
using Microsoft.Win32;
using Npgsql;
using NpgsqlTypes;

namespace B1Task;

public partial class ExcelReaderWindow : Window
{
    private string _connectionString;
    public ExcelReaderWindow(string connectionString)
    {
        InitializeComponent();
        _connectionString = connectionString;
    }

    private void UploadFileButton_OnClick(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Excel files (*.xls, *.xlsx)|*.xls;*.xlsx",
            Multiselect = false
        };
        openFileDialog.ShowDialog();

        if (string.IsNullOrWhiteSpace(openFileDialog.FileName))
        {
            return;
        }
        
        try
        {
            var excelReader = new ExcelParser(_connectionString);
            excelReader.Import(openFileDialog.FileName);
            MessageBox.Show("Импорт завершен");
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            MessageBox.Show($"Ошибка импорта: {exception.Message}");
        }
    }

    private void ShowUploads_OnClick(object sender, RoutedEventArgs e)
    {
        ActionsGrid.Visibility = Visibility.Hidden;
        SelectUploadPanel.Visibility = Visibility.Visible;
        
        SelectUploadGrid.ItemsSource = LoadUploadsList();
    }

    private ObservableCollection<Upload> LoadUploadsList()
    {
        var uploads = new ObservableCollection<Upload>();
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM balancesheetschema.uploadedfiles JOIN balancesheetschema.banks b on b.bankid = uploadedfiles.bankid";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            uploads.Add(new Upload
            {
                FileId = reader.GetInt16(0),
                DateUploaded = reader.GetDateTime(1),
                FileName = reader.GetString(2),
                PeriodStart = reader.GetDateTime(3).ToShortDateString(),
                PeriodEnd = reader.GetDateTime(4).ToShortDateString(),
                SheetName = reader.GetString(5),
                BankName = reader.GetString(8),
            });
            
        }
        
        return uploads;
    }

    class Upload
    {
        public int FileId { get; set; }
        public string SheetName { get; set; }
        public string? BankName { get; set; }
        public string PeriodStart { get; set; }
        public string PeriodEnd { get; set; }
        public DateTime DateUploaded { get; set; }
        public string? FileName { get; set; }
    }

    private void SelectUploadGrid_OnCurrentCellChanged(object? sender, EventArgs e)
    {
        ShowSheetButton.IsEnabled = true;
    }

    private void ShowSheetButton_OnClick(object sender, RoutedEventArgs e)
    {
        var selectedUpload = (Upload)SelectUploadGrid.SelectedItem;
        var balanceSheetWindow = new BalanceSheetWindow(selectedUpload.FileId, selectedUpload.SheetName, _connectionString);
        balanceSheetWindow.Owner = this;
        balanceSheetWindow.ShowDialog();
    }
}