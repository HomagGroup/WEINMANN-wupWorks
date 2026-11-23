#!/usr/bin/env dotnet
#:package Markdig@0.42.0
#:package PuppeteerSharp@20.2.4

// Builds Interface_version_<X_Y>.pdf from <X_Y>/Interface_version_<X_Y>.md.
// Usage: dotnet run build_pdf.cs -- <version folder, e.g. 3_8> [--keep-html]

using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using PuppeteerSharp;
using PuppeteerSharp.Media;

var scriptDir = Path.GetDirectoryName(Path.GetFullPath(AppContext.GetData("EntryPointFilePath") as string ?? "build_pdf.cs"))!;
var version = args.FirstOrDefault(a => !a.StartsWith("--"))?.Replace('.', '_').TrimEnd('\\', '/');
var keepHtml = args.Contains("--keep-html");

if (version is null)
{
    Console.Error.WriteLine("Usage: dotnet run build_pdf.cs -- <version folder, e.g. 3_8> [--keep-html]");
    return 1;
}

var versionDir = Path.Combine(scriptDir, version);
var mdFile = Path.Combine(versionDir, $"Interface_version_{version}.md");
var htmlFile = Path.Combine(versionDir, $"Interface_version_{version}.print.html");
var pdfFile = Path.Combine(scriptDir, $"Interface_version_{version}.pdf");

if (!File.Exists(mdFile))
{
    Console.Error.WriteLine($"Markdown file not found: {mdFile}");
    return 1;
}

Console.WriteLine($"Input:  {mdFile}");
Console.WriteLine($"Output: {pdfFile}");

var markdown = await File.ReadAllTextAsync(mdFile);
var title = Regex.Match(markdown, @"^#\s+(.+)$", RegexOptions.Multiline) is { Success: true } m
    ? m.Groups[1].Value.Trim()
    : $"Interface version {version.Replace('_', '.')}";

// The hand-maintained TOC (with static page numbers) is only meant for the online view.
const string TocPlaceholder = "<!--GENERATED-TOC-->";
var manualToc = new Regex(@"<h2>\s*Table of Contents\s*</h2>\s*<table class=""toc-table"">.*?</table>",
    RegexOptions.Singleline | RegexOptions.IgnoreCase);
markdown = manualToc.IsMatch(markdown)
    ? manualToc.Replace(markdown, TocPlaceholder, 1)
    : Regex.Replace(markdown, @"^(#\s+.+)$", $"$1\n\n{TocPlaceholder}\n", RegexOptions.Multiline);

var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
var body = Markdown.ToHtml(markdown, pipeline);
body = body.Replace(TocPlaceholder, BuildToc(body));

await File.WriteAllTextAsync(htmlFile, BuildDocument(title, body), Encoding.UTF8);

try
{
    await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
    {
        Headless = true,
        ExecutablePath = FindBrowser() ?? (await new BrowserFetcher().DownloadAsync()).GetExecutablePath(),
    });
    await using var page = await browser.NewPageAsync();
    await page.GoToAsync(new Uri(htmlFile).AbsoluteUri, WaitUntilNavigation.Networkidle0);

    await page.PdfAsync(pdfFile, new PdfOptions
    {
        Format = PaperFormat.A4,
        PrintBackground = true,
        Outline = true,
        Tagged = true,
        DisplayHeaderFooter = true,
        HeaderTemplate = $"""
            <div style="font-size:8pt; width:100%; margin:0 15mm; color:#666; display:flex; justify-content:space-between;">
              <span>WUP interface specification</span><span>{WebUtility.HtmlEncode(title)}</span>
            </div>
            """,
        FooterTemplate = """
            <div style="font-size:8pt; width:100%; margin:0 15mm; color:#666; text-align:right;">
              Page <span class="pageNumber"></span> of <span class="totalPages"></span>
            </div>
            """,
        MarginOptions = new MarginOptions { Top = "20mm", Bottom = "18mm", Left = "15mm", Right = "15mm" },
    });
}
finally
{
    if (!keepHtml)
    {
        File.Delete(htmlFile);
    }
}

Console.WriteLine($"PDF created ({new FileInfo(pdfFile).Length / 1024} KB)");
return 0;

static string BuildToc(string html)
{
    var headings = Regex.Matches(html, @"<h([2-4])\s+id=""([^""]+)""[^>]*>(.*?)</h\1>", RegexOptions.Singleline);
    var toc = new StringBuilder("<nav class=\"toc\">\n<h2 class=\"toc-title\">Table of Contents</h2>\n<ul>\n");
    foreach (Match h in headings)
    {
        var level = int.Parse(h.Groups[1].Value) - 2;
        var text = Regex.Replace(h.Groups[3].Value, "<[^>]+>", "").Trim();
        toc.Append($"<li class=\"toc-l{level}\"><a href=\"#{h.Groups[2].Value}\">{text}</a></li>\n");
    }
    return toc.Append("</ul>\n</nav>").ToString();
}

static string BuildDocument(string title, string body) => $$"""
    <!DOCTYPE html>
    <html lang="en">
    <head>
    <meta charset="utf-8">
    <title>{{WebUtility.HtmlEncode(title)}}</title>
    <style>
    @page { size: A4; }
    body { font-family: "Segoe UI", Arial, sans-serif; font-size: 10pt; line-height: 1.45; color: #222; margin: 0; }
    h1 { font-size: 24pt; margin: 0 0 12pt; }
    h2 { font-size: 16pt; break-before: page; border-bottom: 1px solid #ccc; padding-bottom: 3pt; }
    h3 { font-size: 13pt; }
    h4 { font-size: 11pt; }
    h2, h3, h4, h5 { break-after: avoid; color: #003a63; }
    hr { border: none; border-top: 1px solid #e3e3e3; margin: 12pt 0; break-after: avoid; }
    hr:has(+ h2) { display: none; }
    a { color: #005a9e; text-decoration: none; }
    img { max-width: 100%; height: auto; break-inside: avoid; }
    p:has(> img) { break-inside: avoid; }
    table { border-collapse: collapse; width: 100%; margin: 6pt 0 10pt; font-size: 9pt; }
    th, td { border: 1px solid #ccc; padding: 3pt 5pt; vertical-align: top; text-align: left; }
    th { background: #eef2f6; }
    thead { display: table-header-group; }
    tr { break-inside: avoid; }
    code { font-family: Consolas, "Courier New", monospace; font-size: 9pt; }
    pre { background: #f6f8fa; border: 1px solid #e1e4e8; padding: 6pt 8pt; white-space: pre-wrap; break-inside: avoid; }
    nav.toc { break-after: page; }
    nav.toc h2.toc-title { break-before: auto; }
    nav.toc ul { list-style: none; padding: 0; margin: 0; }
    nav.toc li { margin: 2pt 0; }
    nav.toc a { color: #222; }
    .toc-l0 { font-weight: 700; margin-top: 6pt !important; }
    .toc-l1 { padding-left: 16pt; }
    .toc-l2 { padding-left: 32pt; font-size: 9.5pt; }
    </style>
    </head>
    <body>
    {{body}}
    </body>
    </html>
    """;

static string? FindBrowser()
{
    string[] candidates =
    [
        @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
        @"C:\Program Files\Google\Chrome\Application\chrome.exe",
        @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
        "/usr/bin/google-chrome",
        "/usr/bin/chromium",
        "/usr/bin/microsoft-edge",
    ];
    return candidates.FirstOrDefault(File.Exists);
}
