# ===========================
# Build stage
# ===========================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy solution & restore
COPY *.sln ./
COPY src/Web.Api/*.csproj src/Web.Api/
RUN dotnet restore

# copy everything and publish
COPY . .
RUN dotnet publish src/Web.Api/Web.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===========================
# Runtime stage
# ===========================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
# The port can be overridden with `-e PORT=xxxx`
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Web.Api.dll"]
