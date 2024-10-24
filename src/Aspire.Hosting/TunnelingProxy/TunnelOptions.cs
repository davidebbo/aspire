// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal sealed class TunnelOptions
{
    public int MaxConnectionCount { get; set; } = 10;

    public TransportType Transport { get; set; } = TransportType.HTTP2;
}

internal enum TransportType
{
    WebSockets,
    HTTP2
}
