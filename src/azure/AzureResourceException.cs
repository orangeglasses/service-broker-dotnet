using System;
using System.Net;

namespace azure.resources
{
    public class AzureResourceException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string ErrorContent { get; }

        public AzureResourceException(string message, HttpStatusCode statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public AzureResourceException(string message, HttpStatusCode statusCode, string errorContent)
            : this(message, statusCode)
        {
            ErrorContent = errorContent;
        }
    }
}
