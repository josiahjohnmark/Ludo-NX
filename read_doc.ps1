Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead('Ludo NX.docx')
$entry = $zip.GetEntry('word/document.xml')
$stream = $entry.Open()
$reader = New-Object System.IO.StreamReader($stream)
$content = $reader.ReadToEnd()
$reader.Close()
$stream.Close()
$zip.Dispose()
$matches = [regex]::Matches($content, '<w:t[^>]*>(.*?)</w:t>')
$sb = New-Object System.Text.StringBuilder
foreach ($m in $matches) {
    [void]$sb.AppendLine($m.Groups[1].Value)
}
$sb.ToString() | Out-File -FilePath 'doc_dump.txt' -Encoding utf8
Write-Output "Done regex, length: $($sb.Length)"
