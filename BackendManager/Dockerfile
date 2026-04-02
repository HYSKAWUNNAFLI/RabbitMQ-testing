FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

# Set the working directory to the whole project root
WORKDIR /app

# Copy all the modules and the backend manager code
# Note: we are running this Dockerfile from the ROOT context in docker-compose
COPY . .

# Build the ASP.NET Core Backend
WORKDIR /app/BackendManager
RUN dotnet publish -c Release -o /app/out

# Switch to output directory
WORKDIR /app/out

# Port configuration
ENV ASPNETCORE_URLS=http://+:4000
EXPOSE 4000

# Start the application
# We use the SDK image because `ProcessManager.cs` runs `dotnet run --project ...`
ENTRYPOINT ["dotnet", "BackendManager.dll"]
