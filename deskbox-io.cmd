@echo off
rem deskbox-io 便捷入口：首次自动构建，之后直接调用已构建的 exe
set "BIN=%~dp0bin\Debug\net10.0\deskbox-io.exe"
if not exist "%BIN%" dotnet build "%~dp0deskbox-io.csproj" -v q >nul
"%BIN%" %*
