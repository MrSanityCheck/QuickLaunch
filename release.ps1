param(
    [switch]$Major
)

$ErrorActionPreference = 'Stop'

# Read latest tag from the remote so we don't depend on a full local fetch
$latest = git describe --tags --abbrev=0 2>$null
if (-not $latest) { $latest = "v0.0.0" }

if ($latest -notmatch '^v?(\d+)\.(\d+)\.\d+$') {
    throw "Could not parse latest tag: $latest"
}

[int]$maj = $Matches[1]
[int]$min = $Matches[2]

if ($Major) {
    $maj++
    $min = 0
    Write-Host "Major bump: $latest -> v$maj.$min.0" -ForegroundColor Yellow
} else {
    $min++
    Write-Host "Minor bump: $latest -> v$maj.$min.0" -ForegroundColor Cyan
}

$newTag = "v$maj.$min.0"

git tag $newTag
git push origin $newTag

Write-Host "Tag $newTag pushed. Watch the build at:" -ForegroundColor Green
Write-Host "  https://github.com/MrSanityCheck/QuickLaunch/actions" -ForegroundColor Green
