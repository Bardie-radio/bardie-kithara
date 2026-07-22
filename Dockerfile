# Build from the parent folder that contains `kithara/`:
#
#   docker build -f kithara/Dockerfile -t kithara .
#
# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY kithara/Directory.Build.props kithara/Directory.Packages.props kithara/
COPY kithara/Kithara.sln kithara/
COPY kithara/libs kithara/libs/
COPY kithara/src/Kithara kithara/src/Kithara/

RUN dotnet restore kithara/src/Kithara/Kithara.csproj
RUN dotnet publish kithara/src/Kithara/Kithara.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/ \
    && groupadd --gid 1000 kithara \
    && useradd --uid 1000 --gid kithara --create-home kithara

COPY --from=build /app/publish .
RUN mkdir -p /data/mtls /data/db && chown -R kithara:kithara /data /app

USER kithara
ENV ASPNETCORE_URLS= \
    BARDIE_GRPC_TLS_DATA_PATH=/data/mtls \
    DbProvider=sqlite \
    DbConnectionString="Data Source=/data/db/kithara.db"

EXPOSE 8080 5000
HEALTHCHECK --interval=30s --timeout=3s --start-period=25s --retries=3 \
  CMD curl -fsS http://127.0.0.1:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "Kithara.dll"]
