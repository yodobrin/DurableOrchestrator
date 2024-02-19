FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0-bookworm

RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends nodejs npm

RUN npm install -g azure-functions-core-tools@4 --unsafe-perm true
