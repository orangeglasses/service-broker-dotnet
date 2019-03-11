using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace azure.Lib
{
    internal class Http : IHttp
    {
        public async Task SendRequestAndDecodeResponse(
            HttpMessageInvoker client, HttpRequestMessage request, Action<HttpStatusCode, JsonTextReader> handleJsonResponse,
            CancellationToken ct = default)
        {
            using (var response = await client.SendAsync(request, ct))
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var streamReader = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                handleJsonResponse(response.StatusCode, jsonTextReader);
            }
        }

        public async Task SendRequestAndDecodeResponse(
            HttpMessageInvoker client, HttpRequestMessage request, Func<HttpStatusCode, JsonTextReader, Task> handleJsonResponse,
            CancellationToken ct = default)
        {
            using (var response = await client.SendAsync(request, ct))
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var streamReader = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                await handleJsonResponse(response.StatusCode, jsonTextReader);
            }
        }

        public async Task<T> SendRequestAndDecodeResponse<T>(
            HttpMessageInvoker client, HttpRequestMessage request, Func<HttpStatusCode, JsonTextReader, T> handleJsonResponse,
            CancellationToken ct = default)
        {
            using (var response = await client.SendAsync(request, ct))
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var streamReader = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                return handleJsonResponse(response.StatusCode, jsonTextReader);
            }
        }

        public async Task<T> SendRequestAndDecodeResponse<T>(
            HttpMessageInvoker client, HttpRequestMessage request, Func<HttpStatusCode, JsonTextReader, Task<T>> handleJsonResponse,
            CancellationToken ct = default)
        {
            using (var response = await client.SendAsync(request, ct))
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var streamReader = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                return await handleJsonResponse(response.StatusCode, jsonTextReader);
            }
        }
    }
}
