// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using UFX.Relay.Tunnel.Forwarder;
using UFX.Relay.Tunnel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTunnelForwarder(options =>
{
    options.DefaultTunnelId = "aspire";
});
var app = builder.Build();
app.MapTunnelHost();
app.MapTunnelForwarder();

app.Run();
