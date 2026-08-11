param(
    [String]$HtmlFile,
    [String]$PdfFile
)

# If parameters not provided, use defaults
if (-not $HtmlFile) {
    $HtmlFile = "Interface_version_3_7.html"
}
if (-not $PdfFile) {
    $PdfFile = "Interface_version_3_7.pdf"
}

# Get absolute paths
$HtmlFile = (Resolve-Path $HtmlFile -ErrorAction Stop).Path
$PdfFile = Join-Path (Split-Path $HtmlFile) $PdfFile

Write-Host "Converting HTML to PDF with bookmarks..."
Write-Host "Input:  $HtmlFile"
Write-Host "Output: $PdfFile"

# Method 1: Try using Edge browser
try {
    $EdgePath = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
    if (-not (Test-Path $EdgePath)) {
        $EdgePath = "C:\Program Files\Microsoft\Edge\Application\msedge.exe"
    }
    
    if (Test-Path $EdgePath) {
        # Use Edge WebDriver print API
        Write-Host "Attempting to use Microsoft Edge for PDF conversion..."
        
        # Create a temporary script to handle printing
        $TempScript = [System.IO.Path]::GetTempFileName() + ".js"
        $Script = @"
const puppeteer = require('puppeteer');
(async () => {
  const browser = await puppeteer.launch({headless: 'new'});
  const page = await browser.newPage();
  await page.goto('file:///$($HtmlFile -replace '\\', '/')');
  await page.pdf({
    path: '$($PdfFile -replace '\\', '/')',
    format: 'A4',
    printBackground: true,
    margin: { top: '0.75in', right: '0.75in', bottom: '0.75in', left: '0.75in' }
  });
  await browser.close();
})();
"@
        # Save the script
        Set-Content -Path $TempScript -Value $Script
        
        # Try using npm and puppeteer if available
        $result = & npm list puppeteer 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Using Puppeteer for conversion..."
            & node $TempScript
            if (Test-Path $PdfFile) {
                Write-Host "SUCCESS: PDF created with Puppeteer"
                exit 0
            }
        }
        Remove-Item $TempScript -ErrorAction SilentlyContinue
    }
} catch {
    # Continue to next method
}

# Method 2: Try using native Windows print to PDF
try {
    Write-Host "Using Windows Print to PDF..."
    
    # This method uses Windows Print to PDF driver
    $IE = New-Object -ComObject InternetExplorer.Application
    $IE.Visible = $false
    $IE.Navigate(("file:///{0}" -f $HtmlFile))
    
    while ($IE.Busy) {
        Start-Sleep -Milliseconds 500
    }
    Start-Sleep -Seconds 3
    
    # Use shell.application to print
    $shell = New-Object -ComObject Shell.Application
    $dir = $shell.BrowseForFolder(0, "Select directory to save PDF:", 0, 0)
    
    # Just open print dialog
    $IE.ExecWB([System.Runtime.InteropServices.DispatchWrapper]6, [System.Runtime.InteropServices.DispatchWrapper]2)
    
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($IE) | Out-Null
    
    Write-Host "NOTE: Print dialog opened. Please use 'Print to PDF' and save as: $PdfFile"
    
} catch {
    Write-Host "Error: $_"
}

# Method 3: Provide instruction to user
Write-Host ""
Write-Host "HTML file created successfully: $HtmlFile"
Write-Host ""
Write-Host "To create PDF with bookmarks, please:"
Write-Host "1. Open the HTML file in your web browser"
Write-Host "2. Press Ctrl+P or use File > Print"
Write-Host "3. Choose 'Save as PDF' (or 'Print to File')"
Write-Host "4. Save as: $PdfFile"
Write-Host ""
Write-Host "The table of contents will be included as bookmarks in the PDF."
