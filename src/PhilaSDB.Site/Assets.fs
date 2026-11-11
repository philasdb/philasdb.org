/// Static files in wwwroot, referenced from pages with a content hash (?v=…) so browsers can cache them for
/// a year and still get a new version as soon as the file changes.
module PhilaSDB.Site.Assets

open System
open System.Collections.Generic
open System.IO
open System.Security.Cryptography

type Fingerprints(webRoot: string) =
    let hashes = Dictionary<string, string>(StringComparer.Ordinal)

    do
        if Directory.Exists webRoot then
            for path in Directory.EnumerateFiles(webRoot, "*", SearchOption.AllDirectories) do
                let url = "/" + Path.GetRelativePath(webRoot, path).Replace('\\', '/')
                use stream = File.OpenRead path
                let hash = SHA256.HashData stream
                hashes[url] <- Convert.ToHexStringLower(hash, 0, 6)

    /// "/css/global.css" → "/css/global.css?v=1a2b3c4d5e6f". Fails for a file that doesn't exist, so a
    /// broken link stops startup instead of shipping.
    member _.Url(path: string) =
        match hashes.TryGetValue path with
        | true, hash -> $"{path}?v={hash}"
        | false, _ -> failwith $"No static file {path} in {webRoot}"

    /// A text file's contents, for small stylesheets that are inlined into pages instead of costing a
    /// render-blocking request
    member _.Text(path: string) =
        if not (hashes.ContainsKey path) then
            failwith $"No static file {path} in {webRoot}"

        File.ReadAllText(Path.Combine(webRoot, path.TrimStart '/'))
