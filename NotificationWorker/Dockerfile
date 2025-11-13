# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY .. .
RUN dotnet restore NotificationWorker/NotificationWorker.csproj
RUN dotnet publish NotificationWorker/NotificationWorker.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationWorker.dll"]
