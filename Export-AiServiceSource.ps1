<#
.SYNOPSIS
    Collects every source file in the AI service into one printable HTML page.

.DESCRIPTION
    Open the result in a browser and use Ctrl+P -> "Save as PDF".
    Page breaks are set so each file starts on a fresh page.

.EXAMPLE
    .\Export-AiServiceSource.ps1
    .\Export-AiServiceSource.ps1 -Source "server\services\tracking-service" -Output tracking.html
#>

param(
    [string]$Source = "server\services\ai-service\SafeRide.Ai",
    [string]$Output = "ai-service-source.html",
    [string]$Title  = "SafeRide AI - AI service source"
)

if (-not (Test-Path $Source)) {
    Write-Error "Source folder not found: $Source"
    exit 1
}

$root = (Resolve-Path $Source).Path

# Code and configuration worth printing. Build output and the EF designer files
# are excluded: they are generated, enormous, and nobody reads them.
$include = '*.cs', '*.csproj', '*.slnx', '*.json', 'Dockerfile'
$exclude = '\\bin\\', '\\obj\\', '\.Designer\.cs$', 'ModelSnapshot\.cs$'

$files = Get-ChildItem -Path $root -Recurse -File -Include $include |
    Where-Object {
        $path = $_.FullName
        -not ($exclude | Where-Object { $path -match $_ })
    } |
    Sort-Object FullName

if ($files.Count -eq 0) {
    Write-Error "No files matched under $root"
    exit 1
}

function ConvertTo-HtmlText([string]$text) {
    $text.Replace('&', '&amp;').Replace('<', '&lt;').Replace('>', '&gt;')
}

$generated = Get-Date -Format "d MMMM yyyy, HH:mm"

$builder = [System.Text.StringBuilder]::new()

[void]$builder.AppendLine(@"
<!doctype html>
<html>
<head>
<meta charset="utf-8">
<title>$Title</title>
<style>
  body   { font: 13px/1.5 -apple-system, Segoe UI, system-ui, sans-serif; color: #1e293b; margin: 32px; }
  h1     { font-size: 22px; margin: 0 0 4px; }
  .meta  { color: #64748b; font-size: 12px; margin-bottom: 28px; }
  .toc   { margin-bottom: 40px; }
  .toc h2 { font-size: 14px; text-transform: uppercase; letter-spacing: .04em; color: #64748b; }
  .toc ol { padding-left: 20px; }
  .toc a { color: #0369a1; text-decoration: none; }
  .file  { page-break-before: always; break-before: page; }
  .file h2 { font-size: 14px; background: #f1f5f9; border-left: 3px solid #0369a1;
             padding: 8px 10px; margin: 0 0 2px; font-family: ui-monospace, Consolas, monospace; }
  .file .lines { color: #64748b; font-size: 11px; margin: 0 0 10px 13px; }
  pre    { font: 11px/1.45 ui-monospace, Consolas, "Courier New", monospace;
           background: #fafafa; border: 1px solid #e2e8f0; border-radius: 4px;
           padding: 12px; white-space: pre-wrap; word-wrap: break-word; }
  @media print {
    body { margin: 12mm; }
    .toc { page-break-after: always; }
  }
</style>
</head>
<body>
<h1>$Title</h1>
<div class="meta">$($files.Count) files &middot; generated $generated</div>
<div class="toc">
<h2>Contents</h2>
<ol>
"@)

$index = 0
foreach ($file in $files) {
    $index++
    $relative = $file.FullName.Substring($root.Length).TrimStart('\', '/')
    [void]$builder.AppendLine("<li><a href=""#f$index"">$(ConvertTo-HtmlText $relative)</a></li>")
}

[void]$builder.AppendLine("</ol></div>")

$index = 0
foreach ($file in $files) {
    $index++
    $relative = $file.FullName.Substring($root.Length).TrimStart('\', '/')
    $content  = Get-Content -Path $file.FullName -Raw -Encoding UTF8
    $lineCount = ($content -split "`n").Count

    [void]$builder.AppendLine("<section class=""file"" id=""f$index"">")
    [void]$builder.AppendLine("<h2>$(ConvertTo-HtmlText $relative)</h2>")
    [void]$builder.AppendLine("<p class=""lines"">$lineCount lines</p>")
    [void]$builder.AppendLine("<pre>$(ConvertTo-HtmlText $content)</pre>")
    [void]$builder.AppendLine("</section>")
}

[void]$builder.AppendLine("</body></html>")

Set-Content -Path $Output -Value $builder.ToString() -Encoding UTF8

Write-Host "Wrote $Output"
Write-Host "$($files.Count) files included."
Write-Host ""
Write-Host "Open it, then Ctrl+P -> Destination 'Save as PDF'."
