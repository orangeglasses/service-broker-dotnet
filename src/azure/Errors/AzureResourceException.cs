using System;
using System.Net;

namespace azure.Errors
{
    public class AzureResourceException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public Error Error { get; }

        public AzureResourceException(string message, HttpStatusCode statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public AzureResourceException(string message, HttpStatusCode statusCode, Error error)
            : this(message, statusCode)
        {
            Error = error;
        }
    }
}
