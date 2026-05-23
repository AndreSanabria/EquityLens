FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/EquityLens.Api/EquityLens.Api.csproj src/EquityLens.Api/
RUN dotnet restore src/EquityLens.Api/EquityLens.Api.csproj

COPY src/EquityLens.Api/ src/EquityLens.Api/
RUN dotnet publish src/EquityLens.Api/EquityLens.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EquityLens.Api.dll"]
