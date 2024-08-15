//#define TEST_SQL
//#define TEST_STORAGE

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if TEST_STORAGE
using Azure.Provisioning;
using Azure.Provisioning.Storage;
#endif

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var foo = builder.AddParameter("foo");
var mySecret = builder.AddParameter("mysecret", true);
var myConn = builder.AddConnectionString("myconn");

#if TEST_SQL
builder.AddSqlServer("sql")
                 .WithDataVolume()
                 .AddDatabase("sqldb");
builder.AddSqlServer("sql")
                 .WithVolume("DavidTest.AppHost-sql-data", "/var/opt/mssql")
                 .AddDatabase("sqldb");
builder.AddSqlServer("sql")
                 .WithBindMount("DavidTest.AppHost-sql-data", "/var/opt/mssql")
                 .AddDatabase("sqldb");

var db = builder.AddMySql("mysql", port: 43123)
    .WithDataVolume()
    .AddDatabase("mydb")
    ;
#endif

#if TEST_STORAGE
#pragma warning disable AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var blobs = builder.AddAzureStorage("storage")
    .ConfigureConstruct(c =>
    {
        var account = c.GetSingleResource<StorageAccount>()!;

        account.AssignProperty(x => x.Sku.Name, "'Standard_LRS'");
        account.AssignProperty(x => x.EnableHttpsTrafficOnly, "true");
    }
    )
    .RunAsEmulator(e => e.WithImageTag("latest"))
    .AddBlobs("uploads");
#pragma warning restore AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#endif

var apiService = builder.AddProject<Projects.DavidTest_ApiService>("apiservice")
       //.WithExternalHttpEndpoints()
       .WithEnvironment("FOO", foo)
       .WithEnvironment("MYSECRET", mySecret)
       .WithReference(myConn)
#if TEST_STORAGE
       .WithReference(blobs)
#endif
#if TEST_SQL
       .WithReference(db)
#endif
       //.WithEndpointsInEnvironment(e => e.UriScheme == "https")
       //.WithEnvironment(async context =>
       //{
       //    // Variant of https://github.com/dotnet/aspire/issues/2887#issuecomment-2074397449
       //    if (context.EnvironmentVariables["ASPNETCORE_URLS"] is ReferenceExpression urls)
       //    {
       //        var value = await urls.GetValueAsync(context.CancellationToken);

       //        context.EnvironmentVariables["ASPNETCORE_URLS"] = ReferenceExpression.Create(
       //            $"{(EndpointReferenceExpression)urls.ValueProviders[0]}://*:{(EndpointReferenceExpression)urls.ValueProviders[1]}");
       //    }
       //})
       ;

builder.AddProject<Projects.DavidTest_Web>("webfrontend")
    //.WithHttpEndpoint(5002, name: "http2")
    //.WithHttpsEndpoint(5003, name: "https")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WithReference(apiService);

// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// to test end developer dashboard launch experience. Refer to Directory.Build.props
// for the path to the dashboard binary (defaults to the Aspire.Dashboard bin output
// in the artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);

builder.Build().Run();
