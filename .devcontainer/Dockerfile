# Use the .NET 8.0 SDK as a base image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base

# Install system dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    wget \
    apt-transport-https \
    lsb-release \
    gpg \
    curl \
    gnupg2 \
    software-properties-common \
    && rm -rf /var/lib/apt/lists/*

# Install the Azure CLI
RUN curl -sL https://aka.ms/InstallAzureCLIDeb | bash

# Install Azure Functions Core Tools
RUN wget -q https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y azure-functions-core-tools-4

# Clean up
RUN apt-get clean && rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/*

# Set the working directory
WORKDIR /workspace
