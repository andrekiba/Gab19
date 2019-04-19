using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Gab.Functions
{
    public class StreamBinder
    {
        public Task<string> ReadFromStreamAsync(Stream input, CancellationToken cancellationToken)
        {
            if (input == null)
            {
                return Task.FromResult<string>(null);
            }

            using (var reader = new StreamReader(input))
            {
                return Task.FromResult(reader.ReadToEnd());
            }
        }

        public Task WriteToStreamAsync(string value, Stream output, CancellationToken cancellationToken)
        {
            using (var writer = new StreamWriter(output))
            {
                writer.Write(value);
            }

            return Task.CompletedTask;
        }
    }
}
