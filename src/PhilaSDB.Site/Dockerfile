# Native AOT build of the website. Build from the repo root, which holds the shared props, data and libraries:
#   docker build -f src/PhilaSDB.Site/Dockerfile -t philasdb-site .
# .NET 11 images are based on Ubuntu 26.04 (resolute). The -aot SDK image adds the native toolchain
# (clang, zlib) the AOT compiler links with.
FROM mcr.microsoft.com/dotnet/sdk:11.0-resolute-aot AS build
WORKDIR /src

# Restore first, from the project files only, so the layer is reused until a dependency changes
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/PhilaSDB.Scripture/PhilaSDB.Scripture.fsproj src/PhilaSDB.Scripture/
COPY src/PhilaSDB.ServiceDefaults/PhilaSDB.ServiceDefaults.csproj src/PhilaSDB.ServiceDefaults/
COPY src/PhilaSDB.Site/PhilaSDB.Site.fsproj src/PhilaSDB.Site/
RUN dotnet restore src/PhilaSDB.Site -r linux-x64

COPY data/ data/
COPY src/ src/
RUN dotnet publish src/PhilaSDB.Site -c Release -r linux-x64 --no-restore -o /app/publish \
    && rm -f /app/publish/*.dbg /app/publish/*.pdb

# The app is a self-contained native executable, so it needs only the OS dependencies, not the .NET runtime
FROM mcr.microsoft.com/dotnet/runtime-deps:11.0-resolute-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["./PhilaSDB.Site"]
