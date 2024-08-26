using System.IO;
using System.Text;

namespace B1Task.TableGenerator;

public class FileMerger
{
    public async Task<int> MergeFiles(string sourceFolderPath, string destinationFilePath, string? filter,
        IProgress<int>? progress)
    {
        var removedCount = 0;

        await Task.Run(() =>
        {
            using (var writer = new StreamWriter(destinationFilePath, false, Encoding.UTF8))
            {
                foreach (var filePath in Directory.GetFiles(sourceFolderPath, "*.txt"))
                {
                    foreach (var line in File.ReadLines(filePath))
                    {
                        if (!string.IsNullOrEmpty(filter) && line.Contains(filter))
                        {
                            removedCount++;
                            continue;
                        }

                        writer.WriteLine(line);
                    }

                    progress?.Report(1);
                }
            }
        });
        return removedCount;
    }
}