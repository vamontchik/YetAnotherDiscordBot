FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

WORKDIR /app
COPY *.csproj ./
RUN dotnet restore
COPY src/* ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:7.0 AS runtime

WORKDIR /app
ENV LIBSODIUM_VERSION 1.0.18
COPY --from=build /app/out ./
COPY config.yml ./
RUN apt-get update -y && apt-get upgrade -y
RUN apt-get install -y python3
RUN apt-get install -y libopus0
RUN apt-get install -y curl build-essential unzip locate
RUN apt-get install -y ffmpeg
RUN curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o /usr/local/bin/yt-dlp
RUN chmod a+rx /usr/local/bin/yt-dlp
RUN \
    mkdir -p /tmpbuild/libsodium && \
    cd /tmpbuild/libsodium && \
    curl -L https://download.libsodium.org/libsodium/releases/libsodium-${LIBSODIUM_VERSION}.tar.gz -o libsodium-${LIBSODIUM_VERSION}.tar.gz && \
    tar xfvz libsodium-${LIBSODIUM_VERSION}.tar.gz && \
    cd /tmpbuild/libsodium/libsodium-${LIBSODIUM_VERSION}/ && \
    ./configure && \
    make && make check && \
    make install && \
    mv src/libsodium /usr/local/ && \
    rm -Rf /tmpbuild/
ENTRYPOINT ["dotnet", "DiscordBot.dll"]
