# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only project files (build context should be project root)
COPY . ./

RUN dotnet restore NotificationWorker/NotificationWorker.csproj
RUN dotnet publish NotificationWorker/NotificationWorker.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationWorker.dll"]




# =====================================================================================================================
############################################## PRODUCTION CONFIGURATION ##############################################
# =====================================================================================================================


 ## Build stage
 #FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
 #WORKDIR /src
#
 ## Copy entire solution into container
 #COPY ../ ./
#
 ## Restore + publish
 #RUN dotnet restore NotificationWorker/NotificationWorker.csproj
 #RUN dotnet publish NotificationWorker/NotificationWorker.csproj -c Release -o /app/publish
#
 ## Runtime stage
 #FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
 #WORKDIR /app
 #COPY --from=build /app/publish .
 #ENTRYPOINT ["dotnet", "NotificationWorker.dll"]

