FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/AccountTransferLedger.API/AccountTransferLedger.API.csproj", "src/AccountTransferLedger.API/"]
COPY ["src/AccountTransferLedger.Infrastructure/AccountTransferLedger.Infrastructure.csproj", "src/AccountTransferLedger.Infrastructure/"]
COPY ["src/AccountTransferLedger.Application/AccountTransferLedger.Application.csproj", "src/AccountTransferLedger.Application/"]
COPY ["src/AccountTransferLedger.Domain/AccountTransferLedger.Domain.csproj", "src/AccountTransferLedger.Domain/"]

RUN dotnet restore "src/AccountTransferLedger.API/AccountTransferLedger.API.csproj"

COPY . .
WORKDIR "/src/src/AccountTransferLedger.API"
RUN dotnet publish "AccountTransferLedger.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AccountTransferLedger.API.dll"]
