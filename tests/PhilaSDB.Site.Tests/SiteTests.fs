module PhilaSDB.Site.Tests.SiteTests

open System.IO
open System.Text
open Xunit
open PhilaSDB.Site

let private site = lazy (Site.build Paths.content Paths.kjv Paths.webRoot)

let private html (path: string) =
    match site.Value.Pages.TryGetValue path with
    | true, page -> Encoding.UTF8.GetString page.Identity
    | false, _ -> failwith $"No page at {path}"

[<Theory>]
[<InlineData "/">]
[<InlineData "/about">]
[<InlineData "/giving">]
[<InlineData "/livestream">]
[<InlineData "/sermons">]
[<InlineData "/tyler-gahagan">]
[<InlineData "/concert/2025">]
[<InlineData "/concert/2025/menu">]
[<InlineData "/concert/2026">]
[<InlineData "/bulletin">]
[<InlineData "/bulletin/archive">]
let ``every page from the old site is there`` (path: string) =
    Assert.StartsWith("<!DOCTYPE html>", html path, System.StringComparison.OrdinalIgnoreCase)

[<Fact>]
let ``every bulletin has a page and the archive links to it`` () =
    let bulletins = Bulletins.loadFolder (Path.Combine(Paths.content, "bulletins"))
    let archive = html "/bulletin/archive"

    for bulletin in bulletins do
        Assert.Contains("Bulletin | ", html $"/bulletin/{bulletin.Slug}")
        Assert.Contains($"href=\"/bulletin/{bulletin.Slug}\"", archive)

[<Fact>]
let ``the bulletin page shows the latest bulletin`` () =
    let latest = Bulletins.loadFolder (Path.Combine(Paths.content, "bulletins")) |> List.head
    Assert.Contains($"<title>{Views.BulletinViews.longDate latest.Date} | ", html "/bulletin")

[<Fact>]
let ``bulletins show scripture text and hymns`` () =
    let page = html "/bulletin/2026-11-07"
    // Thought of the Day: Psalm 27:5, with its text
    Assert.Contains("<sup>5</sup>For in the time of trouble he shall hide me in his pavilion", page)
    // Opening Song: hymn 217 in the default hymnal
    Assert.Contains("Hymns of Faith #217", page)
    Assert.Contains("https://philasdb-bible.fly.dev/passage?search=Psalm%2027%3A5", page)

[<Fact>]
let ``songs given by hymnal code and number are looked up`` () =
    Assert.Contains("Tabernacle Hymns: Number Four #81", html "/bulletin/2025-09-13")

[<Fact>]
let ``static files are linked with a content hash`` () =
    Assert.Matches("src=\"/js/livestream-link.js\\?v=[0-9a-f]{12}\"", html "/")

[<Fact>]
let ``small stylesheets are inlined instead of blocking rendering`` () =
    Assert.DoesNotContain("/css/global.css", html "/")
    Assert.Contains(".navbar-full", html "/")
    Assert.DoesNotContain("/css/bulletin.css", html "/bulletin/2026-11-07")
    Assert.Contains(".left-column", html "/bulletin/2026-11-07")

[<Fact>]
let ``photos come in AVIF and WebP sizes`` () =
    let home = html "/"
    Assert.Matches("<source type=\"image/avif\" srcset=\"/images/sized/cover_image-480.avif\\?v=[0-9a-f]{12} 480w, ", home)
    Assert.Contains("sizes=\"(max-width: 925px) 100vw, 925px\"", home)
    // The original stays as the fallback and keeps its dimensions, so nothing shifts while it loads
    Assert.Matches("<img src=\"/images/cover_image.webp\\?v=[0-9a-f]{12}\" alt=\"Philadelphia Seventh Day Baptist Church\" width=\"925\" height=\"520\"", home)

[<Fact>]
let ``the sitemap lists the pages`` () =
    let sitemap = html "/sitemap-0.xml"
    Assert.Contains("<loc>https://philasdb.org/</loc>", sitemap)
    Assert.Contains("<loc>https://philasdb.org/bulletin/2026-11-07</loc>", sitemap)

[<Fact>]
let ``export writes a static copy of the site`` () =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    try
        Site.export site.Value Paths.webRoot outDir |> ignore
        Assert.True(File.Exists(Path.Combine(outDir, "index.html")))
        Assert.True(File.Exists(Path.Combine(outDir, "bulletin", "2026-11-07", "index.html")))
        Assert.True(File.Exists(Path.Combine(outDir, "css", "global.css")))
        Assert.True(File.Exists(Path.Combine(outDir, "404.html")))
    finally
        if Directory.Exists outDir then
            Directory.Delete(outDir, true)

[<Fact>]
let ``CSS and JavaScript are served compressed at the highest level`` () =
    let pico = site.Value.Static["/css/pico.min.css"]
    Assert.Equal("text/css; charset=utf-8", pico.ContentType)
    // 80 KB of CSS; the fastest Brotli level gave 18.5 KB
    Assert.True(pico.Brotli.Value.Length < 12_500, $"pico.min.css is {pico.Brotli.Value.Length} bytes with Brotli")
    Assert.True(site.Value.Static.ContainsKey "/js/livestream-link.js")
