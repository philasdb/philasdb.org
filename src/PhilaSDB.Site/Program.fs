module PhilaSDB.Site.Program

open System
open System.Diagnostics
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.ResponseCompression
open Microsoft.AspNetCore.StaticFiles
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Net.Http.Headers
open PhilaSDB.Site.Site

/// Pick the smallest encoding the browser accepts (every current browser sends br and gzip)
let private chooseEncoding (acceptEncoding: string) (page: RenderedPage) =
    let accepts (name: string) =
        acceptEncoding.Split(',')
        |> Array.exists (fun part ->
            let token = (part.Split ';').[0].Trim()
            token.Equals(name, StringComparison.OrdinalIgnoreCase))

    if accepts "br" then struct ("br", page.Brotli.Value)
    elif accepts "gzip" then struct ("gzip", page.Gzip.Value)
    else struct (null, page.Identity)

let private writePage (ctx: HttpContext) (status: int) (page: RenderedPage) : Task =
    let headers = ctx.Response.Headers
    ctx.Response.StatusCode <- status
    ctx.Response.ContentType <- page.ContentType
    headers.Vary <- "Accept-Encoding"
    headers.ETag <- page.ETag
    // Revalidate every time: a 304 is a few bytes, and a new bulletin shows up as soon as it's deployed
    headers.CacheControl <- "no-cache"

    let ifNoneMatch = ctx.Request.Headers.IfNoneMatch.ToString()

    if status = StatusCodes.Status200OK && ifNoneMatch.Contains page.ETag then
        ctx.Response.StatusCode <- StatusCodes.Status304NotModified
        Task.CompletedTask
    else
        let struct (encoding, body) = chooseEncoding (ctx.Request.Headers.AcceptEncoding.ToString()) page

        if not (isNull encoding) then
            headers.ContentEncoding <- encoding

        ctx.Response.ContentLength <- body.LongLength

        if HttpMethods.IsHead ctx.Request.Method then
            Task.CompletedTask
        else
            ctx.Response.Body.WriteAsync(body, 0, body.Length)

/// "/about/" and "/about" are the same page, as they were on GitHub Pages
let private normalize (path: string) =
    if path.Length > 1 && path.EndsWith '/' then path.TrimEnd '/' else path

/// Serve the site: static files from wwwroot, every page from memory
let private serve (app: WebApplication) (site: Site) =
    app.Use(fun (ctx: HttpContext) (next: RequestDelegate) ->
        let headers = ctx.Response.Headers
        headers.XContentTypeOptions <- "nosniff"
        headers["Referrer-Policy"] <- "strict-origin-when-cross-origin"
        next.Invoke ctx)
    |> ignore

    app.UseResponseCompression() |> ignore

    app.UseStaticFiles(
        StaticFileOptions(
            OnPrepareResponse =
                fun ctx ->
                    // Pages link to static files with ?v=<content hash>, so those URLs never change content
                    ctx.Context.Response.Headers.CacheControl <-
                        if ctx.Context.Request.Query.ContainsKey "v" then
                            "public, max-age=31536000, immutable"
                        else
                            "public, max-age=3600"
        )
    )
    |> ignore

    app.MapDefaultEndpoints() |> ignore
    // Fly's health check. RequestDelegate rather than a lambda handler: F# has no request delegate generator,
    // and minimal API parameter binding by reflection isn't AOT safe
    app.MapGet("/healthz", RequestDelegate(fun ctx -> ctx.Response.WriteAsync "Healthy")) |> ignore

    app.Run(fun ctx ->
        if HttpMethods.IsGet ctx.Request.Method || HttpMethods.IsHead ctx.Request.Method then
            match site.Pages.TryGetValue(normalize ctx.Request.Path.Value) with
            | true, page -> writePage ctx StatusCodes.Status200OK page
            | false, _ -> writePage ctx StatusCodes.Status404NotFound site.NotFound
        else
            ctx.Response.StatusCode <- StatusCodes.Status405MethodNotAllowed
            ctx.Response.Headers.Allow <- "GET, HEAD"
            Task.CompletedTask)

    app.Run()

[<EntryPoint>]
let main args =
    // "export <dir>" writes the site out as static files instead of serving it
    let builder = WebApplication.CreateSlimBuilder(args)
    builder.AddServiceDefaults() |> ignore

    builder.Services.AddResponseCompression(fun options ->
        // Fly.io terminates TLS, so Kestrel sees plain HTTP in production; this keeps local HTTPS runs the same
        options.EnableForHttps <- true
        options.Providers.Add<BrotliCompressionProvider>()
        options.Providers.Add<GzipCompressionProvider>())
    |> ignore

    let app = builder.Build()
    let log = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger "PhilaSDB.Site"

    let timer = Stopwatch.StartNew()
    let baseDir = AppContext.BaseDirectory

    let site =
        Site.build
            (Path.Combine(baseDir, "content"))
            (Path.Combine(baseDir, "data", "eng-kjv.osis.xml"))
            app.Environment.WebRootPath

    log.LogInformation(
        "Rendered {Pages} pages ({Bulletins} bulletins, {Verses} KJV verses) in {Elapsed} ms",
        site.Pages.Count,
        site.BulletinCount,
        site.VerseCount,
        timer.ElapsedMilliseconds
    )

    match args with
    | [| "export"; outDir |] ->
        let count = Site.export site app.Environment.WebRootPath outDir
        log.LogInformation("Exported {Pages} pages and the static files to {OutDir}", count, Path.GetFullPath outDir)
    | _ -> serve app site

    0
