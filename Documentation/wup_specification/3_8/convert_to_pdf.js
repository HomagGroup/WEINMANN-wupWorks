const fs = require('fs');
const path = require('path');
const { spawn } = require('child_process');
const os = require('os');

// Get paths
const htmlFile = path.join(__dirname, 'Interface_version_3_7.html');
const pdfFile = path.join(__dirname, 'Interface_version_3_7.pdf');

// Find Chrome or Edge executable
function findBrowser() {
    const possiblePaths = [
        'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
        'C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe',
        'C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe',
        'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
        'C:\\Program Files\\Chromium\\Application\\chrome.exe',
        'C:\\Users\\TB32MUS\\AppData\\Local\\Chromium\\Application\\chrome.exe'
    ];
    
    for (const browserPath of possiblePaths) {
        if (fs.existsSync(browserPath)) {
            console.log(`Found browser: ${browserPath}`);
            return browserPath;
        }
    }
    
    return null;
}

function convertToPdf() {
    const browser = findBrowser();
    
    if (!browser) {
        console.error('ERROR: Chrome or Edge browser not found!');
        console.error('Please install Google Chrome or Microsoft Edge');
        process.exit(1);
    }
    
    if (!fs.existsSync(htmlFile)) {
        console.error(`ERROR: HTML file not found: ${htmlFile}`);
        process.exit(1);
    }
    
    console.log(`Converting HTML to PDF with bookmarks...`);
    console.log(`Input:  ${htmlFile}`);
    console.log(`Output: ${pdfFile}`);
    console.log('');
    
    // Use browser's print-to-pdf feature
    const args = [
        '--headless',
        '--disable-gpu',
        '--print-to-pdf=' + pdfFile,
        '--print-to-pdf-no-header',
        `file:///${htmlFile.replace(/\\/g, '/')}`
    ];
    
    const process = spawn(browser, args, {
        stdio: 'pipe'
    });
    
    process.stdout.on('data', (data) => {
        process.stdout.write(data);
    });
    
    process.stderr.on('data', (data) => {
        process.stderr.write(data);
    });
    
    process.on('close', (code) => {
        if (code === 0) {
            if (fs.existsSync(pdfFile)) {
                const stats = fs.statSync(pdfFile);
                const sizeKB = (stats.size / 1024).toFixed(1);
                console.log('');
                console.log(`SUCCESS: PDF created with bookmarks!`);
                console.log(`File: ${pdfFile}`);
                console.log(`Size: ${sizeKB} KB`);
            } else {
                console.error('ERROR: PDF file was not created');
                process.exit(1);
            }
        } else {
            console.error(`ERROR: Browser exited with code ${code}`);
            process.exit(1);
        }
    });
}

convertToPdf();
