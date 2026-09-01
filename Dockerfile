FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
 
# Copy csproj and restore dependencies to cache this layer
COPY *.slnx ./
COPY *.csproj ./
RUN dotnet restore
 
# Copy everything else and build the release app
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore
 
# 2. Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
 
# Replace "YourApp.dll" with your actual project output DLL name
ENTRYPOINT ["dotnet", "FiserveDotnetFrameworkTest.dll"]
