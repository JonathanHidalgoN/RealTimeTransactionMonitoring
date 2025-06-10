FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY *.sln .
COPY src/*/*.csproj ./src/
COPY tests/*/*.csproj ./tests/
RUN for f in $(find . -name '*.csproj'); do mkdir -p $(dirname $f); done
RUN for f in $(find . -name '*.csproj'); do mv $(basename $f) $(dirname $f); done

RUN dotnet restore "FinancialMonitoringSolution.sln"

COPY . .

RUN dotnet publish "src/FinancialMonitoring.Api/FinancialMonitoring.Api.csproj" -c Release -o /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "FinancialMonitoring.Api.dll"]
