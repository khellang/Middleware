using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ProblemDetails.Tests.Helpers
{
    public static class JsonHttpExtensions
    {
        private static readonly MediaTypeHeaderValue DefaultApplicationJsonMediaType = new MediaTypeHeaderValue("application/json");

        private static MediaTypeHeaderValue ApplicationJsonMediaType => DefaultApplicationJsonMediaType.Clone();

        public static Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient client, string requestUri, T value, JsonSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            return client.PostAsync(requestUri, new JsonObjectContent<T>(value, options), cancellationToken);
        }

        public static Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient client, Uri requestUri, T value, JsonSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            return client.PostAsync(requestUri, new JsonObjectContent<T>(value, options), cancellationToken);
        }

        public static Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient client, string requestUri, T value, JsonSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            return client.PutAsync(requestUri, new JsonObjectContent<T>(value, options), cancellationToken);
        }

        public static Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient client, Uri requestUri, T value, JsonSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            return client.PutAsync(requestUri, new JsonObjectContent<T>(value, options), cancellationToken);
        }

        public static ValueTask<object> ReadJsonAsync(this HttpContent content, Type type, JsonSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            return content.ReadJsonAsync<object>(type, options, cancellationToken);
        }

        public static ValueTask<T> ReadJsonAsync<T>(this HttpContent content, JsonSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            return content.ReadJsonAsync<T>(typeof(T), options, cancellationToken);
        }

        private static ValueTask<T> ReadJsonAsync<T>(this HttpContent content, Type type, JsonSerializerOptions options, CancellationToken cancellationToken)
        {
            if (content is JsonObjectContent<T> jsonContent)
            {
                return new ValueTask<T>(jsonContent.Value);
            }

            static async ValueTask<T> Awaited(HttpContent c, Type t, JsonSerializerOptions o, CancellationToken ct)
            {
                var stream = await c.ReadAsStreamAsync();
                return (T) await JsonSerializer.DeserializeAsync(stream, t, o, ct);
            }

            return Awaited(content, type, options, cancellationToken);
        }

        private static T Clone<T>(this T value) where T : ICloneable => (T) value.Clone();

        private sealed class JsonObjectContent<T> : HttpContent
        {
            public JsonObjectContent(T value, JsonSerializerOptions options)
            {
                Value = value;
                Options = options;
                Headers.ContentType = ApplicationJsonMediaType;
                Headers.ContentType.CharSet = Encoding.UTF8.WebName;
            }

            public T Value { get; }

            private JsonSerializerOptions Options { get; }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                return JsonSerializer.SerializeAsync(stream, Value, Options);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = -1;
                return false;
            }
        }
    }
}
