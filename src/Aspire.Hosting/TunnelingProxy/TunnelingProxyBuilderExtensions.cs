// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.ReverseProxyTunnel;

internal static class TunnelingProxyBuilderExtensions
{
    internal static void SetUpReverseProxyTunnel(this IDistributedApplicationBuilder distributedAppBuilder, IHostApplicationBuilder hostAppBuilder, TunnelingProxyConfiguration config)
    {
        hostAppBuilder.Services.AddSingleton<TunnelingProxyManager>(_ => new TunnelingProxyManager(distributedAppBuilder, config));
        hostAppBuilder.Services.AddSingleton<TunnelingProxyBackendServiceHost>();
        hostAppBuilder.Services.AddHostedService(sp => sp.GetRequiredService<TunnelingProxyBackendServiceHost>());
    }
}
