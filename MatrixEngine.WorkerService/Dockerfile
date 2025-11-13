FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MatrixEngine.WorkerService/MatrixEngine.WorkerService.csproj", "MatrixEngine.WorkerService/"]
RUN dotnet restore "MatrixEngine.WorkerService/MatrixEngine.WorkerService.csproj"

COPY . .
WORKDIR "/src/MatrixEngine.WorkerService"
RUN dotnet build "MatrixEngine.WorkerService.csproj" -c Release -o /app/build

FROM build AS publish
# RUN dotnet publish "MatrixEngine.WorkerService.csproj" -c Release -o /app/publish /p:UseAppHost=false
RUN dotnet publish "MatrixEngine.WorkerService.csproj" -c Release -o /app/publish/WorkerService /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish/WorkerService .

ENTRYPOINT ["dotnet", "MatrixEngine.WorkerService.dll"]
