# Use the official .NET Core SDK image for building
FROM mcr.microsoft.com/dotnet/core/sdk:8.0 AS build
WORKDIR /src

# Copy only the project file and restore dependencies
COPY ["MatrixEngine.WorkerService/MatrixEngine.WorkerService.csproj", "MatrixEngine.WorkerService/"]
RUN dotnet restore "MatrixEngine.WorkerService/MatrixEngine.WorkerService.csproj"

# Copy the entire project and build the application
COPY . .
WORKDIR "/src/MatrixEngine.WorkerService"
RUN dotnet build "MatrixEngine.WorkerService.csproj" -c Release -o /app/build

# Use a separate stage for publishing the application
FROM build AS publish
RUN dotnet publish "MatrixEngine.WorkerService.csproj" -c Release -o /app/publish

# Use the official .NET Core runtime image for the final stage
FROM mcr.microsoft.com/dotnet/core/runtime:8.0 AS final
WORKDIR /app

# Copy the published output from the previous stage
COPY --from=publish /app/publish .

# Expose the port for the health check endpoint (if applicable)
EXPOSE 80

# Health check endpoint (replace with your actual health check logic)
# HEALTHCHECK --interval=30s --timeout=3s CMD curl --fail http://localhost:80/health || exit 1

# Start the worker service
ENTRYPOINT ["dotnet", "MatrixEngine.WorkerService.dll"]
