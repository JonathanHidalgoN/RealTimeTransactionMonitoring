FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY TransactionProcessor.csproj .
RUN dotnet restore TransactionProcessor.csproj

COPY . .
RUN dotnet publish TransactionProcessor.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TransactionProcessor.dll"]
