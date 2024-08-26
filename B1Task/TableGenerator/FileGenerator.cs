using System.Globalization;
using System.IO;
using System.Text;

namespace B1Task.TableGenerator;

public abstract class FileGenerator
{
    private static readonly ThreadLocal<Random> RandomWrapper = new(() => new Random());

    public static async Task GenerateFilesAsync(string folderPath, int fileCount, int linesPerFile,
        IProgress<int> progress)
    {
        await Task.Run(() =>
        {
            Parallel.For(0, fileCount, i =>
            {
                var fileName = Path.Combine(folderPath, $"file_{i + 1}.txt");
                using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, 4096,
                           FileOptions.SequentialScan))
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    for (var j = 0; j < linesPerFile; j++) writer.WriteLine(GenerateLine());
                }

                progress.Report(1);
            });
        });
    }

    private static string GenerateLine()
    {
        var resultArray = new List<string>();
        var random = RandomWrapper.Value!;
        resultArray.Add(GenerateRandomDate(random).ToString("dd.MM.yyyy"));
        resultArray.Add(GenerateRandomString(10, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ", random));
        resultArray.Add(GenerateRandomString(10, "абвгдеёжзийклмнопрстуфхцчшщъыьэюяАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ",
            random));
        resultArray.Add((random.Next(1, 50000000) * 2).ToString());
        resultArray.Add(Math.Round(random.NextDouble() * (20 - 1) + 1, 8).ToString(CultureInfo.InvariantCulture));

        return string.Join("||", resultArray);
    }

    private static DateTime GenerateRandomDate(Random random)
    {
        var start = new DateTime(DateTime.Now.Year - 5, 1, 1);
        var range = (DateTime.Now - start).Days;
        return start.AddDays(random.Next(range));
    }

    private static string GenerateRandomString(int length, string chars, Random random)
    {
        var result = new char[length];
        for (var i = 0; i < length; i++) result[i] = chars[random.Next(chars.Length)];
        return new string(result);
    }
}