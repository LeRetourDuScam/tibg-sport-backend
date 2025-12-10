# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["tibg-sport-backend/tibg-sport-backend.csproj", "tibg-sport-backend/"]
COPY ["TIBG.Models/TIBG.Core.Models.csproj", "TIBG.Models/"]
COPY ["TIBG.Core/TIBG.API.Core.csproj", "TIBG.Core/"]
COPY ["TIBG.Contracts/TIBG.Contracts.csproj", "TIBG.Contracts/"]

# Restore dependencies
RUN dotnet restore "tibg-sport-backend/tibg-sport-backend.csproj"

# Copy all source code
COPY . .

# Build and publish
WORKDIR "/src/tibg-sport-backend"
RUN dotnet build "tibg-sport-backend.csproj" -c Release -o /app/build
RUN dotnet publish "tibg-sport-backend.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Expose port
EXPOSE 5000

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "tibg-sport-backend.dll"]
