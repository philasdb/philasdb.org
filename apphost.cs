// Aspire AppHost for the philasdb apps: `dotnet run apphost.cs` (or `aspire run`) starts every app with the
// Aspire dashboard (logs, traces, metrics). As the bible and livestream apps move into this repo, add them
// here next to the site.
#:sdk Aspire.AppHost.Sdk@13.6.5
#:project ./src/PhilaSDB.Site/PhilaSDB.Site.fsproj

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.PhilaSDB_Site>("site")
    .WithHttpHealthCheck("/healthz")
    .WithExternalHttpEndpoints();

builder.Build().Run();
