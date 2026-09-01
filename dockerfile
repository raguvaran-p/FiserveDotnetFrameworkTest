
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-ltsc2022 AS build
 
WORKDIR C:/src

COPY . .

RUN msbuild FiserveDotnetFrameworkTest.slnx -t:Restore
RUN nuget restore packages.config -PackagesDirectory packages
RUN dir packages
RUN msbuild FiserveDotnetFrameworkTest.csproj /p:Configuration=Release /p:DeployOnBuild=true /p:WebPublishMethod=FileSystem /p:PublishUrl=C:\publish
FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8
COPY --from=build C:/publish/ .

