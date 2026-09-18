param(
    [ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z]+(\.[0-9A-Za-z]+)*)?$')]
    [string]$PackageVersion
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Invoke-DotNet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet failed with exit code $LASTEXITCODE."
    }
}

$repoRoot = Split-Path $PSScriptRoot -Parent
Push-Location $repoRoot
try {
    if (-not $PackageVersion) {
        [xml]$project = Get-Content 'src/Mnemosyne/Mnemosyne.csproj' -Raw
        $PackageVersion = $project.Project.PropertyGroup.Version
    }

    $output = Join-Path $repoRoot "artifacts/verify/$([guid]::NewGuid().ToString('N'))"
    $results = Join-Path $output 'results'
    Invoke-DotNet test Mnemosyne.sln --configuration Release --collect 'XPlat Code Coverage' --results-directory $results --logger trx --verbosity minimal

    $reports = @(Get-ChildItem $results -Directory | Get-ChildItem -File -Filter coverage.cobertura.xml)
    if ($reports.Count -ne 1) {
        throw "Expected one coverage report, found $($reports.Count)."
    }
    [xml]$coverage = Get-Content $reports[0].FullName -Raw
    $culture = [Globalization.CultureInfo]::InvariantCulture
    $lineRate = [double]::Parse($coverage.coverage.'line-rate', $culture)
    $branchRate = [double]::Parse($coverage.coverage.'branch-rate', $culture)
    Write-Host ('Coverage: {0:P1} lines, {1:P1} branches.' -f $lineRate, $branchRate)
    if ($lineRate -lt 0.80 -or $branchRate -lt 0.70) {
        throw 'Coverage must be at least 80% lines and 70% branches.'
    }

    Invoke-DotNet pack src/Mnemosyne/Mnemosyne.csproj --configuration Release --no-restore --output $output "-p:PackageVersion=$PackageVersion"
    $package = Join-Path $output "Doticca.Mnemosyne.$PackageVersion.nupkg"
    $symbols = Join-Path $output "Doticca.Mnemosyne.$PackageVersion.snupkg"
    if (-not (Test-Path $symbols)) {
        throw 'The symbols package is missing.'
    }

    $archive = [IO.Compression.ZipFile]::OpenRead($package)
    try {
        foreach ($required in @('LICENSE', 'README.md', 'lib/net10.0/Mnemosyne.dll', 'lib/net10.0/Mnemosyne.xml')) {
            if ($null -eq $archive.GetEntry($required)) {
                throw "Package is missing $required."
            }
        }
        $reader = [IO.StreamReader]::new($archive.GetEntry('Doticca.Mnemosyne.nuspec').Open())
        try {
            [xml]$manifest = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
        $metadata = $manifest.package.metadata
        if ($metadata.license.type -ne 'expression' -or $metadata.license.InnerText -ne 'MIT') {
            throw 'Package must declare the MIT license expression.'
        }
        if ($metadata.version -ne $PackageVersion -or $metadata.id -ne 'Doticca.Mnemosyne') {
            throw 'Unexpected package identity.'
        }
        if ($metadata.repository.url -ne 'https://github.com/teamdoticca/mnemosyne' -or
            [string]::IsNullOrWhiteSpace($metadata.repository.commit)) {
            throw 'Package must identify its source repository and commit.'
        }
    }
    finally {
        $archive.Dispose()
    }

    $consumer = 'tests/Mnemosyne.PackageSmoke/Mnemosyne.PackageSmoke.csproj'
    Invoke-DotNet restore $consumer --source $output --packages (Join-Path $output 'consumer-packages') "-p:MnemosynePackageVersion=$PackageVersion" '-p:NuGetAudit=false'
    Invoke-DotNet run --project $consumer --configuration Release --no-restore "-p:MnemosynePackageVersion=$PackageVersion"
    Write-Host "Verified package: $package"
}
finally {
    Pop-Location
}