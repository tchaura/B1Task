// See https://aka.ms/new-console-template for more information

using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using ExcelDataReader;
using Npgsql;

System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

var balanceSheetImporter =
    new BalanceSheetImporter("Server=127.0.0.1;Port=5432;Database=postgres;UserId=postgres;Password=admin; Include Error Detail=true;");
balanceSheetImporter.Import(@"C:\Users\tchau\Downloads\ОСВ для тренинга.xls");

public class BalanceSheetImporter
{
    private readonly string _connectionString;

    public BalanceSheetImporter(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void Import(string filePath)
    {
        // Step 1: Read the XLS file
        DataSet dataSet = ReadExcelFile(filePath);

        // Step 2: Process the data and insert it into the database
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    DataTable sheet = dataSet.Tables[0];
                    
                    var periodString = sheet.Rows[2].ItemArray[0]?.ToString();
                    if (string.IsNullOrEmpty(periodString) || !periodString.Contains("за период с"))
                    {
                        throw new FormatException("Invalid period string");
                    }
                    var (periodStart, periodEnd) = GetPeriodsFromString(periodString);

                    int fileId = InsertUploadedFile(connection, filePath, DateTime.Now, periodStart, periodEnd);
                    int bankId = InsertBank(connection, "Bank Name");

                    int classificationId = 0;
                    CreateClassification(connection, classificationId, string.Empty);


                    for (var i = 8; i < sheet.Rows.Count; i++)
                    {
                        var row = sheet.Rows[i];
                        var firstCell = row[0].ToString()!;
                        if (!IsValidRow(row))
                        {
                            if (IsClassificationRow(row))
                            {
                                (classificationId, var classificationName) = GetClassificationFromString(firstCell);
                                CreateClassification(connection, classificationId, classificationName);
                            }
                        }

                        int accountId;
                        if (firstCell.Contains ("по классу", StringComparison.InvariantCultureIgnoreCase))
                        {
                            accountId = classificationId;
                        }
                        else if (firstCell.Contains("баланс", StringComparison.InvariantCultureIgnoreCase))
                        {
                            classificationId = 0;
                            accountId = 0;
                        }
                        else
                        {
                            if (!int.TryParse(firstCell, out accountId))
                            {
                                continue;
                            }
                        }
                        CreateAccount(connection, accountId);
                        
                        
                        InsertBalanceSheet(
                            connection, 
                            classificationId,
                            accountId, 
                            bankId, 
                            fileId, 
                            Convert.ToDecimal(row[1]), // Initial Debit
                            Convert.ToDecimal(row[2]), // Initial Credit
                            Convert.ToDecimal(row[3]), // Turnover Debit
                            Convert.ToDecimal(row[4]), // Turnover Credit
                            Convert.ToDecimal(row[5]), // Ending Debit
                            Convert.ToDecimal(row[6]), // Ending Credit
                            "RUB" // Assuming currency, replace if necessary
                        );
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
    
    private DataSet ReadExcelFile(string filePath)
    {
        using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
        {
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = false
                    }
                });

                return result;
            }
        }
    }

    private static bool IsClassificationRow(DataRow row)
    {
        var rowString = row[0].ToString()!;
        if (!rowString.Contains("класс", StringComparison.InvariantCultureIgnoreCase))
        {
            return false;
        }
        return true;
    }

    private (int, string) GetClassificationFromString(string rowString)
    {
        const string pattern = @"\bКЛАСС\s+(\d+)\b";
        var match = Regex.Match(rowString, pattern);
        var classificationId = int.Parse(match.Groups[1].Value);
        var classificationName = rowString;
        
        return (classificationId, classificationName);
    }
    
    private (DateTime, DateTime) GetPeriodsFromString(string periodString)
    {
        DateTime periodStart;
        DateTime periodEnd;
        string pattern = @"\d{2}\.\d{2}\.\d{4}";
        MatchCollection matches = Regex.Matches(periodString, pattern);

        if (matches.Count == 2)
        {
            periodStart = DateTime.ParseExact(matches[0].Value, "dd.MM.yyyy", CultureInfo.InvariantCulture);
            periodEnd = DateTime.ParseExact(matches[1].Value, "dd.MM.yyyy", CultureInfo.InvariantCulture);
        }
        else
        {
            throw new FormatException("Invalid period string");
        }

        return (periodStart, periodEnd);
    }

    private bool IsValidRow(DataRow row)
    {
        return (row[0] is string || row[0] is double) && row[1] is double && row[2] is double && row[3] is double && row[4] is double &&
               row[5] is double && row[6] is double;
    }

    private void CreateClassification(NpgsqlConnection connection, int classificationId, string classificationName)
    {
        string query =
            "INSERT INTO balancesheetschema.classifications (classificationid, classificationname) values (@classificationId, @classificationName) ON CONFLICT DO NOTHING ";
        using (var command = new NpgsqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("classificationId", classificationId);
            command.Parameters.AddWithValue("classificationName", classificationName);
            command.ExecuteNonQuery();
        }
    }

    private int InsertBank(NpgsqlConnection connection, string bankName)
    {
        string query = "INSERT INTO BalanceSheetSchema.Banks (BankName) VALUES (@bankName) RETURNING BankID;";
        using (var command = new NpgsqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("bankName", bankName);
            return (int)command.ExecuteScalar();
        }
    }

    private int InsertUploadedFile(NpgsqlConnection connection, string fileName, DateTime dateUploaded, DateTime periodStart, DateTime periodEnd)
    {
        string query = @"
            INSERT INTO BalanceSheetSchema.UploadedFiles (FileName, DateUploaded, PeriodStart, PeriodEnd) 
            VALUES (@fileName, @dateUploaded, @periodStart, @periodEnd) 
            RETURNING FileID;";
        using (var command = new NpgsqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("fileName", fileName);
            command.Parameters.AddWithValue("dateUploaded", dateUploaded);
            command.Parameters.AddWithValue("periodStart", periodStart);
            command.Parameters.AddWithValue("periodEnd", periodEnd);
            return (int)command.ExecuteScalar();
        }
    }

    private void CreateAccount(NpgsqlConnection connection, int accountId)
    {
        // string selectQuery = "SELECT AccountID FROM BalanceSheetSchema.Accounts WHERE accountid = @accountId;";
        // using (var selectCommand = new NpgsqlCommand(selectQuery, connection))
        // {
            // selectCommand.Parameters.AddWithValue("accountId", accountId);
            // var result = selectCommand.ExecuteScalar();
            //
            // if (result != null)
            // {
            //     return (int)result;
            // }
            // else
            // {
                string insertQuery = "INSERT INTO BalanceSheetSchema.Accounts (accountid, accountname) VALUES (@accountId, @accountId) ON CONFLICT DO NOTHING RETURNING AccountID;";
                using (var insertCommand = new NpgsqlCommand(insertQuery, connection))
                {
                    insertCommand.Parameters.AddWithValue("accountId", accountId);
                    insertCommand.ExecuteNonQuery();
                    // return (int)insertCommand.ExecuteScalar()!;
                }
            // }
        // }
    }

    private void InsertBalanceSheet(
        NpgsqlConnection connection,
        int classificationId,
        int accountId,
        int bankId,
        int fileId,
        decimal initialDebit,
        decimal initialCredit,
        decimal turnoverDebit,
        decimal turnoverCredit,
        decimal endingDebit,
        decimal endingCredit,
        string currency)
    {
        string query = @"
            INSERT INTO BalanceSheetSchema.BalanceSheet (
                ClassificationID, AccountID, BankID, FileID, InitialDebit, InitialCredit, 
                TurnoverDebit, TurnoverCredit, EndingDebit, EndingCredit, Currency) 
            VALUES (
                @classificationId, @accountId, @bankId, @fileId, @initialDebit, @initialCredit, 
                @turnoverDebit, @turnoverCredit, @endingDebit, @endingCredit, @currency);";
        using (var command = new NpgsqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("classificationId", classificationId);
            command.Parameters.AddWithValue("accountId", accountId);
            command.Parameters.AddWithValue("bankId", bankId);
            command.Parameters.AddWithValue("fileId", fileId);
            command.Parameters.AddWithValue("initialDebit", initialDebit);
            command.Parameters.AddWithValue("initialCredit", initialCredit);
            command.Parameters.AddWithValue("turnoverDebit", turnoverDebit);
            command.Parameters.AddWithValue("turnoverCredit", turnoverCredit);
            command.Parameters.AddWithValue("endingDebit", endingDebit);
            command.Parameters.AddWithValue("endingCredit", endingCredit);
            command.Parameters.AddWithValue("currency", currency);

            command.ExecuteNonQuery();
        }
    }
}
