
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-ltsc2022
 
WORKDIR /src

COPY *.slnx ./
COPY *.csproj ./


RUN msbuild FiserveDotnetFrameworkTest.slnx -t:Restore

RUN msbuild FiserveDotnetFrameworkTest.slnx /p:Configuration=Release

FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8

COPY --from=build /src/bin/Release/_PublishedWebsites/ .
