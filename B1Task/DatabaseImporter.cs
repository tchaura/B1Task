using System.Globalization;
using System.IO;
using Npgsql;
using System.Threading.Tasks.Dataflow;

namespace B1Task
{
    public class DatabaseImporter(string connectionString)
    {
        const int BatchSize = 10000;

        public async Task ImportDataAsync(string filePath, IProgress<int> progress)
        {
            var block = new ActionBlock<List<string>>(async lines =>
            {
                await InsertBatchAsync(lines, progress);
            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            });

            using (var reader = new StreamReader(filePath))
            {
                string line;
                var batch = new List<string>();

                while ((line = (await reader.ReadLineAsync())!) != null)
                {
                    batch.Add(line);
                    if (batch.Count >= BatchSize)
                    {
                        await block.SendAsync(batch);
                        batch = new List<string>();
                    }
                }

                if (batch.Count > 0)
                {
                    await block.SendAsync(batch);
                }
            }

            block.Complete();
            await block.Completion;
        }

        private async Task InsertBatchAsync(List<string> lines, IProgress<int> progress)
        {
            const string copyCommand = "COPY DataTable (date, latintext, cyrillictext, evennumber, floatingnumber) FROM STDIN (FORMAT BINARY)";
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            await using var writer = await conn.BeginBinaryImportAsync(copyCommand);
            foreach (var line in lines)
            {
                var parts = line.Split("||", StringSplitOptions.None);

                await writer.StartRowAsync();
                await writer.WriteAsync(DateTime.ParseExact(parts[0], "dd.MM.yyyy", CultureInfo.InvariantCulture),
                    NpgsqlTypes.NpgsqlDbType.Date);
                await writer.WriteAsync(parts[1], NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(parts[2], NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(long.Parse(parts[3]), NpgsqlTypes.NpgsqlDbType.Integer);
                await writer.WriteAsync(decimal.Parse(parts[4], CultureInfo.InvariantCulture),
                    NpgsqlTypes.NpgsqlDbType.Numeric);
            }

            await writer.CompleteAsync();
            progress.Report(BatchSize);

        }
    }
}
