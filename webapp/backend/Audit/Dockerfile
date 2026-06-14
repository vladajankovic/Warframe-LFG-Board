# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the solution and project files
COPY ./Audit/Audit.csproj ./Audit/

# Restore dependencies
RUN dotnet restore ./Audit/Audit.csproj

# Copy the remaining source code
COPY ./Audit ./Audit/

# Build the application
RUN dotnet build ./Audit/Audit.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish ./Audit/Audit.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

EXPOSE 8080
EXPOSE 8443

ENTRYPOINT ["dotnet", "Audit.dll"]
