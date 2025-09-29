# ===========================
# Build stage
# ===========================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy solution & restore
COPY *.sln ./
COPY src/Web.Api/*.csproj src/Web.Api/
COPY src/Core/*.csproj src/Core/
# Add test project csproj files
COPY tests/Core.Tests/*.csproj tests/Core.Tests/
COPY tests/Web.Api.Tests/*.csproj tests/Web.Api.Tests/
RUN dotnet restore

# copy everything and publish
COPY . .
RUN dotnet publish src/Web.Api/Web.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===========================
# Runtime stage
# ===========================
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime

# Create non-root user
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /home/appuser
USER appuser

WORKDIR /app
COPY --from=build /app/publish .

# Support for $PORT environment variable with default to 8080
ENV PORT=8080
ENV ASPNETCORE_URLS=http://+:${PORT}
EXPOSE ${PORT}

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD wget -qO- http://localhost:${PORT}/health || exit 1

ENTRYPOINT ["dotnet", "Web.Api.dll"]
