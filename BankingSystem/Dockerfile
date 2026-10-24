
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["BankingSystem.csproj", "./"]
RUN dotnet restore "BankingSystem.csproj"

COPY . .
RUN dotnet publish "BankingSystem.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5107
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

EXPOSE 5107

ENTRYPOINT ["dotnet", "BankingSystem.dll"]
