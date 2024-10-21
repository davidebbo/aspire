// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.ReverseProxyTunnel;
internal static class ReverseProxyBuilderExtensions
{
    internal static void SetUpReverseProxyTunnel(this IDistributedApplicationBuilder distributedAppBuilder, IHostApplicationBuilder hostAppBuilder)
    {
        var tunnelConfig = new TunnelProxyConfig();

        foreach (var resource in distributedAppBuilder.Resources)
        {
            foreach (var endpointReferenceAnnotation in resource.Annotations.OfType<EndpointReferenceAnnotation>())
            {
                if (resource.IsContainer() && !endpointReferenceAnnotation.Resource.IsContainer())
                {
                    Console.WriteLine(resource.Name + "   " + endpointReferenceAnnotation.Resource.Name);
                    foreach (var endpointAnnotation in endpointReferenceAnnotation.Resource.Annotations.OfType<EndpointAnnotation>())
                    {
                        if (endpointReferenceAnnotation.UseAllEndpoints || endpointReferenceAnnotation.EndpointNames.Contains(endpointAnnotation.Name))
                        {
                            if (!tunnelConfig.TunneledEndpoints.ContainsKey(endpointAnnotation))
                            {
                                tunnelConfig.TunneledEndpoints.Add(endpointAnnotation, tunnelConfig.AllocatePort());
                            }
                        }
                    }
                }
            }
        }

        var tunnelFrontEndBuilder = distributedAppBuilder.AddContainer(tunnelConfig.FrontendContainerName, tunnelConfig.FrontendContainerImage)
            .WithHttpsEndpoint(name: "tunnel", targetPort: tunnelConfig.FrontendControlPort, isProxied: false)
            .WithContainerRuntimeArgs("-e", $"ASPNETCORE_HTTPS_PORTS={tunnelConfig.FrontendControlPort}")
            .WithContainerRuntimeArgs("-e", $"ASPNETCORE_Kestrel__Certificates__Default__Path={tunnelConfig.PfxContainerPath}")
            .WithContainerRuntimeArgs("-e", $"ASPNETCORE_Kestrel__Certificates__Default__Password={tunnelConfig.PfxPassword}")
            .WithContainerRuntimeArgs("-v", $"{tunnelConfig.PfxLocalFolder}:{tunnelConfig.PfxContainerFolder}")
            ;

        foreach (var entry in tunnelConfig.TunneledEndpoints)
        {
            tunnelFrontEndBuilder.WithHttpEndpoint(name: $"ep{entry.Value}", targetPort: entry.Value, isProxied: false);
        }

        // Create a semicolon separated list of the tunneled endpoint ports
        var ports = string.Join(";", tunnelConfig.TunneledEndpoints.Values.Select(p => p.ToString(CultureInfo.InvariantCulture)));
        tunnelFrontEndBuilder.WithContainerRuntimeArgs("-e", $"ASPNETCORE_HTTP_PORTS={ports}");

        hostAppBuilder.Services.AddSingleton<TunnelProxyConfig>(_ => tunnelConfig);
        hostAppBuilder.Services.AddSingleton<ReverseProxyTunnelBackendServiceHost>();
        hostAppBuilder.Services.AddHostedService(sp => sp.GetRequiredService<ReverseProxyTunnelBackendServiceHost>());
    }

}

class TunnelProxyConfig
{
    public TunnelProxyConfig()
    {
        FrontendControlPort = AllocatePort();
    }

    public int FrontendControlPort { get; }
    public string PfxLocalFolder { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aspnet", "https");
    public string PfxContainerFolder { get; } = "/https/";
    // HACK: should not hard code the pfx file name
    public string PfxContainerPath { get => PfxContainerFolder + "aspnetapp.pfx"; }
    // HACK: come up with proper Pfx password management
    public string PfxPassword { get; } = "qqq";
    public string FrontendContainerName { get; } = "reverseproxytunnelfrontend";
    public string FrontendContainerImage { get; } = "aspirereverseproxytunnelfrontend";

    public Dictionary<EndpointAnnotation, int> TunneledEndpoints = [];

    public int MapPort(int port) => TunneledEndpoints.Single(entry => entry.Key.Port == port).Value;

    // HACK: need to use a proper port allocation mechanism
    int _port = 57500;
    public int AllocatePort()
    {
        return _port++;
    }
}
