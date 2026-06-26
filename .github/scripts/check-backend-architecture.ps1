$ErrorActionPreference = "Stop"

$failed = $false

function Assert-Contains {
    param(
        [string] $File,
        [string] $Expected
    )

    $content = Get-Content -LiteralPath $File -Raw
    if (-not $content.Contains($Expected)) {
        Write-Host "::error file=$File::Missing expected project reference: $Expected"
        $script:failed = $true
    }
}

function Assert-NotContains {
    param(
        [string] $File,
        [string] $Unexpected
    )

    $content = Get-Content -LiteralPath $File -Raw
    if ($content.Contains($Unexpected)) {
        Write-Host "::error file=$File::Forbidden project reference: $Unexpected"
        $script:failed = $true
    }
}

$apiProject = "CleaningServiceApp/CleaningService.API/CleaningService.API.csproj"
$bllProject = "CleaningServiceApp/BLL/Cleaning.BLL.csproj"
$dalProject = "CleaningServiceApp/DAL/Cleaning.DAL.csproj"

Assert-Contains $apiProject "..\BLL\Cleaning.BLL.csproj"
Assert-NotContains $apiProject "..\DAL\Cleaning.DAL.csproj"

Assert-Contains $bllProject "..\DAL\Cleaning.DAL.csproj"
Assert-NotContains $bllProject "..\CleaningService.API\CleaningService.API.csproj"

Assert-NotContains $dalProject "..\BLL\Cleaning.BLL.csproj"
Assert-NotContains $dalProject "..\CleaningService.API\CleaningService.API.csproj"

if ($failed) {
    exit 1
}
