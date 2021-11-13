using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Marstris.Core.Communication
{
    public static class StreamReaderExtensions
    {
        public static async Task<T> ReadAsync<T>(this StreamReader reader)
        {
            var line = await reader.ReadLineAsync();
            return JsonSerializer.Deserialize<T>(line);
        }
        
        public static T Read<T>(this StreamReader reader)
        {
            var line = reader.ReadLine();
            return JsonSerializer.Deserialize<T>(line);
        }
    }
    
    public static class StreamWriterExtensions
    {
        public static async Task WriteAndFlushAsync(this StreamWriter writer, object o)
        {
            var json = JsonSerializer.Serialize(o);
            await writer.WriteLineAsync(json);
            await writer.FlushAsync();
        }
        
        public static void WriteAndFlush(this StreamWriter writer, object o)
        {
            var json = JsonSerializer.Serialize(o);
            writer.WriteLine(json);
            writer.Flush();
        }
        
        public static async Task WriteLineAndFlushAsync(this StreamWriter writer, string line)
        {
            await writer.WriteLineAsync(line);
            await writer.FlushAsync();
        }
    }
}