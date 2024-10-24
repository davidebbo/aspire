// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Options;

internal sealed class TunnelConnectionListenerFactory(IOptions<TunnelOptions> options) : IConnectionListenerFactory
{
    private readonly TunnelOptions _options = options.Value;

    public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
    {
        return new(new TunnelConnectionListener(_options, endpoint));
    }
}
