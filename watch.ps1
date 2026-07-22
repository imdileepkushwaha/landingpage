# Softflip admin/dev server with auto-reload
# Usage: .\watch.ps1
# CSS / Razor / C# changes apply without manual restart.

Set-Location $PSScriptRoot
dotnet watch run --launch-profile http --non-interactive
