// See https://aka.ms/new-console-template for more information

using ExcelDataReader;

System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

using var file = File.Open(@"C:\Users\tchau\Downloads\ОСВ для тренинга.xls", FileMode.Open, FileAccess.Read);
using var reader = ExcelReaderFactory.CreateReader(file);
var dataSet = reader.AsDataSet();
        
Console.WriteLine(dataSet.Tables[0].Columns[0].Table.Rows[0].ItemArray[0]);