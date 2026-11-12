# 校验 Cobertura 覆盖率报告的行覆盖率是否达到阈值。
#
# 用法：
#   pwsh scripts/check-coverage.ps1                       # 默认阈值 90%
#   pwsh scripts/check-coverage.ps1 -Threshold 85         # 自定义阈值
#   pwsh scripts/check-coverage.ps1 -Path ./coverage      # 指定报告目录
#
# 退出码：
#   0  覆盖率达标
#   1  覆盖率低于阈值
#   2  未找到覆盖率报告

[CmdletBinding()]
param(
    [string]$Path = './coverage',
    [double]$Threshold = 90
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $Path)) {
    Write-Host "未找到覆盖率目录：$Path" -ForegroundColor Red
    exit 2
}

$reports = @(Get-ChildItem -Path $Path -Recurse -Filter 'coverage.cobertura.xml')
if ($reports.Count -eq 0) {
    Write-Host "覆盖率目录中没有 coverage.cobertura.xml：$Path" -ForegroundColor Red
    exit 2
}

# 同一个程序集可能被多个测试项目各报告一次；按「类名 + 行号」去重，
# 保证同一行无论被报告多少次都只统计一次。
$lines = @{}

foreach ($report in $reports) {
    [xml]$xml = Get-Content -Path $report.FullName

    foreach ($package in $xml.coverage.packages.package) {
        foreach ($class in $package.classes.class) {
            foreach ($line in $class.lines.line) {
                $key = '{0}:{1}' -f $class.name, $line.number

                if (-not $lines.ContainsKey($key)) {
                    $lines[$key] = @{ Class = $class.name; Covered = $false }
                }

                if ([int]$line.hits -gt 0) {
                    $lines[$key].Covered = $true
                }
            }
        }
    }
}

$totalLines = $lines.Count
$coveredLines = @($lines.Values | Where-Object { $_.Covered }).Count

if ($totalLines -eq 0) {
    Write-Host '覆盖率报告中没有可统计的行。' -ForegroundColor Red
    exit 2
}

$actual = [math]::Round($coveredLines / $totalLines * 100, 2)

Write-Host ''
Write-Host '覆盖率最低的类：' -ForegroundColor Cyan

$lines.Values |
    Group-Object Class |
    ForEach-Object {
        $total = $_.Count
        $covered = @($_.Group | Where-Object { $_.Covered }).Count

        [pscustomobject]@{
            Name    = $_.Name
            Percent = [math]::Round($covered / $total * 100, 2)
            Covered = $covered
            Total   = $total
        }
    } |
    Sort-Object Percent |
    Select-Object -First 10 |
    ForEach-Object {
        Write-Host ("  {0,6:N2}%  {1,4}/{2,4}  {3}" -f $_.Percent, $_.Covered, $_.Total, $_.Name)
    }

Write-Host ''
Write-Host ("行覆盖率：{0}/{1} = {2:N2}%（阈值 {3:N2}%）" -f $coveredLines, $totalLines, $actual, $Threshold)

if ($actual -lt $Threshold) {
    Write-Host "覆盖率未达到阈值，构建失败。" -ForegroundColor Red
    exit 1
}

Write-Host '覆盖率达标。' -ForegroundColor Green
exit 0
