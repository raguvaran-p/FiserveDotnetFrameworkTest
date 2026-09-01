FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-ltsc2022 AS build

WORKDIR C:/src

COPY . .

# Restore solution
RUN msbuild FiserveDotnetFrameworkTest.slnx -t:Restore

# Restore packages.config packages
RUN nuget restore packages.config -PackagesDirectory packages

# Build and create the IIS web package
RUN msbuild FiserveDotnetFrameworkTest.csproj /p:Configuration=Release /p:DeployOnBuild=true /p:WebPublishMethod=FileSystem

# Verify the generated publish files
RUN dir C:\src\obj\Release\Package\PackageTmp


# Runtime image for ASP.NET Framework 4.8
FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8

# Copy the generated application files into IIS
COPY --from=build C:/src/obj/Release/Package/PackageTmp/ C:/inetpub/wwwroot/

