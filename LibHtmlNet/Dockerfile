FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["LibHtmlNet/LibHtmlNet.csproj", "LibHtmlNet/"]
RUN dotnet restore "LibHtmlNet/LibHtmlNet.csproj"
COPY . .
WORKDIR "/src/LibHtmlNet"
RUN dotnet build "LibHtmlNet.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LibHtmlNet.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LibHtmlNet.dll"]
