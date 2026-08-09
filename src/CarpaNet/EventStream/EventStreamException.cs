using System;

namespace CarpaNet.EventStream;

/// <summary>
/// Exception thrown when an error occurs in an event stream.
/// </summary>
public sealed class EventStreamException : Exception
{
    /// <summary>
    /// The error name from the event stream.
    /// </summary>
    public string? ErrorName { get; }

    /// <summary>
    /// The error header from the stream.
    /// </summary>
    public EventStreamHeader? Header { get; }

    /// <summary>
    /// Creates a new EventStreamException.
    /// </summary>
    /// <param name="message">The error message.</param>
    public EventStreamException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new EventStreamException.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public EventStreamException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates a new EventStreamException from an error header.
    /// </summary>
    /// <param name="header">The error header.</param>
    public EventStreamException(EventStreamHeader header)
        : base(FormatMessage(header))
    {
        Header = header;
        ErrorName = header.Error;
    }

    private static string FormatMessage(EventStreamHeader header)
    {
        if (!string.IsNullOrEmpty(header.Message))
            return string.IsNullOrEmpty(header.Error) ? header.Message! : $"[{header.Error}] {header.Message}";
        if (!string.IsNullOrEmpty(header.Error))
            return header.Error!;
        return $"Unknown event stream error (op={header.Op})";
    }

    /// <summary>
    /// Creates a new EventStreamException with error name and message.
    /// </summary>
    /// <param name="errorName">The error name.</param>
    /// <param name="message">The error message.</param>
    public EventStreamException(string errorName, string message) : base(message)
    {
        ErrorName = errorName;
    }
}
