#!/bin/bash

# Exit immediately if a command exits with a non-zero status
set -e

echo "Starting Uno Platform Skia Desktop builds..."

# ---------------------------------------------------------
# Windows Builds
# ---------------------------------------------------------
echo "Building for Windows (x64)..."
dotnet publish -c Release -f net10.0-desktop -r win-x64 --self-contained -p:PublishSingleFile=true

echo "Building for Windows (x86)..."
dotnet publish -c Release -f net10.0-desktop -r win-x86 --self-contained -p:PublishSingleFile=true

echo "Building for Windows (ARM64)..."
dotnet publish -c Release -f net10.0-desktop -r win-arm64 --self-contained -p:PublishSingleFile=true

# ---------------------------------------------------------
# Linux Builds
# ---------------------------------------------------------
echo "Building for Linux (x64)..."
dotnet publish -c Release -f net10.0-desktop -r linux-x64 --self-contained -p:PublishSingleFile=true

echo "Building for Linux (ARM64)..."
dotnet publish -c Release -f net10.0-desktop -r linux-arm64 --self-contained -p:PublishSingleFile=true

echo "Building for Linux (ARM32)..."
dotnet publish -c Release -f net10.0-desktop -r linux-arm --self-contained -p:PublishSingleFile=true

# ---------------------------------------------------------
# macOS Builds
# ---------------------------------------------------------
echo "Building for macOS (Intel x64)..."
dotnet publish -c Release -f net10.0-desktop -r osx-x64 --self-contained -p:PublishSingleFile=true

echo "Building for macOS (Apple Silicon ARM64)..."
dotnet publish -c Release -f net10.0-desktop -r osx-arm64 --self-contained -p:PublishSingleFile=true

echo "======================================================"
echo "All builds completed successfully!"
echo "Check the bin/Release/net10.0-desktop/ directory for your files."
