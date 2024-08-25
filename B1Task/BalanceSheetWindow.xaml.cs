using System.Windows;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Npgsql;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;

namespace B1Task;

public partial class BalanceSheetWindow : Window
{
    public BalanceSheetWindow(int fileId, string sheetName, string connectionString)
    {
        InitializeComponent();

        SheetNameBlock.Text = sheetName;
        BalanceSheetDataGrid.ItemsSource = GetBalanceSheetData(fileId, connectionString);
    }

    public class BalanceSheetRow
    {
        public string? AccountId { get; set; }
        public string InitialDebit { get; set; }
        public string InitialCredit { get; set; }
        public string TurnoverDebit { get; set; }
        public string TurnoverCredit { get; set; }
        public string EndingDebit { get; set; }
        public string EndingCredit { get; set; }
    }

    private List<BalanceSheetRow> GetBalanceSheetData(int fileId, string connectionString)
    {
        var result = new List<BalanceSheetRow>();

        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();

            string query = @"
            SELECT bs.AccountID, c.classificationId, c.ClassificationName, 
                   bs.InitialDebit, bs.InitialCredit, 
                   bs.TurnoverDebit, bs.TurnoverCredit, 
                   bs.EndingDebit, bs.EndingCredit
            FROM BalanceSheetSchema.BalanceSheet bs
            JOIN BalanceSheetSchema.Classifications c ON bs.ClassificationID = c.ClassificationID

            WHERE bs.FileID = @fileId;
        ";

            using (var command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("fileId", fileId);

                using (var reader = command.ExecuteReader())
                {
                    int? lastClassificationId = null;

                    while (reader.Read())
                    {
                        var accountId = reader.GetInt32(0);
                        var classificationId = reader.GetInt32(1);
                        var classificationName = reader.GetString(2);
                        var initialDebit = reader.GetDecimal(3);
                        var initialCredit = reader.GetDecimal(4);
                        var turnoverDebit = reader.GetDecimal(5);
                        var turnoverCredit = reader.GetDecimal(6);
                        var endingDebit = reader.GetDecimal(7);
                        var endingCredit = reader.GetDecimal(8);

                        if (lastClassificationId == null || classificationId != lastClassificationId && classificationId != 0)
                        {
                            result.Add(new BalanceSheetRow
                            {
                                AccountId = $"-- {classificationName} --",
                                InitialDebit = String.Empty,
                                InitialCredit = String.Empty,
                                TurnoverDebit = String.Empty,
                                TurnoverCredit = String.Empty,
                                EndingDebit = String.Empty,
                                EndingCredit =String.Empty
                            });

                            lastClassificationId = classificationId;
                        }

                        result.Add(new BalanceSheetRow
                        {
                            AccountId = accountId == 0 ? "БАЛАНС" :
                                accountId < 10 ? "ПО КЛАССУ" : accountId.ToString(),
                            InitialDebit = initialDebit.ToString(),
                            InitialCredit = initialCredit.ToString(),
                            TurnoverDebit = turnoverDebit.ToString(),
                            TurnoverCredit = turnoverCredit.ToString(),
                            EndingDebit = endingDebit.ToString(),
                            EndingCredit = endingCredit.ToString()
                        });
                    }
                }
            }
        }

        return result;
    }
    
}
public class StartsWithConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string accountId && parameter is string prefix)
        {
            return accountId.StartsWith(prefix);
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}

public class TwoDigitAccountIDConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string accountId)
        {
            return accountId.Length == 2 && int.TryParse(accountId, out _);
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}
