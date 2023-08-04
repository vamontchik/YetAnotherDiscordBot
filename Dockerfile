FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore
COPY src/* ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:7.0 AS runtime

WORKDIR /app
COPY --from=build /app/out ./
COPY config.yml ./
RUN apt-get update -y && apt-get upgrade -y
ENTRYPOINT ["dotnet", "DiscordBot.dll"]
