@echo off
REM Pin Bar策略参数优化结果分析工具

cd /d "%~dp0"

echo ========================================
echo   Pin Bar策略参数优化结果分析工具
echo ========================================
echo.

REM 运行分析命令
dotnet run --configuration Release -- analyze

echo.
echo 📄 查看报告文件:
dir results\optimization_report_*.md /o-d
