# Stage 1: Build the .NET application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["HealthcareAiAssistant.sln", "./"]
COPY ["HealthcareAiApi/HealthcareAiApi.csproj", "HealthcareAiApi/"]

# Restore dependencies
RUN dotnet restore "HealthcareAiAssistant.sln"

# Copy remaining source code
COPY . .
WORKDIR "/src/HealthcareAiApi"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HealthcareAiApi.dll"]