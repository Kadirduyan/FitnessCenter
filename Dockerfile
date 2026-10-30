# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["FitnessCenter.csproj", "./"]
RUN dotnet restore "FitnessCenter.csproj"

# Copy remaining source files and publish
COPY . .
RUN dotnet publish "FitnessCenter.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create directory for persistent SQLite database
RUN mkdir -p /app/data

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/FitnessCenter.db"

EXPOSE 8080

ENTRYPOINT ["dotnet", "FitnessCenter.dll"]
