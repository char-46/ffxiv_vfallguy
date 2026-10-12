# Merges [capture] lines from Dalamud logs (current + rotated) into a persistent archive.
# Safe to run repeatedly: exact-line dedupe. Run after each capture session.
# Usage: pwsh merge_captures.ps1
$archive = "A:\ffxiv_vfallguy\captures\all_captures.log"
$logDir = "C:\Users\char46\AppData\Roaming\XIVLauncherCN"
New-Item -ItemType Directory -Path (Split-Path $archive) -Force | Out-Null

$seen = [System.Collections.Generic.HashSet[string]]::new()
if (Test-Path $archive) {
    foreach ($l in [System.IO.File]::ReadLines($archive)) { [void]$seen.Add($l) }
}
$before = $seen.Count

$new = [System.Collections.Generic.List[string]]::new()
foreach ($f in @("dalamud.log", "dalamud.old.log")) {
    $p = Join-Path $logDir $f
    if (Test-Path $p) {
        foreach ($m in Select-String -Path $p -Pattern "\[capture\]") {
            if ($seen.Add($m.Line)) { $new.Add($m.Line) }
        }
    }
}
if ($new.Count -gt 0) {
    [System.IO.File]::AppendAllLines($archive, $new)
}
"appended: $($new.Count), archive total: $($before + $new.Count)"
# summary
$groups = Select-String -Path $archive -Pattern "\[capture\] (AE|SC) (\d+)" | ForEach-Object { if ($_.Line -match '\] (AE|SC) (\d+)') { $matches[2] } } | Group-Object | Sort-Object Name
"id counts: " + (($groups | ForEach-Object { "$($_.Name):$($_.Count)" }) -join ' ')
$buffs = Select-String -Path $archive -Pattern "\[capture\] BUFF" | ForEach-Object { $_.Line -replace '^.*BUFF ', 'BUFF ' } | Group-Object | Sort-Object Count -Descending
if ($buffs) { "buffs: " + (($buffs | ForEach-Object { "$($_.Name) x$($_.Count)" }) -join ' | ') } else { "buffs: none" }
