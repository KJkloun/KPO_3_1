FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["src/Gateway/ApiGateway/ApiGateway.csproj", "src/Gateway/ApiGateway/"]
RUN dotnet restore "src/Gateway/ApiGateway/ApiGateway.csproj"

# Copy the rest of the code
COPY . .

# Build and publish
RUN dotnet build "src/Gateway/ApiGateway/ApiGateway.csproj" -c Release -o /app/build
RUN dotnet publish "src/Gateway/ApiGateway/ApiGateway.csproj" -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ApiGateway.dll"] 