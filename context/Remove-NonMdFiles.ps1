$Root = '.'
$PreviewOnly = $true # set to $false when you’re ready to delete for real

$items = Get-ChildItem -Path $Root -File -Recurse |
         Where-Object { $_.Extension -ne '.md' -and $_.Extension -ne '.ps1' }

if ($PreviewOnly) {
    Write-Host "Preview mode: the following files *would* be deleted:`n"
    $items | Select-Object FullName
} else {
    $items | Remove-Item -Force
    Write-Host "Deleted $($items.Count) non-Markdown file(s)."
}
