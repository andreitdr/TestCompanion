@echo off
echo Building for Windows...
dotnet publish -c Release -f net10.0-desktop -r win-x64 --self-contained -p:PublishSingleFile=true
dotnet publish -c Release -f net10.0-desktop -r win-x86 --self-contained -p:PublishSingleFile=true
dotnet publish -c Release -f net10.0-desktop -r win-arm64 --self-contained -p:PublishSingleFile=true

echo Building for Linux...
dotnet publish -c Release -f net10.0-desktop -r linux-x64 --self-contained -p:PublishSingleFile=true
dotnet publish -c Release -f net10.0-desktop -r linux-arm64 --self-contained -p:PublishSingleFile=true
dotnet publish -c Release -f net10.0-desktop -r linux-arm --self-contained -p:PublishSingleFile=true

echo Building for macOS...
dotnet publish -c Release -f net10.0-desktop -r osx-x64 --self-contained -p:PublishSingleFile=true
dotnet publish -c Release -f net10.0-desktop -r osx-arm64 --self-contained -p:PublishSingleFile=true

echo Done!
pause
