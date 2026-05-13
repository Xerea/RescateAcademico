param(
    [Parameter(Mandatory=$true)][string]$InputDocx,
    [Parameter(Mandatory=$true)][string]$OutputPdf
)

$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0

$outputDir = Split-Path -Parent $OutputPdf
if (-not (Test-Path -LiteralPath $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

try {
        $doc = $word.Documents.Open($InputDocx, $false, $false)
    try {
        $doc.Fields.Update() | Out-Null
        $doc.TablesOfContents | ForEach-Object { $_.Update() | Out-Null }
        $doc.Save()
        $doc.ExportAsFixedFormat($OutputPdf, 17)
    }
    finally {
        $doc.Close($false)
    }
}
finally {
    $word.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null
}
