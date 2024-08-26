using System.Data;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using ExcelDataReader;
using Npgsql;

namespace B1Task.ExcelReader;

public class ExcelParser(NpgsqlConnection connection)
{
    public void Import(string filePath)
    {
        var dataSet = ReadExcelFile(filePath);

        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            var sheet = dataSet.Tables[0];

            var sheetName = sheet.Rows[1].ItemArray[0] + " " + sheet.Rows[2].ItemArray[0] + " " +
                            sheet.Rows[3].ItemArray[0];

            var bankName = sheet.Rows[0].ItemArray[0]?.ToString();
            if (string.IsNullOrEmpty(bankName)) throw new NullReferenceException("Bank name is empty");

            var periodString = sheet.Rows[2].ItemArray[0]?.ToString();
            if (string.IsNullOrEmpty(periodString) || !periodString.Contains("за период с"))
                throw new FormatException("Invalid period string");

            var (periodStart, periodEnd) = GetPeriodsFromString(periodString);

            var bankId = InsertBank(connection, bankName);
            var fileId = InsertUploadedFile(connection, Path.GetFileName(filePath), DateTime.Now, periodStart,
                periodEnd, sheetName, bankId);

            var classificationId = 0;
            InsertClassification(connection, classificationId, string.Empty);


            for (var i = 8; i < sheet.Rows.Count; i++)
            {
                var row = sheet.Rows[i];
                var firstCell = row[0].ToString()!;
                if (!IsValidRow(row))
                    if (IsClassificationRow(row))
                    {
                        (classificationId, var classificationName) = GetClassificationFromString(firstCell);
                        InsertClassification(connection, classificationId, classificationName);
                    }

                int accountId;
                if (firstCell.Contains("по классу", StringComparison.InvariantCultureIgnoreCase))
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
                    if (!int.TryParse(firstCell, out accountId)) continue;
                }

                CreateAccount(connection, accountId);


                InsertBalanceSheet(
                    connection,
                    classificationId,
                    accountId,
                    bankId,
                    fileId,
                    Convert.ToDecimal(row[1]),
                    Convert.ToDecimal(row[2]),
                    Convert.ToDecimal(row[3]),
                    Convert.ToDecimal(row[4]),
                    Convert.ToDecimal(row[5]),
                    Convert.ToDecimal(row[6]),
                    "RUB"
                );
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        finally
        {
            connection.Close();
        }
    }

    private DataSet ReadExcelFile(string filePath)
    {
        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var result = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration
            {
                UseHeaderRow = false
            }
        });

        return result;
    }

    private static bool IsClassificationRow(DataRow row)
    {
        var rowString = row[0].ToString()!;
        if (!rowString.Contains("класс", StringComparison.InvariantCultureIgnoreCase)) return false;

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
        var pattern = @"\d{2}\.\d{2}\.\d{4}";
        var matches = Regex.Matches(periodString, pattern);

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
        return (row[0] is string || row[0] is double) && row[1] is double && row[2] is double && row[3] is double &&
               row[4] is double &&
               row[5] is double && row[6] is double;
    }

    private void InsertClassification(NpgsqlConnection connection, int classificationId, string classificationName)
    {
        var query =
            "INSERT INTO balancesheetschema.classifications (classificationid, classificationname) values (@classificationId, @classificationName) ON CONFLICT DO NOTHING ";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("classificationId", classificationId);
        command.Parameters.AddWithValue("classificationName", classificationName);
        command.ExecuteNonQuery();
    }

    private static int InsertBank(NpgsqlConnection connection, string bankName)
    {
        const string query = "INSERT INTO BalanceSheetSchema.Banks (BankName) VALUES (@bankName) RETURNING BankID;";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("bankName", bankName);
        return (int)command.ExecuteScalar()!;
    }

    private static int InsertUploadedFile(NpgsqlConnection connection, string fileName, DateTime dateUploaded,
        DateTime periodStart, DateTime periodEnd, string sheetName, int bankId)
    {
        const string query = """
                             INSERT INTO BalanceSheetSchema.UploadedFiles (FileName, DateUploaded, PeriodStart, PeriodEnd, sheetname, bankId) 
                             VALUES (@fileName, @dateUploaded, @periodStart, @periodEnd, @sheetName, @bankId) 
                             RETURNING FileID;
                             """;
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("fileName", fileName);
        command.Parameters.AddWithValue("dateUploaded", dateUploaded);
        command.Parameters.AddWithValue("periodStart", periodStart);
        command.Parameters.AddWithValue("periodEnd", periodEnd);
        command.Parameters.AddWithValue("sheetName", sheetName);
        command.Parameters.AddWithValue("bankId", bankId);
        return (int)command.ExecuteScalar()!;
    }

    private static void CreateAccount(NpgsqlConnection connection, int accountId)
    {
        const string insertQuery =
            "INSERT INTO BalanceSheetSchema.Accounts (accountid, accountname) VALUES (@accountId, @accountId) ON CONFLICT DO NOTHING RETURNING AccountID;";
        using var insertCommand = new NpgsqlCommand(insertQuery, connection);
        insertCommand.Parameters.AddWithValue("accountId", accountId);
        insertCommand.ExecuteNonQuery();
    }

    private static void InsertBalanceSheet(
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
        const string query = """
                             INSERT INTO BalanceSheetSchema.BalanceSheet (
                                 ClassificationID, AccountID, BankID, FileID, InitialDebit, InitialCredit, 
                                 TurnoverDebit, TurnoverCredit, EndingDebit, EndingCredit, Currency) 
                             VALUES (
                                 @classificationId, @accountId, @bankId, @fileId, @initialDebit, @initialCredit, 
                                 @turnoverDebit, @turnoverCredit, @endingDebit, @endingCredit, @currency);
                             """;
        using var command = new NpgsqlCommand(query, connection);
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