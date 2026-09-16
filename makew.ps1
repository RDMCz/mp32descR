Get-Item -LiteralPath ".\PUBLISHED" -ErrorAction SilentlyContinue | Remove-Item -Recurse

dotnet publish .\mp32descR\mp32descR.csproj -r win-x64 -c Release -o .\PUBLISHED

Remove-Item ".\PUBLISHED" -Recurse -Include *.pdb

Write-Host "Done. Press enter to dismiss."
Read-Host