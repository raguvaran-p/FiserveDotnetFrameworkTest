
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-ltsc2022
 
WORKDIR C:/src

COPY *.slnx ./
COPY *.csproj ./


RUN nuget restore

RUN msbuild FiserveDotnetFrameworkTest.slnx /p:Configuration=Release

FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8

COPY --from=build C:/src/bin/Release/_PublishedWebsites/ .
