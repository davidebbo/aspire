// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting;
using Aspire.Hosting.ReverseProxyTunnel;

class TunnelingProxyManager
{
    const string FrontendContainerImage = "aspirereverseproxytunnelfrontend";
    public const string FrontendContainerName = "reverseproxytunnelfrontend";

    public TunnelingProxyManager(IDistributedApplicationBuilder distributedAppBuilder, TunnelingProxyConfiguration config)
    {
        FrontendControlPort = AllocatePort();

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
                            if (!TunneledEndpoints.ContainsKey(endpointAnnotation))
                            {
                                TunneledEndpoints.Add(endpointAnnotation, AllocatePort());
                            }
                        }
                    }
                }
            }
        }

        var pfxFileName = Path.GetFileName(config.PfxPath);
        var pfxFolder = Path.GetDirectoryName(config.PfxPath);
        var pfxContainerFolder = "/https/";
        var pfxContainerPath = $"{pfxContainerFolder}{pfxFileName}";

        var tunnelFrontEndBuilder = distributedAppBuilder.AddContainer(FrontendContainerName, FrontendContainerImage)
            .WithHttpsEndpoint(name: "tunnel", targetPort: FrontendControlPort, isProxied: false)
            .WithContainerRuntimeArgs("-e", $"ASPNETCORE_HTTPS_PORTS={FrontendControlPort}")
            .WithContainerRuntimeArgs("-e", $"ASPNETCORE_Kestrel__Certificates__Default__Path={pfxContainerPath}")
            .WithContainerRuntimeArgs("-e", $"ASPNETCORE_Kestrel__Certificates__Default__Password={config.PfxPassword}")
            .WithContainerRuntimeArgs("-v", $"{pfxFolder}:{pfxContainerFolder}");

        foreach (var entry in TunneledEndpoints)
        {
            tunnelFrontEndBuilder.WithHttpEndpoint(name: $"ep{entry.Value}", targetPort: entry.Value, isProxied: false);
        }

        // Create a semicolon separated list of the tunneled endpoint ports
        var ports = string.Join(";", TunneledEndpoints.Values.Select(p => p.ToString(CultureInfo.InvariantCulture)));
        tunnelFrontEndBuilder.WithContainerRuntimeArgs("-e", $"ASPNETCORE_HTTP_PORTS={ports}");
    }

    public int FrontendControlPort { get; private set; }

    public Dictionary<EndpointAnnotation, int> TunneledEndpoints = [];

    public int MapPort(int port) => TunneledEndpoints.Single(entry => entry.Key.Port == port).Value;

    // HACK: need to use a proper port allocation mechanism
    int _port = 57500;
    public int AllocatePort()
    {
        return _port++;
    }
}
