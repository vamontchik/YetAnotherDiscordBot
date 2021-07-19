# Use Microsoft's official build .NET image.
# Use 5.0 not 3.1, and then cd into the app/ dir
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /app

# Install production dependencies.
# Copy csproj and restore as distinct layers.
COPY *.csproj ./
RUN dotnet restore

# Copy local code to the container image.
# cd into the app/ dir
COPY src/* ./
WORKDIR /app

# Build a release artifact.
RUN dotnet publish -c Release -o out

# Use Microsoft's official runtime .NET image
# Use 5.0 not 3.1, and then cd into the app/ dir
# Then, make sure to copy in the build artifacts
# and the auth files / tokens
FROM mcr.microsoft.com/dotnet/runtime:5.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./
COPY token.txt ./
COPY id.txt ./

# Run the application on container startup.
ENTRYPOINT ["dotnet", "DiscordBot.dll"]
