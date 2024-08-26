using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Npgsql;

namespace B1Task;

public partial class BalanceSheetWindow : Window
{
    public BalanceSheetWindow(int fileId, string? sheetName, NpgsqlConnection connection)
    {
        InitializeComponent();

        if (sheetName != null) SheetNameBlock.Text = sheetName;
        BalanceSheetDataGrid.ItemsSource = GetBalanceSheetData(fileId, connection);
    }

    private static List<BalanceSheetRow> GetBalanceSheetData(int fileId, NpgsqlConnection connection)
    {
        var result = new List<BalanceSheetRow>();

        connection.Open();

        const string query = """
                                SELECT bs.AccountID, c.classificationId, c.ClassificationName, 
                                       bs.InitialDebit, bs.InitialCredit, 
                                       bs.TurnoverDebit, bs.TurnoverCredit, 
                                       bs.EndingDebit, bs.EndingCredit
                                FROM BalanceSheetSchema.BalanceSheet bs
                                JOIN BalanceSheetSchema.Classifications c ON bs.ClassificationID = c.ClassificationID
                                WHERE bs.FileID = @fileId
                                ORDER BY bs.balanceSheetID;
                             """;

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

                    if (lastClassificationId == null ||
                        (classificationId != lastClassificationId && classificationId != 0))
                    {
                        result.Add(new BalanceSheetRow
                        {
                            AccountId = $"{classificationName}",
                            InitialDebit = string.Empty,
                            InitialCredit = string.Empty,
                            TurnoverDebit = string.Empty,
                            TurnoverCredit = string.Empty,
                            EndingDebit = string.Empty,
                            EndingCredit = string.Empty
                        });

                        lastClassificationId = classificationId;
                    }

                    result.Add(new BalanceSheetRow
                    {
                        AccountId = accountId == 0 ? "БАЛАНС" :
                            accountId < 10 ? "ПО КЛАССУ" : accountId.ToString(),
                        InitialDebit = initialDebit.ToString(CultureInfo.InvariantCulture),
                        InitialCredit = initialCredit.ToString(CultureInfo.InvariantCulture),
                        TurnoverDebit = turnoverDebit.ToString(CultureInfo.InvariantCulture),
                        TurnoverCredit = turnoverCredit.ToString(CultureInfo.InvariantCulture),
                        EndingDebit = endingDebit.ToString(CultureInfo.InvariantCulture),
                        EndingCredit = endingCredit.ToString(CultureInfo.InvariantCulture)
                    });
                }
            }
        }

        connection.Close();

        return result;
    }

    public class BalanceSheetRow
    {
        public string? AccountId { get; set; }
        public string? InitialDebit { get; set; }
        public string? InitialCredit { get; set; }
        public string? TurnoverDebit { get; set; }
        public string? TurnoverCredit { get; set; }
        public string? EndingDebit { get; set; }
        public string? EndingCredit { get; set; }
    }
}

public class StartsWithConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string accountId && parameter is string prefix) return accountId.StartsWith(prefix);
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return false;
    }
}

public class TwoDigitAccountIdConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string accountId) return accountId.Length == 2 && int.TryParse(accountId, out _);
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return false;
    }
}