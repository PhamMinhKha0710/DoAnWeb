# Script cài đặt và cấu hình Gitea cho DevCommunity
param(
    [string]$InstallPath = "C:\gitea",
    [string]$GiteaVersion = "1.21.0",
    [string]$AdminUsername = "admin",
    [string]$AdminPassword = "", # Empty by default for security, will prompt if not provided
    [string]$AdminEmail = "admin@example.com",
    [string]$GiteaPort = "3000"
)

# Kiểm tra quyền admin
function Test-Administrator {
    $user = [Security.Principal.WindowsIdentity]::GetCurrent();
    $principal = New-Object Security.Principal.WindowsPrincipal $user;
    return $principal.IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
}

if (-not (Test-Administrator)) {
    Write-Error "Script này cần quyền quản trị (administrator). Vui lòng chạy PowerShell với quyền quản trị và thử lại."
    exit 1
}

# Tạo thư mục cài đặt nếu chưa tồn tại
if (-not (Test-Path $InstallPath)) {
    Write-Host "Tạo thư mục cài đặt $InstallPath..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
}

# Tạo các thư mục dữ liệu
$DataPath = Join-Path $InstallPath "data"
$CustomPath = Join-Path $InstallPath "custom"
$ConfigPath = Join-Path $DataPath "gitea"
$LogPath = Join-Path $DataPath "log"

@($DataPath, $CustomPath, $ConfigPath, $LogPath) | ForEach-Object {
    if (-not (Test-Path $_)) {
        New-Item -ItemType Directory -Path $_ -Force | Out-Null
    }
}

# Tải Gitea nếu chưa có
$GiteaExe = Join-Path $InstallPath "gitea.exe"
if (-not (Test-Path $GiteaExe)) {
    $DownloadUrl = "https://dl.gitea.io/gitea/$GiteaVersion/gitea-$GiteaVersion-windows-4.0-amd64.exe"
    Write-Host "Đang tải Gitea từ $DownloadUrl..." -ForegroundColor Yellow
    
    try {
        Invoke-WebRequest -Uri $DownloadUrl -OutFile $GiteaExe
        Write-Host "Gitea đã được tải thành công." -ForegroundColor Green
    }
    catch {
        Write-Error "Không thể tải Gitea. Lỗi: $_"
        exit 1
    }
}

# Tạo file cấu hình Gitea
$AppIniPath = Join-Path $ConfigPath "app.ini"
if (-not (Test-Path $AppIniPath)) {
    Write-Host "Tạo file cấu hình Gitea..." -ForegroundColor Yellow
    
    $AppIniContent = @"
APP_NAME = DevCommunity Repositories
RUN_MODE = prod
RUN_USER = COMPUTERNAME

[server]
PROTOCOL = http
DOMAIN = localhost
HTTP_PORT = $GiteaPort
ROOT_URL = http://localhost:$GiteaPort/
DISABLE_SSH = false
SSH_PORT = 2222
OFFLINE_MODE = false

[database]
DB_TYPE = sqlite3
PATH = data/gitea/gitea.db

[repository]
ROOT = $DataPath/repositories

[security]
INSTALL_LOCK = true

[service]
REGISTER_EMAIL_CONFIRM = false
ENABLE_NOTIFY_MAIL = false
DISABLE_REGISTRATION = false
ENABLE_CAPTCHA = false
REQUIRE_SIGNIN_VIEW = false

[picture]
DISABLE_GRAVATAR = false

[session]
PROVIDER = file

[log]
MODE = file
LEVEL = Info
ROOT_PATH = $LogPath
"@
    
    # Ghi nội dung cấu hình vào file
    Set-Content -Path $AppIniPath -Value $AppIniContent
    Write-Host "File cấu hình Gitea đã được tạo thành công." -ForegroundColor Green
}

