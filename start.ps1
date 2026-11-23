
$root = $PSScriptRoot

$backend = Join-Path $root "backend\pitwall-api"
$connector = Join-Path $root "connector"


$configPath = Join-Path $root ".env"

if (-not (Test-Path $configPath)) {
    throw "Brak pliku .env"
}

Get-Content $configPath | ForEach-Object {
    $line = $_.Trim()

    if ($line -and -not $line.StartsWith("#")) {
        $parts = $line -split "=", 2

        if ($parts.Count -eq 2) {
            [Environment]::SetEnvironmentVariable(
                $parts[0].Trim(),
                $parts[1].Trim(),
                "Process"
            )
        }
    }
}

# Laravel
Start-Process powershell.exe `
    -WorkingDirectory $backend `
    -ArgumentList "-NoExit", "-Command", "php artisan serve --host=127.0.0.1 --port=8000"

# Connector C#
Start-Process powershell.exe `
    -WorkingDirectory $connector `
    -ArgumentList "-NoExit", "-Command", "dotnet watch run"
