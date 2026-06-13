# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the solution and project files
COPY ./ItemsStore/ItemsStore.csproj ./ItemsStore/
COPY ./ItemsStore.Tests/ItemsStore.Tests.csproj ./ItemsStore.Tests/

# Restore dependencies
RUN dotnet restore ./ItemsStore/ItemsStore.csproj

# Copy the remaining source code
COPY ./ItemsStore ./ItemsStore/
COPY ./ItemsStore.Tests ./ItemsStore.Tests/

# Build the application
RUN dotnet build ./ItemsStore/ItemsStore.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish ./ItemsStore/ItemsStore.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080
EXPOSE 8443

ENTRYPOINT ["dotnet", "ItemsStore.dll"]
