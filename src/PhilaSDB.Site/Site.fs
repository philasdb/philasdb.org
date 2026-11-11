/// Every page of the site, rendered once at startup and kept in memory compressed, the way a static site
/// generator would write files. Requests are a dictionary lookup; nothing is rendered per request.
module PhilaSDB.Site.Site

open System
open System.Collections.Generic
open System.IO
open System.IO.Compression
open System.Security.Cryptography
open System.Text
open Oxpecker.ViewEngine
open PhilaSDB.Scripture
open PhilaSDB.Site.Assets
open PhilaSDB.Site.Bulletins
open PhilaSDB.Site.Views

type RenderedPage =
    { ContentType: string
      Identity: byte[]
      /// Compressed on first use: maximum compression is slow enough to add up to most of startup
      Brotli: Lazy<byte[]>
      Gzip: Lazy<byte[]>
      /// Quoted strong ETag of the uncompressed bytes
      ETag: string }

let private compress (bytes: byte[]) (wrap: Stream -> Stream) =
    use output = new MemoryStream()

    do
        use stream = wrap output
        stream.Write(bytes, 0, bytes.Length)

    output.ToArray()

let private rendered (contentType: string) (bytes: byte[]) =
    { ContentType = contentType
      Identity = bytes
      Brotli = lazy (compress bytes (fun s -> new BrotliStream(s, CompressionLevel.SmallestSize)))
      Gzip = lazy (compress bytes (fun s -> new GZipStream(s, CompressionLevel.SmallestSize)))
      ETag = "\"" + Convert.ToHexStringLower(SHA256.HashData bytes, 0, 12) + "\"" }

let private htmlPage (view: HtmlElement) =
    rendered "text/html; charset=utf-8" (Render.toHtmlDocBytes view)

let private textPage (contentType: string) (text: string) =
    rendered contentType (Encoding.UTF8.GetBytes text)

type Site =
    { Pages: IReadOnlyDictionary<string, RenderedPage>
      /// Text files from wwwroot (CSS, JavaScript, the favicon), compressed once like the pages instead of on
      /// every request at the fastest level
      Static: IReadOnlyDictionary<string, RenderedPage>
      NotFound: RenderedPage
      BulletinCount: int
      VerseCount: int }

let private sitemap (paths: string seq) =
    let sb = StringBuilder()
    sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""") |> ignore
    sb.AppendLine("""<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""") |> ignore

    for path in paths do
        sb.Append("<url><loc>").Append(Layout.canonicalUrl path).AppendLine("</loc></url>") |> ignore

    sb.Append("</urlset>").ToString()

let private sitemapIndex =
    $"""<?xml version="1.0" encoding="UTF-8"?>
<sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9"><sitemap><loc>{Layout.siteUrl}/sitemap-0.xml</loc></sitemap></sitemapindex>"""

/// Render the whole site from the content folder (bulletins, hymnals), the KJV and wwwroot
let build (contentRoot: string) (biblePath: string) (webRoot: string) =
    let assets = Fingerprints webRoot
    let bible = Bible.Load biblePath
    let hymnals = Hymnals.load (Path.Combine(contentRoot, "hymnals.json"))
    let bulletins = Bulletins.loadFolder (Path.Combine(contentRoot, "bulletins"))

    let ctx: BulletinViews.Context =
        { Bible = bible
          Hymnals = hymnals
          Assets = assets }

    let htmlPages =
        [ "/", Pages.home assets
          "/about", Pages.about assets
          "/giving", Pages.giving assets
          "/livestream", Pages.livestream assets
          "/sermons", Pages.sermons assets
          "/tyler-gahagan", Pages.tylerGahagan assets
          "/concert/2025", Pages.concert2025 assets
          "/concert/2025/menu", Pages.concert2025Menu assets
          "/concert/2026", Pages.concert2026 assets
          "/bulletin", BulletinViews.latest ctx (List.tryHead bulletins)
          "/bulletin/archive", BulletinViews.archive ctx bulletins
          for bulletin in bulletins do
              $"/bulletin/{bulletin.Slug}", BulletinViews.standalone ctx bulletin ]

    let pages = Dictionary<string, RenderedPage>(StringComparer.OrdinalIgnoreCase)

    for path, view in htmlPages do
        if pages.ContainsKey path then
            failwith $"Two pages at {path} (two bulletins with the same date?)"

        pages[path] <- htmlPage view


    pages["/sitemap-0.xml"] <- textPage "application/xml; charset=utf-8" (sitemap (List.map fst htmlPages))
    pages["/sitemap-index.xml"] <- textPage "application/xml; charset=utf-8" sitemapIndex

    pages["/robots.txt"] <-
        textPage "text/plain; charset=utf-8" $"User-agent: *\nAllow: /\n\nSitemap: {Layout.siteUrl}/sitemap-index.xml\n"

    let staticTypes =
        dict [ ".css", "text/css; charset=utf-8"; ".js", "text/javascript; charset=utf-8"; ".ico", "image/x-icon" ]

    let staticFiles = Dictionary<string, RenderedPage>(StringComparer.Ordinal)

    for file in Directory.EnumerateFiles(webRoot, "*", SearchOption.AllDirectories) do
        match staticTypes.TryGetValue(Path.GetExtension file) with
        | true, contentType ->
            let url = "/" + Path.GetRelativePath(webRoot, file).Replace('\\', '/')
            staticFiles[url] <- rendered contentType (File.ReadAllBytes file)
        | false, _ -> ()

    { Pages = pages
      Static = staticFiles
      NotFound = htmlPage (Pages.notFound assets)
      BulletinCount = bulletins.Length
      VerseCount = bible.VerseCount }

/// Write the rendered site out as static files (index.html per page, plus wwwroot), for any static host. The
/// GitHub Pages deploy uses this until philasdb.org points at the server.
let export (site: Site) (webRoot: string) (outDir: string) =
    if Directory.Exists outDir then
        Directory.Delete(outDir, true)

    for file in Directory.EnumerateFiles(webRoot, "*", SearchOption.AllDirectories) do
        let target = Path.Combine(outDir, Path.GetRelativePath(webRoot, file))
        Directory.CreateDirectory(Path.GetDirectoryName target) |> ignore
        File.Copy(file, target)

    let write (relative: string) (bytes: byte[]) =
        let target = Path.Combine(outDir, relative)
        Directory.CreateDirectory(Path.GetDirectoryName target) |> ignore
        File.WriteAllBytes(target, bytes)

    for KeyValue(path, page) in site.Pages do
        let relative = path.TrimStart '/'

        if relative = "" then write "index.html" page.Identity
        elif Path.HasExtension relative then write relative page.Identity
        else write (Path.Combine(relative, "index.html")) page.Identity

    write "404.html" site.NotFound.Identity
    write "CNAME" (Encoding.ASCII.GetBytes "philasdb.org")
    site.Pages.Count
