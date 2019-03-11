using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace azure.Lib
{
    internal interface IHttp
    {
        Task SendRequestAndDecodeResponse(
            HttpMessageInvoker client, HttpRequestMessage request, Action<HttpStatusCode, JsonTextReader> handleJsonResponse,
            CancellationToken ct = default);

        Task SendRequestAndDecodeResponse(
            HttpMessageInvoker client, HttpRequestMessage request, Func<HttpStatusCode, JsonTextReader, Task> handleJsonResponse,
            CancellationToken ct = default);

        Task<T> SendRequestAndDecodeResponse<T>(
            HttpMessageInvoker client, HttpRequestMessage request, Func<HttpStatusCode, JsonTextReader, T> handleJsonResponse,
            CancellationToken ct = default);

        Task<T> SendRequestAndDecodeResponse<T>(
            HttpMessageInvoker client, HttpRequestMessage request, Func<HttpStatusCode, JsonTextReader, Task<T>> handleJsonResponse,
            CancellationToken ct = default);
    }
}
