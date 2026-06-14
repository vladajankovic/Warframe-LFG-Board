# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the solution and project files
COPY ./UsersDirectory/UsersDirectory.csproj ./UsersDirectory/
COPY ./UsersDirectory.Tests/UsersDirectory.Tests.csproj ./UsersDirectory.Tests/

# Restore dependencies
RUN dotnet restore ./UsersDirectory/UsersDirectory.csproj

# Copy the remaining source code
COPY ./UsersDirectory ./UsersDirectory/
COPY ./UsersDirectory.Tests ./UsersDirectory.Tests/

# Build the application
RUN dotnet build ./UsersDirectory/UsersDirectory.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish ./UsersDirectory/UsersDirectory.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

EXPOSE 8080
EXPOSE 8443

ENTRYPOINT ["dotnet", "UsersDirectory.dll"]