# Cài đặt Gitea như một dịch vụ Windows
$ServiceName = "gitea"
$Service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($null -eq $Service) {
    Write-Host "Cài đặt Gitea như một dịch vụ Windows..." -ForegroundColor Yellow
    
    # Sử dụng NSSM để cài đặt dịch vụ
    $NssmPath = Join-Path $InstallPath "nssm.exe"
    
    if (-not (Test-Path $NssmPath)) {
        # Tải NSSM
        $NssmUrl = "https://nssm.cc/release/nssm-2.24.zip"
        $NssmZip = Join-Path $InstallPath "nssm.zip"
        
        Write-Host "Đang tải NSSM..." -ForegroundColor Yellow
        Invoke-WebRequest -Uri $NssmUrl -OutFile $NssmZip
        
        # Giải nén
        Expand-Archive -Path $NssmZip -DestinationPath $InstallPath -Force
        
        # Tìm file nssm.exe
        $NssmExe = Get-ChildItem -Path $InstallPath -Recurse -Filter "nssm.exe" | Where-Object { $_.DirectoryName -like "*win64*" } | Select-Object -First 1
        
        if ($null -ne $NssmExe) {
            Copy-Item $NssmExe.FullName -Destination $NssmPath
            Remove-Item $NssmZip -Force
        }
        else {
            Write-Error "Không thể tìm thấy nssm.exe sau khi giải nén."
            exit 1
        }
    }
    
    # Cài đặt dịch vụ
    & $NssmPath install $ServiceName $GiteaExe "web"
    & $NssmPath set $ServiceName AppDirectory $InstallPath
    & $NssmPath set $ServiceName DisplayName "Gitea"
    & $NssmPath set $ServiceName Description "Gitea Git Service"
    & $NssmPath set $ServiceName Start SERVICE_AUTO_START
    & $NssmPath set $ServiceName AppStdout "$LogPath\gitea.log"
    & $NssmPath set $ServiceName AppStderr "$LogPath\gitea.err.log"
    
    Write-Host "Dịch vụ Gitea đã được cài đặt thành công." -ForegroundColor Green
}

# Khởi động dịch vụ Gitea
try {
    Start-Service $ServiceName
    Write-Host "Dịch vụ Gitea đã được khởi động." -ForegroundColor Green
}
catch {
    Write-Error "Không thể khởi động dịch vụ Gitea. Lỗi: $_"
}

# Kiểm tra xem Gitea có đang chạy không
$GiteaRunning = $false
$MaxWait = 60
$WaitSeconds = 0

Write-Host "Đang chờ Gitea khởi động..." -ForegroundColor Yellow

while (-not $GiteaRunning -and $WaitSeconds -lt $MaxWait) {
    try {
        $Response = Invoke-RestMethod -Uri "http://localhost:$GiteaPort/api/v1/version" -Method Get -ErrorAction SilentlyContinue
        if ($null -ne $Response) {
            $GiteaRunning = $true
            Write-Host "Gitea đã khởi động thành công (Phiên bản $($Response.version))." -ForegroundColor Green
        }
    }
    catch {
        Start-Sleep -Seconds 1
        $WaitSeconds++
    }
}

if (-not $GiteaRunning) {
    Write-Warning "Không thể xác nhận Gitea đã khởi động sau $MaxWait giây. Vui lòng kiểm tra logs để biết thêm chi tiết."
}

# Hiển thị thông tin quan trọng
Write-Host "`nThông tin cài đặt Gitea:" -ForegroundColor Cyan
Write-Host "------------------------" -ForegroundColor Cyan
Write-Host "Thư mục cài đặt: $InstallPath"
Write-Host "Thư mục dữ liệu: $DataPath"
Write-Host "Thư mục cấu hình: $ConfigPath"
Write-Host "Log path: $LogPath"
Write-Host "URL: http://localhost:$GiteaPort/"
Write-Host "Dịch vụ Windows: $ServiceName"
Write-Host "`nBước tiếp theo:" -ForegroundColor Cyan
Write-Host "1. Mở trình duyệt và truy cập http://localhost:$GiteaPort/"
Write-Host "2. Hoàn thành thiết lập ban đầu"
Write-Host "3. Tạo Admin API Token từ Settings > Applications"
Write-Host "4. Cập nhật file appsettings.json của DevCommunity với token đã tạo"
Write-Host "`nCảm ơn bạn đã sử dụng script này để cài đặt Gitea!" -ForegroundColor Green 