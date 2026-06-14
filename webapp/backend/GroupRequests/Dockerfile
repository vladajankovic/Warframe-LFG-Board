# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the solution and project files
COPY ./GroupRequests/GroupRequests.csproj ./GroupRequests/
COPY ./GroupRequests.Tests/GroupRequests.Tests.csproj ./GroupRequests.Tests/

# Restore dependencies
RUN dotnet restore ./GroupRequests/GroupRequests.csproj

# Copy the remaining source code
COPY ./GroupRequests ./GroupRequests/
COPY ./GroupRequests.Tests ./GroupRequests.Tests/

# Build the application
RUN dotnet build ./GroupRequests/GroupRequests.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish ./GroupRequests/GroupRequests.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

EXPOSE 8080
EXPOSE 8443

ENTRYPOINT ["dotnet", "GroupRequests.dll"]
