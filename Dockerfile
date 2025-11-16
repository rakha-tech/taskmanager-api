# Use the official .NET 8 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Copy the publish output to the image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TaskManager.Api.csproj", "./"]
RUN dotnet restore "./TaskManager.Api.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "TaskManager.Api.csproj" -c Release -o /app/build

# Publish the project
FROM build AS publish
RUN dotnet publish "TaskManager.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaskManager.Api.dll"]