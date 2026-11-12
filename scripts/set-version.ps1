# 根据当前 git tag 生成 version.props。
#
# 用法：
#   pwsh scripts/set-version.ps1          # 从当前 HEAD 的 tag 读取版本
#   pwsh scripts/set-version.ps1 -Clear   # 删除 version.props，回退到 0.0.0-dev
#
# 版本规则：
#   tag v1.2.3        -> 1.2.3
#   tag 1.2.3         -> 1.2.3
#   tag v1.2.3-beta.1 -> 1.2.3-beta.1
#   无 tag            -> 0.0.0-dev

[CmdletBinding()]
param(
    [switch]$Clear
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$versionFile = Join-Path $repoRoot 'version.props'

if ($Clear) {
    if (Test-Path $versionFile) {
        Remove-Item $versionFile -Force
        Write-Host "已删除 version.props，版本回退到 0.0.0-dev" -ForegroundColor Yellow
    }
    else {
        Write-Host "version.props 不存在，无需清理" -ForegroundColor Yellow
    }
    return
}

Push-Location $repoRoot
try {
    # git 在无 tag 时会向 stderr 写入 fatal 信息；这里临时放宽错误策略，
    # 只依据退出码判断，避免把正常的“无 tag”当成脚本失败。
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $tag = git describe --tags --exact-match HEAD 2>$null | Out-String
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }
}
finally {
    Pop-Location
}

if ($exitCode -ne 0 -or [string]::IsNullOrWhiteSpace($tag)) {
    $version = '0.0.0-dev'
    Write-Host "当前提交没有 tag，使用回退版本 $version" -ForegroundColor Yellow
}
else {
    $version = $tag.Trim()
    if ($version.StartsWith('v') -or $version.StartsWith('V')) {
        $version = $version.Substring(1)
    }
    Write-Host "从 tag '$version' 得到版本 $version" -ForegroundColor Green
}

$content = @"
<Project>
	<PropertyGroup>
		<VersionFromTag>$version</VersionFromTag>
	</PropertyGroup>
</Project>
"@

Set-Content -Path $versionFile -Value $content -Encoding UTF8
Write-Host "已写入 $versionFile" -ForegroundColor Green
