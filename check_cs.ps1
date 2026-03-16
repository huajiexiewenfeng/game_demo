function Test-UTF8 {
    param($path)
    $bytes = [System.IO.File]::ReadAllBytes($path)
    $errors = @()
    $i = 0
    while ($i -lt $bytes.Length) {
        $b = $bytes[$i]
        if ($b -le 0x7F) { $i++; continue }
        elseif ($b -ge 0xC2 -and $b -le 0xDF) { $need = 1 }
        elseif ($b -ge 0xE0 -and $b -le 0xEF) { $need = 2 }
        elseif ($b -ge 0xF0 -and $b -le 0xF4) { $need = 3 }
        else { $errors += ("0x" + $b.ToString("X2") + " at " + $i); $i++; continue }
        $ok = $true
        for ($j = 1; $j -le $need; $j++) {
            $ci = $i + $j
            if ($ci -ge $bytes.Length -or $bytes[$ci] -lt 0x80 -or $bytes[$ci] -gt 0xBF) { $ok = $false }
        }
        if (-not $ok) { $errors += ("bad seq 0x" + $b.ToString("X2") + " at " + $i) }
        $i += $need + 1
    }
    return $errors
}

$files = Get-ChildItem -Path "D:/game/demo/Scripts" -Filter "*.cs"
foreach ($f in $files) {
    $errs = Test-UTF8 $f.FullName
    if ($errs.Count -eq 0) {
        Write-Host ("OK: " + $f.Name)
    } else {
        Write-Host ("BAD: " + $f.Name + " (" + $errs.Count + " errors, first: " + $errs[0] + ")")
    }
}
