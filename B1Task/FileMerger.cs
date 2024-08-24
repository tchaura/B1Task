using System.IO;
using System.Text;

namespace B1Task
{
    public class FileMerger
    {
        public async Task<int> MergeFiles(string sourceFolderPath, string destinationFilePath, string? filter, IProgress<int>? progress)
        {
            int removedCount = 0;

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

        public async Task<int> MergeFilesAsync(string sourceFolderPath, string destinationFilePath, string filter = null, IProgress<int> progress = null)
        {
            int removedCount = 0;
            var filePaths = Directory.GetFiles(sourceFolderPath, "*.txt");
            var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            await Task.Run(() =>
            {
                Parallel.ForEach(filePaths, options, filePath =>
                {
                    var localRemovedCount = 0;
                    var tempLines = new List<string>();

                    foreach (var line in File.ReadLines(filePath))
                    {
                        if (!string.IsNullOrEmpty(filter) && line.Contains(filter))
                        {
                            localRemovedCount++;
                        }
                        else
                        {
                            tempLines.Add(line);
                        }
                    }

                    lock (destinationFilePath)
                    {
                        using (var writer = new StreamWriter(destinationFilePath, true, Encoding.UTF8, 4096))
                        {
                            foreach (var tempLine in tempLines)
                            {
                                writer.WriteLine(tempLine);
                            }
                        }
                    }

                    Interlocked.Add(ref removedCount, localRemovedCount);
                    progress?.Report(1);
                });
            });

            return removedCount;
        }


    }
}
