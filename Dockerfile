# Vigia control plane (API + built-in worker) with the built-in plugins.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
# Building the plugin projects stages them into artifacts/plugins/<id>/<version>/.
RUN dotnet build plugins/Vigia.Check.Http -c Release \
 && dotnet build plugins/Vigia.Check.Tcp -c Release \
 && dotnet build plugins/Vigia.Check.Tls -c Release \
 && dotnet build plugins/Vigia.Check.Dns -c Release \
 && dotnet build plugins/Vigia.Check.Ping -c Release \
 && dotnet build plugins/Vigia.Check.Heartbeat -c Release \
 && dotnet publish src/Vigia.Api -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
# Npgsql probes for GSSAPI at startup and logs an error when the library is missing.
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
COPY --from=build /src/artifacts/plugins ./plugins
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Vigia.Api.dll"]
