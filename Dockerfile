FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app
COPY *.csproj ./
RUN dotnet restore
COPY src/* ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime

WORKDIR /app
COPY --from=build /app/out ./
COPY config.yml ./
RUN apt-get update -y && apt-get upgrade -y
RUN apt-get install -y python3
RUN apt-get install -y libopus0 libopus-dev
RUN apt-get install -y curl
RUN apt-get install -y ffmpeg
RUN curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o /usr/local/bin/yt-dlp
RUN chmod a+rx /usr/local/bin/yt-dlp
RUN apt-get install -y libsodium23 libsodium-dev
ENTRYPOINT ["dotnet", "DiscordBot.dll"]
