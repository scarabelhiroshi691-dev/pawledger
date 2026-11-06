$legacyDirectory = 'D:\Codex\PetShopData'
$dataDirectory = if ($env:PETSHOP_DATA_DIR) { $env:PETSHOP_DATA_DIR } elseif (Test-Path -LiteralPath $legacyDirectory) { $legacyDirectory } else { Join-Path $env:LOCALAPPDATA 'PawLedger' }
New-Item -ItemType Directory -Path $dataDirectory -Force | Out-Null
Start-Process explorer.exe -ArgumentList $dataDirectory
