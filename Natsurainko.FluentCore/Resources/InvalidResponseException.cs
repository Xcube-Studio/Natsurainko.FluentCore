﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Resources;

/// <summary>
/// Exception thrown when the response from the server is invalid.
/// </summary>
public class InvalidResponseException : Exception
{
    public required string Url { get; init; }
    public string? Response { get; init; }

    public InvalidResponseException() { }

    [SetsRequiredMembers]
    public InvalidResponseException(
        string url,
        string? resposne,
        string? message = null,
        Exception? innerException = null
        )
        : base(message, innerException)
    {
        Url = url;
        Response = resposne;
    }
}
