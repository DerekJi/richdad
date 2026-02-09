# 批量更新命名空间脚本

Write-Host "开始更新项目配置和命名空间..." -ForegroundColor Green

# 1. 更新 .csproj 文件名
Write-Host "`n步骤 1/4: 更新 .csproj 文件名..." -ForegroundColor Yellow
Rename-Item "src/Trading.Infras.Data/Trading.AlertSystem.Data.csproj" "Trading.Infras.Data.csproj"
Rename-Item "src/Trading.Infras.Service/Trading.AlertSystem.Service.csproj" "Trading.Infras.Service.csproj"
Rename-Item "src/Trading.Infras.Web/Trading.AlertSystem.Web.csproj" "Trading.Infras.Web.csproj"

# 2. 更新 .csproj 文件内容（RootNamespace 和 AssemblyName）
Write-Host "`n步骤 2/4: 更新 .csproj 配置..." -ForegroundColor Yellow
Get-ChildItem -Path "src/Trading.Infras.*" -Recurse -Filter "*.csproj" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $content = $content -replace '<RootNamespace>Trading\.AlertSystem', '<RootNamespace>Trading.Infras'
    $content = $content -replace '<AssemblyName>Trading\.AlertSystem', '<AssemblyName>Trading.Infras'
    $content = $content -replace 'Trading\.AlertSystem\.Data\\', 'Trading.Infras.Data\'
    $content = $content -replace 'Trading\.AlertSystem\.Service\\', 'Trading.Infras.Service\'
    Set-Content $_.FullName $content -NoNewline
    Write-Host "  ✓ $($_.Name)" -ForegroundColor Green
}

# 3. 更新所有 C# 文件的命名空间
Write-Host "`n步骤 3/4: 更新 C# 文件命名空间..." -ForegroundColor Yellow
$csFiles = Get-ChildItem -Path "src/Trading.Infras.*" -Recurse -Filter "*.cs"
$totalFiles = $csFiles.Count
$current = 0

foreach ($file in $csFiles) {
    $current++
    Write-Progress -Activity "更新命名空间" -Status "$current / $totalFiles" -PercentComplete (($current / $totalFiles) * 100)
    
    $content = Get-Content $file.FullName -Raw
    if ($content -match 'namespace Trading\.AlertSystem') {
        $content = $content -replace 'namespace Trading\.AlertSystem\.Data', 'namespace Trading.Infras.Data'
        $content = $content -replace 'namespace Trading\.AlertSystem\.Service', 'namespace Trading.Infras.Service'
        $content = $content -replace 'namespace Trading\.AlertSystem\.Web', 'namespace Trading.Infras.Web'
        $content = $content -replace 'using Trading\.AlertSystem\.Data', 'using Trading.Infras.Data'
        $content = $content -replace 'using Trading\.AlertSystem\.Service', 'using Trading.Infras.Service'
        $content = $content -replace 'using Trading\.AlertSystem\.Web', 'using Trading.Infras.Web'
        Set-Content $file.FullName $content -NoNewline
    }
}
Write-Host "  ✓ 已更新 $totalFiles 个 C# 文件" -ForegroundColor Green

# 4. 更新解决方案文件
Write-Host "`n步骤 4/4: 更新解决方案文件..." -ForegroundColor Yellow
$slnFile = "TradingSystem.sln"
if (Test-Path $slnFile) {
    $content = Get-Content $slnFile -Raw
    $content = $content -replace 'Trading\.AlertSystem\.Data', 'Trading.Infras.Data'
    $content = $content -replace 'Trading\.AlertSystem\.Service', 'Trading.Infras.Service'
    $content = $content -replace 'Trading\.AlertSystem\.Web', 'Trading.Infras.Web'
    Set-Content $slnFile $content -NoNewline
    Write-Host "  ✓ TradingSystem.sln" -ForegroundColor Green
}

Write-Host "`n✅ 所有更新完成！" -ForegroundColor Green
Write-Host "`n下一步: 运行 'dotnet build' 验证编译" -ForegroundColor Cyan
