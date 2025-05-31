FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY FinancialMonitoringSolution.sln .
COPY src/FinancialMonitoring.Api/FinancialMonitoring.Api.csproj ./src/FinancialMonitoring.Api/
COPY src/FinancialMonitoring.Models/FinancialMonitoring.Models.csproj ./src/FinancialMonitoring.Models/
COPY src/FinancialMonitoring.Abstractions/FinancialMonitoring.Abstractions.csproj ./src/FinancialMonitoring.Abstractions/

RUN dotnet restore FinancialMonitoringSolution.sln

COPY ./src/ ./src/

RUN dotnet publish ./src/FinancialMonitoring.Api/FinancialMonitoring.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "FinancialMonitoring.Api.dll"]