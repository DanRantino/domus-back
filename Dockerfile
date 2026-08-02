# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Domus.sln ./
COPY src/Domus.Api/Domus.Api.csproj src/Domus.Api/
RUN dotnet restore src/Domus.Api/Domus.Api.csproj

COPY src/Domus.Api/ src/Domus.Api/
RUN dotnet publish src/Domus.Api/Domus.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Domus.Api.dll"]
