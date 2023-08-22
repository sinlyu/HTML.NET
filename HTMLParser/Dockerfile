FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["HTMLParser/HTMLParser.csproj", "HTMLParser/"]
RUN dotnet restore "HTMLParser/HTMLParser.csproj"
COPY . .
WORKDIR "/src/HTMLParser"
RUN dotnet build "HTMLParser.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HTMLParser.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HTMLParser.dll"]
