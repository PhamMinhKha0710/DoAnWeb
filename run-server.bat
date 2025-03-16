@echo off
echo Starting DevCommunity Server...
echo.
echo Server will be available at:
echo - Local: https://localhost:7133
echo - Network: https://%COMPUTERNAME%:7133
echo.
echo To access from other devices on your network, use one of these URLs:
ipconfig | findstr /C:"IPv4 Address"
echo.
echo Remember to use https and port 7133 when connecting from other devices.
echo Example: https://YOUR-IP-ADDRESS:7133
echo.
echo Press Ctrl+C to stop the server.
echo.

dotnet run --urls="https://0.0.0.0:7133;http://0.0.0.0:5225" 