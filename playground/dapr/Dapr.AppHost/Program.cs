var builder = DistributedApplication.CreateBuilder(args);

var rmq = builder.AddRabbitMQ("rabbitMQ")
                   .WithManagementPlugin()
                   .WithEndpoint("tcp", e => e.Port = 5672)
                   .WithEndpoint("management", e => e.Port = 15672);

var stateStore = builder.AddDaprStateStore("statestore");
var pubSub = builder.AddDaprPubSub("pubsub")
                    .WaitFor(rmq);

builder.AddProject<Projects.DaprServiceA>("servicea")
       .WithDaprSidecar()
       .WithReference(stateStore)
       .WithReference(pubSub);

builder.AddProject<Projects.DaprServiceB>("serviceb")
       .WithDaprSidecar()
       .WithReference(pubSub);

// console app with no appPort (sender only)
builder.AddProject<Projects.DaprServiceC>("servicec")
       .WithReference(stateStore)
       .WithDaprSidecar();

#if !SKIP_DASHBOARD_REFERENCE
// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// or build with `/p:SkipDashboardReference=true`, to test end developer
// dashboard launch experience, Refer to Directory.Build.props for the path to
// the dashboard binary (defaults to the Aspire.Dashboard bin output in the
// artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);
#endif

using var app = builder.Build();

await app.RunAsync();
