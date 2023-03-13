#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Sinapsa.GoL.DistOrleansProc/Sinapsa.GoL.DistOrleansProc.csproj", "Sinapsa.GoL.DistOrleansProc/"]
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