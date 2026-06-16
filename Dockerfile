# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# Build stage — compile and publish using the full .NET SDK image.
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project file first and restore. This layer is cached and only
# re-runs when dependencies change, so source edits don't trigger a full restore.
COPY rytmix-api.csproj ./
RUN dotnet restore

# Copy the rest of the source and publish a Release build.
COPY . ./
RUN dotnet publish rytmix-api.csproj -c Release -o /app/publish --no-restore

# ---------------------------------------------------------------------------
# Runtime stage — smaller ASP.NET runtime image (no SDK) to actually run.
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

# Render injects the port the container must listen on via the PORT env var, and
# only routes traffic if the app binds to 0.0.0.0 on that port. We bind Kestrel
# via ASPNETCORE_URLS. Shell form is used so ${PORT} is expanded at runtime;
# it falls back to 8080 for a plain local `docker run`.
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} dotnet rytmix-api.dll"]
