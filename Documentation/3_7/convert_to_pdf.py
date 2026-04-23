#!/usr/bin/env python3
import subprocess
import os
from pathlib import Path

def convert_markdown_to_html(md_file, output_html):
    """Convert Markdown to standalone HTML with TOC"""
    cmd = [
        'pandoc',
        str(md_file),
        '--standalone',
        '--self-contained',
        '--table-of-contents',
        '--toc-depth=3',
        '--from=markdown',
        '--to=html',
        '-o', str(output_html)
    ]
    
    print(f"Converting markdown to HTML with TOC...")
    result = subprocess.run(cmd, capture_output=True, text=True, timeout=60)
    
    if result.returncode != 0:
        print(f"Warning: {result.stderr[:500]}")
        return False
    
    if os.path.exists(output_html):
        file_size = os.path.getsize(output_html) / 1024
        print(f"HTML file created: {output_html}")
        print(f"File size: {file_size:.1f} KB")
        return True
    
    return False

def try_libreoffice_convert(html_file, output_pdf):
    """Try to convert HTML to PDF using LibreOffice"""
    try:
        cmd = [
            'soffice',
            '--headless',
            '--convert-to', 'pdf',
            '--outdir', os.path.dirname(output_pdf),
            str(html_file)
        ]
        result = subprocess.run(cmd, capture_output=True, text=True, timeout=120)
        
        if result.returncode == 0:
            expected_pdf = str(html_file).replace('.html', '.pdf')
            if os.path.exists(expected_pdf):
                os.rename(expected_pdf, output_pdf)
                file_size = os.path.getsize(output_pdf) / 1024
                print(f"PDF created with LibreOffice: {output_pdf}")
                print(f"File size: {file_size:.1f} KB")
                print(f"Bookmarks included in PDF!")
                return True
    except Exception as e:
        print(f"LibreOffice attempt failed: {str(e)[:100]}")
    
    return False

def main():
    script_dir = Path(__file__).parent
    md_file = script_dir / "Interface_version_3_7.md"
    output_html = script_dir / "Interface_version_3_7.html"
    output_pdf = script_dir / "Interface_version_3_7.pdf"
    
    if not md_file.exists():
        print(f"Error: Markdown file not found: {md_file}")
        return False
    
    print(f"Reading: {md_file}")
    
    # Step 1: Convert to HTML
    if not convert_markdown_to_html(md_file, output_html):
        print("Error converting to HTML")
        return False
    
    # Step 2: Try to convert HTML to PDF
    print("\nAttempting to convert HTML to PDF...")
    pdf_success = try_libreoffice_convert(output_html, output_pdf)
    
    if pdf_success:
        print("\nSuccess! PDF with bookmarks created.")
        return True
    else:
        print(f"\nHTML file created successfully: {output_html}")
        print("\nTo create PDF with bookmarks:")
        print("1. Open the HTML file in a web browser")
        print("2. Press Ctrl+P to print")
        print("3. Select 'Save as PDF'")
        print("4. Save as: Interface_version_3_7.pdf")
        print("\nThe table of contents will be converted to bookmarks in the PDF.")
        return True

if __name__ == "__main__":
    success = main()
    exit(0 if success else 1)
