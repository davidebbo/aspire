// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting;
using Aspire.Hosting.ReverseProxyTunnel;

class TunnelingProxyManager
{
    const string FrontendContainerImage = "aspiretunnelingproxyfrontend";
    public const string FrontendContainerName = "tunnelingproxyfrontend";

    public TunnelingProxyManager(IDistributedApplicationBuilder distributedAppBuilder, TunnelingProxyConfiguration config)
    {
        FrontendControlPort = AllocatePort();

        // Find all the endpoints that need to be tunneled
        // Go through all the container resources
        foreach (var resource in distributedAppBuilder.Resources.Where(r => r.IsContainer()))
        {
            // Look for non-container referenced resources, as these are the ones we want to tunnel
            foreach (var referencedResource in resource.Annotations.OfType<EndpointReferenceAnnotation>()
                .Where(a => !a.Resource.IsContainer())
                .Select(a => a.Resource))
            {
                // In that non-container referenced resource, process all the endpoint annotations
                // that are not already processed and have a known port
                foreach (var ep in referencedResource.Annotations.OfType<EndpointAnnotation>()
                    .Where(ep => !TunneledEndpoints.ContainsKey(ep) && ep.Port != null))
                {
                    // Allocate a port for this endpoint to be used on the tunnel proxy frontend
                    TunneledEndpoints.Add(ep, AllocatePort());
                }
            }
        }

        if (TunneledEndpoints.Count == 0)
        {
            // No tunneled endpoints, no need to create the tunnel frontend container
            return;
        }

        var pfxFileName = Path.GetFileName(config.PfxPath);
        var pfxFolder = Path.GetDirectoryName(config.PfxPath);
        var pfxContainerFolder = "/https/";
        var pfxContainerPath = $"{pfxContainerFolder}{pfxFileName}";

        var tunnelFrontEndBuilder = distributedAppBuilder.AddContainer(FrontendContainerName, FrontendContainerImage)
            // Expose the https endpoint used from the backend to connect to the frontend container
            .WithHttpsEndpoint(name: "tunnel", targetPort: FrontendControlPort, isProxied: false)
            .WithContainerRuntimeArgs("-e", $"ASPNETCORE_HTTPS_PORTS={FrontendControlPort}")
            // Set up the tunnel frontend container to have access to the host's PFX file, which
            // is needed since the communication from the backend (running on the host) is https
            .WithContainerRuntimeArgs("-e", $"ASPNETCORE_Kestrel__Certificates__Default__Path={pfxContainerPath}")
            .WithContainerRuntimeArgs("-e", $"ASPNETCORE_Kestrel__Certificates__Default__Password={config.PfxPassword}")
            .WithContainerRuntimeArgs("-v", $"{pfxFolder}:{pfxContainerFolder}");

        // Create endpoints on the frontend container for each tunneled endpoint
        foreach (var entry in TunneledEndpoints)
        {
            tunnelFrontEndBuilder.WithHttpEndpoint(name: $"ep{entry.Value}", targetPort: entry.Value, isProxied: false);
        }

        // Create a semicolon separated list of the tunneled endpoint ports to use as ASPNETCORE_HTTP_PORTS
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
