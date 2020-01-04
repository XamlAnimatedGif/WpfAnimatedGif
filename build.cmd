@echo off
dotnet build -c Release /bl:artifacts/build.binlog
dotnet pack -c Release --no-build WpfAnimatedGif/WpfAnimatedGif.csproj -o artifacts /bl:artifacts/pack.binlog

