
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-ltsc2022
 
WORKDIR C:/src

COPY *.* ./

RUN msbuild FiserveDotnetFrameworkTest.slnx -t:Restore
RUN nuget restore packages.config -PackagesDirectory packages
RUN dir packages
RUN msbuild FiserveDotnetFrameworkTest.slnx /p:Configuration=Release

FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8

COPY --from=build C:/src/bin/Release/_PublishedWebsites/ .
