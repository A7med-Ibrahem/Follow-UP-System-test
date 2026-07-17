# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore
COPY ["smartFollowup.API/smartFollowup.API.csproj", "smartFollowup.API/"]
RUN dotnet restore "smartFollowup.API/smartFollowup.API.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/smartFollowup.API"
RUN dotnet build "smartFollowup.API.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "smartFollowup.API.csproj" -c Release -o /app/publish

# Final Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create uploads folder
RUN mkdir -p /app/wwwroot/uploads

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "smartFollowup.API.dll"]