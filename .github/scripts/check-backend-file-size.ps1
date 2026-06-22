$ErrorActionPreference = "Stop"

$limit = 300
$baselineFile = ".github/quality/backend-file-size-baseline.txt"
$failed = $false
$baseline = @{}

if (Test-Path -LiteralPath $baselineFile) {
    Get-Content -LiteralPath $baselineFile |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object {
            $baseline[$_.Replace("\", "/")] = $true
        }
}

Get-ChildItem -Path CleaningServiceApp -Recurse -File -Filter *.cs |
    Where-Object {
        $path = $_.FullName.Replace("\", "/")
        $path -notmatch "/bin/" -and
        $path -notmatch "/obj/" -and
        $path -notmatch "/CleaningServiceApp/DAL/Migrations/"
    } |
    ForEach-Object {
        $relativePath = (Resolve-Path -LiteralPath $_.FullName -Relative).TrimStart(".", "\", "/").Replace("\", "/")
        $lineCount = (Get-Content -LiteralPath $_.FullName).Count

        if ($lineCount -gt $limit -and -not $baseline.ContainsKey($relativePath)) {
            Write-Host "::error file=$relativePath::File has $lineCount lines; limit is $limit. Split it or add a justified baseline entry."
            $failed = $true
        }
    }

if ($failed) {
    exit 1
}
