# Philadelphia Seventh Day Baptist Church Website

The source code for [https://philasdb.org](https://philasdb.org).

The site is an F# web app on .NET 11, published as a Native AOT executable. There's no Node.js, npm or
JavaScript build step: pages are server-rendered with [Oxpecker.ViewEngine](https://github.com/Lanayx/Oxpecker),
and the few scripts in `wwwroot/js` are plain JavaScript modules served as they are.

## Prerequisites

- [.NET SDK 11.0](https://dotnet.microsoft.com/download/dotnet/11.0) (pinned in `global.json`)
- For a Native AOT publish outside Docker: `clang` and `zlib1g-dev` on Linux (see
  [Native AOT prerequisites](https://learn.microsoft.com/dotnet/core/deploying/native-aot/#prerequisites))
- [Docker](https://www.docker.com/get-started) (optional)

## Commands

All commands run from the repo root.

| Command | Action |
| :-- | :-- |
| `dotnet run --project src/PhilaSDB.Site` | Starts the site at `http://localhost:5080` |
| `dotnet run apphost.cs` | Starts the site with the [Aspire](https://aspire.dev) dashboard (logs, traces, metrics) |
| `dotnet test` | Runs the tests |
| `dotnet run --project src/PhilaSDB.Site -- export dist` | Writes the whole site as static files to `dist/` |
| `dotnet publish src/PhilaSDB.Site -r linux-x64 -c Release` | Native AOT build |
| `docker build -f src/PhilaSDB.Site/Dockerfile -t philasdb-site .` | Docker image (build from the repo root) |

## Adding a bulletin

Same as before: add `src/PhilaSDB.Site/content/bulletins/YYYY-MM-DD.json` (see `content/example-v2-bulletin.json`)
and push to `main`. Version 1 files (the older fixed format) are still read and migrated to version 2.

Scripture references (`"Psalm 27:1-14"`, `"John 3:16; Romans 8:28"`) are looked up in the KJV for the "Thought of the
Day" and any scripture item with `"showPassage": true`. Hymns given as a number come from `content/hymnals.json`
(Hymns of Faith); `{ "hymnalCode": "TH41952", "hymnNumber": 81 }` picks a hymn from another hymnal.

If a bulletin can't be read (a bad date, an unknown item type), the site doesn't start, and the error names the
file. `dotnet test` checks every bulletin, so CI catches it before it's deployed.

## How it works

At startup the app reads the bulletins, hymnals and KJV text and renders every page once (about 150 ms with Native
AOT). Requests are a dictionary lookup: pages are served from memory with an `ETag`, compressed with Brotli or gzip
on first use. CSS and JavaScript are served the same way, linked with a content hash (`/css/pico.min.css?v=…`)
and cached for a year; the small site stylesheets are inlined into the pages. Pico CSS and hls.js are vendored
(see `src/PhilaSDB.Site/VENDORED.md`).

Photos are shown with `<picture>` in AVIF and WebP at several widths. After adding or replacing a photo, run
`python3 scripts/images.py` (needs [Pillow](https://pypi.org/project/pillow/)) and commit the files it makes; the
widths each photo needs are listed at the top of the script. [docs/lighthouse.md](docs/lighthouse.md) compares the
site with the Astro version in Lighthouse.

| File | Role |
| :-- | :-- |
| `Bulletins.fs` | The bulletin JSON format (v1 and v2), reading it, and the v1 → v2 migration |
| `Hymnals.fs` | Hymn lookups by hymnal and number |
| `Assets.fs` | Content-hash URLs for files in `wwwroot` |
| `Views/Layout.fs` | Head, header, footer: the shared page layout |
| `Views/Pages.fs` | Home, about, giving, livestream, sermons, concerts and the other fixed pages |
| `Views/BulletinViews.fs` | The latest bulletin, the printable bulletin page and the archive |
| `Site.fs` | Renders every page at startup; the static export |
| `Program.fs` | HTTP: pages, static files, compression, `/healthz` |

The livestream indicator and the livestream page still poll the
[livestream server](https://github.com/philasdb/livestream) from the browser, and the sermons page still calls
the YouTube API from the browser. Moving these to the server (server-sent events with Datastar, and a server-side,
cached video list) is planned; see [docs/stack-migration.md](docs/stack-migration.md).

## Repository layout

The layout is meant to grow into a monorepo for all the philasdb apps (this site,
[bible](https://github.com/philasdb/bible) and [livestream](https://github.com/philasdb/livestream)):

```
global.json                 .NET SDK version for every project
Directory.Build.props       Shared build settings (net11.0, warnings as errors, invariant globalization)
Directory.Packages.props    One version of each NuGet package for the whole repo
philasdb.slnx               Solution with every project
apphost.cs                  Aspire AppHost: runs all the apps together locally
data/                       Shared data (the KJV OSIS text)
scripts/images.py           Makes the AVIF/WebP sizes of the site's photos
src/
  PhilaSDB.Scripture/       Shared library: KJV text and scripture references
  PhilaSDB.ServiceDefaults/ Shared Aspire service defaults (OpenTelemetry, health checks)
  PhilaSDB.Site/            The website, with its own Dockerfile and fly.toml
tests/
  PhilaSDB.Site.Tests/
```

Each app keeps its `Dockerfile` and `fly.toml` in its own folder and builds from the repo root, so the bible and
livestream apps can move in under `src/` (and their tests under `tests/`) without changing how anything else
builds. They'd then share `PhilaSDB.ServiceDefaults`, `PhilaSDB.Scripture` and `data/eng-kjv.osis.xml` instead of
keeping copies.

## Deployment

CI (`.github/workflows/site.yml`) builds and tests every push, builds the Native AOT Docker image, fails on new
trim/AOT warnings and smoke-tests the container. On `main` it:

- **Deploys to GitHub Pages**, using `export` to write the site as static files. This keeps philasdb.org updating
  while it's still hosted on GitHub Pages.
- **Deploys to Fly.io** (`src/PhilaSDB.Site/fly.toml`, app `philasdb-site`) once the `FLY_API_TOKEN` repository
  secret is set.

To move philasdb.org to Fly: create the app (`fly apps create philasdb-site`), add `FLY_API_TOKEN`, check
`https://philasdb-site.fly.dev`, then `fly certs add philasdb.org` and point DNS at Fly. The Pages job can be
removed after that.
