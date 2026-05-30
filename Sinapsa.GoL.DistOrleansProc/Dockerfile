#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

EXPOSE 5050
EXPOSE 11111
EXPOSE 30000
EXPOSE 8000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Directory.Packages.props", "."]
COPY ["Sinapsa.GoL.DistOrleansProc/Sinapsa.GoL.DistOrleansProc.csproj", "Sinapsa.GoL.DistOrleansProc/"]
COPY ["Sinapsa.GoL.DistOrleansProc.Domain/Sinapsa.GoL.DistOrleansProc.Domain.csproj", "Sinapsa.GoL.DistOrleansProc.Domain/"]
COPY ["Sinapsa.GoL.DistOrleansProc.GrainInterfaces/Sinapsa.GoL.DistOrleansProc.GrainInterfaces.csproj", "Sinapsa.GoL.DistOrleansProc.GrainInterfaces/"]
COPY ["Sinapsa.GoL.DistOrleansProc.Orleans.Core/Sinapsa.GoL.DistOrleansProc.Orleans.Core.csproj", "Sinapsa.GoL.DistOrleansProc.Orleans.Core/"]
COPY ["Sinapsa.GoL.DistOrleansProc.Grains/Sinapsa.GoL.DistOrleansProc.Grains.csproj", "Sinapsa.GoL.DistOrleansProc.Grains/"]
RUN dotnet restore "Sinapsa.GoL.DistOrleansProc/Sinapsa.GoL.DistOrleansProc.csproj"
COPY . .
WORKDIR "/src/Sinapsa.GoL.DistOrleansProc"
RUN dotnet build "Sinapsa.GoL.DistOrleansProc.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Sinapsa.GoL.DistOrleansProc.csproj" -c Release -o /app/publish /p:UseAppHost=false


FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sinapsa.GoL.DistOrleansProc.dll"]