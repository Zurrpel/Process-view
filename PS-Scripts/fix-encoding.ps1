<# 
Пересохранение всех .ps1-скриптов в текущей директории в UTF-8 с BOM
(для корректного чтения кириллицы в PowerShell 5.1).

Запуск из директории со скриптами:

    .\fix-encoding.ps1

Обрабатываются все *.ps1 рядом с этим файлом, кроме самого fix-encoding.ps1.
#>

$dir = if ($PSScriptRoot) { $PSScriptRoot } else { (Get-Location).Path }
$utf8Bom = [System.Text.UTF8Encoding]::new($true)

Get-ChildItem -Path $dir -Filter '*.ps1' -File |
    Where-Object { $_.Name -ne 'fix-encoding.ps1' } |
    ForEach-Object {
        $path = $_.FullName

        try {
            # Читаем как есть и пересохраняем как UTF-8 с BOM
            $content = [System.IO.File]::ReadAllText($path)
            [System.IO.File]::WriteAllText($path, $content, $utf8Bom)
            Write-Host "Пересохранено в UTF-8 с BOM: $path" -ForegroundColor Green
        } catch {
            Write-Warning ("Failed to process file {0}: {1}" -f $path, $_)
        }
    }

