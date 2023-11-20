using System.Text.Json;
using Unleash.Serialization;

namespace PiBox.Plugins.Management.Unleash
{
    public class SystemTextSerializer : IJsonSerializer
    {
        private readonly JsonSerializerOptions _options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public T Deserialize<T>(Stream stream)
        {
            // empty file?
            if (stream.Length == 3)
            {
                // edge case if the cache file for the toggle collection is null/empty
                var buffer = new byte[3];
                stream.ReadExactly(buffer, 0, 3);
                if (buffer[0] == 239 && buffer[1] == 187 && buffer[2] == 191)
                {
                    return JsonSerializer.Deserialize<T>("{}");
                }
            }

            return JsonSerializer.Deserialize<T>(stream, _options);
        }

        public void Serialize<T>(Stream stream, T instance)
        {
            JsonSerializer.Serialize<T>(stream, instance, _options);
            stream.Position = 0;
        }
    }
}
