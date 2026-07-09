# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the solution and project files
COPY ./UserNotification/UserNotification.csproj ./UserNotification/
COPY ./UserNotification.Tests/UserNotification.Tests.csproj ./UserNotification.Tests/

# Restore dependencies
RUN dotnet restore ./UserNotification/UserNotification.csproj

# Copy the remaining source code
COPY ./UserNotification ./UserNotification/
COPY ./UserNotification.Tests ./UserNotification.Tests/

# Build the application
RUN dotnet build ./UserNotification/UserNotification.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish ./UserNotification/UserNotification.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

EXPOSE 8080
EXPOSE 8443

ENTRYPOINT ["dotnet", "UserNotification.dll"]
