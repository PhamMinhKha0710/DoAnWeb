@echo off
setlocal enabledelayedexpansion

REM Xác định đường dẫn của SQL script
SET SCRIPT_PATH=%~dp0gitea-user-columns.sql

REM Xác định server name và database name từ appsettings.json
FOR /F "tokens=*" %%a IN ('type ..\appsettings.json ^| findstr /C:"DevCommunityDB"') DO (
    SET CONNECTION_STRING=%%a
)

REM Trích xuất Server name và Database name từ connection string
echo Analyzing connection string...
SET CONNECTION_STRING=!CONNECTION_STRING:"=!
SET CONNECTION_STRING=!CONNECTION_STRING:,=!
FOR /F "tokens=2 delims=:" %%a IN ("!CONNECTION_STRING!") DO (
    SET CONN_DETAILS=%%a
)

SET CONN_DETAILS=!CONN_DETAILS: =!
SET SERVER_NAME=""
SET DATABASE_NAME=""

FOR /F "tokens=1-4 delims=;" %%a IN ("!CONN_DETAILS!") DO (
    SET PART1=%%a
    SET PART2=%%b
    SET PART3=%%c
    SET PART4=%%d
    
    echo Analyzing parts: !PART1!, !PART2!, !PART3!, !PART4!
    
    FOR /F "tokens=1,2 delims==" %%i IN ("!PART1!") DO (
        IF "%%i"=="Server" SET SERVER_NAME=%%j
    )
    
    FOR /F "tokens=1,2 delims==" %%i IN ("!PART2!") DO (
        IF "%%i"=="Database" SET DATABASE_NAME=%%j
    )
    
    FOR /F "tokens=1,2 delims==" %%i IN ("!PART3!") DO (
        IF "%%i"=="Server" SET SERVER_NAME=%%j
        IF "%%i"=="Database" SET DATABASE_NAME=%%j
    )
    
    FOR /F "tokens=1,2 delims==" %%i IN ("!PART4!") DO (
        IF "%%i"=="Server" SET SERVER_NAME=%%j
        IF "%%i"=="Database" SET DATABASE_NAME=%%j
    )
)

echo Using Server: !SERVER_NAME!
echo Using Database: !DATABASE_NAME!

IF "!SERVER_NAME!"=="" (
    echo Could not determine Server name from connection string.
    SET /P SERVER_NAME=Please enter SQL Server name:
)

IF "!DATABASE_NAME!"=="" (
    echo Could not determine Database name from connection string.
    SET /P DATABASE_NAME=Please enter Database name:
)

REM Check if SQL Server authentication or Windows authentication
SET /P USE_SQL_AUTH=Use SQL Server Authentication? (y/n):

IF /I "!USE_SQL_AUTH!"=="y" (
    SET /P SQL_USER=Enter SQL Server username:
    SET /P SQL_PASSWORD=Enter SQL Server password:
    SET AUTH_PARAMS=-U !SQL_USER! -P !SQL_PASSWORD!
) ELSE (
    SET AUTH_PARAMS=-E
)

REM Kiểm tra xem sqlcmd có tồn tại không
sqlcmd -? >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    echo Error: sqlcmd utility not found. Please install SQL Server Command Line Tools.
    goto :exit
)

echo Running SQL script to update database...
sqlcmd -S !SERVER_NAME! -d !DATABASE_NAME! !AUTH_PARAMS! -i "!SCRIPT_PATH!" -o "update_result.txt"

IF %ERRORLEVEL% NEQ 0 (
    echo Error running SQL script. Check update_result.txt for details.
) ELSE (
    echo Database updated successfully. See update_result.txt for details.
    type update_result.txt
)

:exit
echo.
echo Press any key to exit...
pause >nul
endlocal 