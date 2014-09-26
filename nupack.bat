@echo off
mkdir NuGetPackages 2> NUL
nuget pack -OutputDirectory NuGetPackages WpfAnimatedGif.nuspec
