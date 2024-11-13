using AutoEncodeServer.Enums;

namespace AutoEncodeServer.Data.Request;

/// <summary>Generic request object for internal manager processing</summary>
public class ManagerRequest
{
    /// <summary>The type of request.</summary>
    public ManagerRequestType Type { get; set; }
}

/// <summary>Extends <see cref="ManagerRequest"/> by adding a request data payload.</summary>
/// <typeparam name="T">The type of request data.</typeparam>
public class ManagerRequest<T> : ManagerRequest
{
    /// <summary>Additional data needed to process the request.</summary>
    public T RequestData { get; set; }
}

