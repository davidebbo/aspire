// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Configuration;

namespace Aspire.Hosting.ReverseProxyTunnel;

internal sealed class TunnelingProxyBackendServiceHost : IHostedService
{
    private readonly WebApplication? _app;
    private readonly ILogger<TunnelingProxyBackendServiceHost> _logger;
    private readonly TunnelingProxyManager _tunnelManager;

    public TunnelingProxyBackendServiceHost(
        ILoggerFactory loggerFactory,
        TunnelingProxyManager tunnelManager)
    {
        _logger = loggerFactory.CreateLogger<TunnelingProxyBackendServiceHost>();
        _tunnelManager = tunnelManager;

        var builder = WebApplication.CreateSlimBuilder();

        builder.Services.AddReverseProxy()
               .LoadFromMemory(GetRouteConfigList(), GetClusterConfigList());

        // This is the HTTP/2 endpoint to register this app as part of the cluster endpoint
        var url = $"https://localhost:{_tunnelManager.FrontendControlPort}/connect-h2?host=backend1.app";
        builder.WebHost.UseTunnelTransport(url);

        // Hack to avoid some odd port conflict. Without it, we get:
        // "Failed to bind to address https://127.0.0.1:15887: address already in use"
        // Where 15887 is the apphost launchSettings port
        builder.WebHost.UseUrls("http://localhost");

        _app = builder.Build();

        _app.MapReverseProxy();
    }

    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        if (_app is not null)
        {
            await _app.StartAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    async Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        if (_app is not null)
        {
            await _app.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    IReadOnlyList<RouteConfig> GetRouteConfigList()
    {
        return _tunnelManager.TunneledEndpoints.Select(kvp => GetRouteConfig(kvp.Value, kvp.Value)).ToList();
    }

    static RouteConfig GetRouteConfig(int num, int port)
    {
        return new RouteConfig
        {
            RouteId = $"route{num}",
            ClusterId = $"cluster{num}",
            Match = new RouteMatch
            {
                Path = "{**catch-all}",
                Headers =
                [
                    new RouteHeader
                    {
                        Name = "X-Forwarded-Host",
                        Values = [$"localhost:{port}", $"reverseproxytunnelfrontend:{port}"]
                    }
                ]
            }
        };
    }

    private IReadOnlyList<ClusterConfig> GetClusterConfigList()
    {
        return _tunnelManager.TunneledEndpoints.Select(kvp => GetClusterConfig(kvp.Value, kvp.Key)).ToList();
    }

    static ClusterConfig GetClusterConfig(int num, EndpointAnnotation endpoint)
    {
        return new ClusterConfig
        {
            ClusterId = $"cluster{num}",
            Destinations = new Dictionary<string, DestinationConfig>
                {
                    {
                        "cluster{num}/destination1",
                        new DestinationConfig
                        {
                            Address = $"{endpoint.UriScheme}://localhost:{endpoint.Port}/"
                        }
                    }
                }
        };
    }
}
