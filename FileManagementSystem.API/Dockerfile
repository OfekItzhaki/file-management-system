# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy all .csproj files first for efficient layer caching
COPY ["FileManagementSystem.API/FileManagementSystem.API.csproj", "FileManagementSystem.API/"]
COPY ["FileManagementSystem.Application/FileManagementSystem.Application.csproj", "FileManagementSystem.Application/"]
COPY ["FileManagementSystem.Domain/FileManagementSystem.Domain.csproj", "FileManagementSystem.Domain/"]
COPY ["FileManagementSystem.Infrastructure/FileManagementSystem.Infrastructure.csproj", "FileManagementSystem.Infrastructure/"]
COPY ["FileManagementSystem.Tests/FileManagementSystem.Tests.csproj", "FileManagementSystem.Tests/"]

# Restore dependencies
RUN dotnet restore "FileManagementSystem.API/FileManagementSystem.API.csproj"

# Copy the rest of the source code
COPY . .

# Build and publish
WORKDIR "/src/FileManagementSystem.API"
RUN dotnet publish "FileManagementSystem.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Environment variables
ENV ASPNETCORE_URLS=http://+:5295
EXPOSE 5295

ENTRYPOINT ["dotnet", "FileManagementSystem.API.dll"]
