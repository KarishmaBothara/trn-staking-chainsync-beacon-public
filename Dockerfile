# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy only the needed project sub directories
COPY MatrixEngine.Core/. ./MatrixEngine.Core/
COPY MatrixEngine.WorkerService/. ./MatrixEngine.WorkerService/

# Restore dependencies
RUN dotnet restore ./MatrixEngine.Core/MatrixEngine.Core.csproj
RUN dotnet restore ./MatrixEngine.WorkerService/MatrixEngine.WorkerService.csproj

# Build the worker service
WORKDIR /src/MatrixEngine.WorkerService
RUN dotnet build -c Release -o /app/build

# Final image
FROM mcr.microsoft.com/dotnet/runtime:6.0 AS final
WORKDIR /app

# Copy the build output files
COPY --from=build /app/build .
# Copy appsettings files specifically to ensure they're included
COPY --from=build /src/MatrixEngine.WorkerService/appsettings*.json ./

# Expose the port for the health check endpoint (if applicable)
EXPOSE 80

# Start the worker service
ENTRYPOINT ["dotnet", "MatrixEngine.WorkerService.dll"]
