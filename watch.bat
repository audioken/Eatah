@echo off
start "Eatah.Api" cmd /k "cd src\Eatah.Api && dotnet watch run"
start "Eatah.WebClient" cmd /k "cd src\Eatah.WebClient && dotnet watch run"
