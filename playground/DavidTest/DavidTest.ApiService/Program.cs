using Azure.Storage.Blobs;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

var foo = builder.Configuration.GetValue<string>("foo");
Console.WriteLine(foo);
var mysecret = builder.Configuration.GetValue<string>("mysecret");
var myConn = builder.Configuration.GetConnectionString("myconn");
Console.WriteLine(myConn);

var postgresConn = builder.Configuration.GetConnectionString("postgres");

builder.AddAzureBlobClient("uploads");

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching", foo, mysecret
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
});

app.MapGet("/blob", async (BlobServiceClient bsc) =>
{
    var container = bsc.GetBlobContainerClient("mycontainer");
    await container.CreateIfNotExistsAsync();

    var blobNameAndContent = Guid.NewGuid().ToString();
    await container.UploadBlobAsync(blobNameAndContent, new BinaryData(blobNameAndContent));

    var blobs = container.GetBlobsAsync();

    var blobNames = new List<string>();

    await foreach (var blob in blobs)
    {
        blobNames.Add(blob.Name);
    }

    return blobNames;
});

app.MapDefaultEndpoints();

app.Run();

sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
