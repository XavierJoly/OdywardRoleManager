using System;
using System.Net;

namespace _0900_OdywardRoleManager.Services;

public sealed class GraphApiException : Exception
{
    public GraphApiException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
