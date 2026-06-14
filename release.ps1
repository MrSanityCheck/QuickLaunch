param(
    [switch]$Major,
    [switch]$Patch
)

$ErrorActionPreference = 'Stop'

$latest = git describe --tags --abbrev=0 2>$null
if (-not $latest) { $latest = "v0.0.0" }

if ($latest -notmatch '^v?(\d+)\.(\d+)\.(\d+)$') {
    throw "Could not parse latest tag: $latest"
}

[int]$maj = $Matches[1]
[int]$min = $Matches[2]
[int]$pat = $Matches[3]

if ($Major) {
    $maj++; $min = 0; $pat = 0
    Write-Host "Major bump: $latest -> v$maj.$min.$pat" -ForegroundColor Yellow
} elseif ($Patch) {
    $pat++
    Write-Host "Patch bump: $latest -> v$maj.$min.$pat" -ForegroundColor Cyan
} else {
    $min++; $pat = 0
    Write-Host "Minor bump: $latest -> v$maj.$min.$pat" -ForegroundColor Cyan
}

$newTag = "v$maj.$min.$pat"

git tag $newTag
git push origin $newTag

Write-Host "Tag $newTag pushed. Watch the build at:" -ForegroundColor Green
Write-Host "  https://github.com/MrSanityCheck/QuickLaunch/actions" -ForegroundColor Green
