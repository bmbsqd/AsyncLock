@echo off

dotnet restore
dotnet pack
nuget nuget push .\bin\Release\*.nupkg
