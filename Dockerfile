FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY src/Kpmg.Web/Kpmg.Web.csproj src/Kpmg.Web/
RUN dotnet restore src/Kpmg.Web/Kpmg.Web.csproj

COPY src/ src/
RUN dotnet publish src/Kpmg.Web/Kpmg.Web.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .

# Kestrel is configured to listen on http://0.0.0.0:8087 in appsettings.json.
EXPOSE 8087
USER app

ENTRYPOINT ["dotnet", "Kpmg.Web.dll"]
