# Migrating philasdb.org from Astro/Node to F# on .NET (NativeAOT + Datastar)

## Status

- **Done (this branch):** the site is ported to F# on .NET 11 with Native AOT. Every page and bulletin renders as
  it did in Astro (checked text-for-text and visually against the Astro build). Node, pnpm, Astro, Lit and
  video.js are gone. The repo is laid out for a monorepo, with an Aspire `apphost.cs`, a Dockerfile and fly.toml,
  and CI that deploys to GitHub Pages (static export) and to Fly once `FLY_API_TOKEN` is set.
- **Not yet:** live status over SSE/Datastar and the livestream webhook (step 4), the server-side YouTube list
  (step 1), and bulletin storage and admin (step 5). The livestream indicator and sermons page still work as before,
  from the browser.

## Goals

1. No Node.js, npm/pnpm, Vite or Astro anywhere in the build or runtime.
2. The livestream indicator and the livestream page update over SSE (Datastar) with no client polling.
3. The sermons video list is assembled on the server. No API key reaches the browser.
4. Run on Fly.io next to `philasdb-bible` and `psdb-livestream`, keeping the whole org's bill under the $5 waiver.
5. Stay as fast as the current GitHub Pages site.
6. Keep the door open to one `philasdb` monorepo later.
7. Publish bulletins without committing to git or redeploying, with an admin upload and later an editor.

## What the site is today

| Area | How it works now | Notes |
|---|---|---|
| Framework | Astro 7 static build, deployed to GitHub Pages | About 2,500 lines across 30 `.astro`/`.ts` files |
| Bulletins | ~90 JSON files in `src/data/bulletins`, rendered at build time | Scripture text comes from a 10 MB KJV OSIS file parsed with `@xmldom/xmldom`; hymns come from `hymnals.json` |
| Livestream indicator | `LivestreamLink.ts` custom element in the header, on **every page** | Calls `psdb-livestream.fly.dev/api/status` **every 10 s** |
| Livestream page | Inline script plus video.js | Calls the same status endpoint **every 5 s** and loads the HLS stream |
| Sermons | `SermonsPage.ts`, a Lit component | Calls the YouTube Data API **from the browser with the API key in the bundle**, two `search.list` calls per page view |
| Images | `astro:assets` `<Picture>` (sharp) | ~10 images, mostly webp already |
| Styling | Pico CSS from jsDelivr, two local CSS files | |
| Tooling | pnpm, vite-plus (oxlint/fmt), a TypeScript Aspire apphost | Also an empty, stray `src/philasdb.media-server` folder |

## Findings

### 1. The YouTube key is public, and the quota allows only ~50 sermon page views a day

- `AIzaSy…` is hard-coded in `SermonsPage.ts`, so it ships in the public JS bundle and sits in git history. Anyone can spend its quota.
- `search.list` costs **100 quota units**, and the page makes two calls per view. That's 200 units per view against the default **10,000 units/day**, so the sermons page goes blank after about **50 views a day**, or sooner if someone abuses the key.
- On the server, use `playlistItems.list` on the channel's uploads playlist (`UU` + the channel ID without its `UC` prefix) at **1 unit** per 50 videos. Finished livestreams are in the uploads playlist too, so one call replaces both searches. Cache the result in memory for about 30–60 minutes. That costs roughly 24–48 units a day **no matter how much traffic the site gets**.
- Whatever else you decide: once the server version is live, **rotate the current key** and restrict the new one in Google Cloud to the YouTube Data API only.

### 2. The header poll keeps the livestream machine awake

`psdb-livestream` is set to `auto_stop_machines = 'stop'` with `min_machines_running = 0`, but every open tab of *any* page calls it every 10 s. Each call goes through Fly's proxy and counts as traffic, so the RTMP server stays running and billed whenever someone is browsing the site, even when nothing is streaming. A browser-side SSE connection straight to the livestream app would have the same problem, since it's an open connection.

**Fix:** browsers should never talk to the livestream app except to fetch HLS segments while a stream is live. The site server owns "is it live?" and tells the browsers (see the architecture below).

### 3. The bible repo has already proven this stack

`philasdb/bible` already runs **F# + Oxpecker + Oxpecker.ViewEngine + StarFederation.Datastar 1.4 + NativeAOT** on Fly. It uses a chiseled `runtime-deps` image, a vendored `datastar.js` (v1.0.4, so no CDN or npm), SSE with a 15 s heartbeat, response compression on `text/event-stream`, and a CI pipeline that deploys with `flyctl deploy --image`. `psdb-livestream` is the same kind of F# NativeAOT app. The website can copy these patterns directly, so it isn't a new risk.

`PhilaSDB.Bible.Data` (`OsisParser`, `Domain`) and `ReferenceParser.fs` already parse KJV references like `Psalm 27:1-14` and `1 Samuel 4:2-7:2`. That replaces the site's `bible-reader.ts` and its copy of `eng-kjv.osis.xml`. In a monorepo they become a shared project reference. Until then, copy the two files or reference the project with a git submodule.

### 4. Datastar fits SSE and Fly's auto-stop well

- By default, Datastar **closes the SSE connection when the tab is hidden and reopens it when the tab is visible again**. Background tabs don't hold connections open, so the site machine can still suspend overnight.
- One Datastar SSE stream can patch the header indicator **and** swap the offline overlay for the player on `/livestream`. Two polling loops become one push.
- Fly's proxy closes idle connections, so send a heartbeat (comment line or no-op patch) every 15–25 s, as the bible app does.

### 5. Most of the site should stay pre-rendered

About 95% of what's served (home, about, giving, concert pages, every bulletin) only changes when a commit lands. Rendering these on each request would add work and nothing else. In the new design they're still rendered **once**, from F# views, at build time or at startup. They're served as precompressed bytes with an `ETag` and long cache headers on fingerprinted assets. Only three things are dynamic: live status (SSE), the sermons list (server-cached), and the livestream player swap.

### 6. Performance compared to GitHub Pages

| | GitHub Pages (today) | Fly, single `ewr` machine |
|---|---|---|
| Static HTML | CDN edge, ~20–50 ms | Secaucus, NJ, ~10–20 ms from Philadelphia and slower farther away. Fine for a local congregation |
| Cold start | None | A **suspended** machine resumes in a few hundred ms. A **stopped** one boots in ~1 s; NativeAOT itself starts in tens of ms |
| JS shipped | Lit + Temporal polyfill + video.js (~several hundred KB on the livestream page) | `datastar.js` ~33 KB. hls.js only on `/livestream` while live (Safari plays HLS natively) |
| Sermons page | Blank until two cross-origin API calls finish | Rendered on the server from cache, in the first byte |

The first visitor after an idle period pays the resume latency. Use `auto_stop_machines = "suspend"` (as `philasdb-bible` does), not `stop`, to keep that small. If that's ever a problem, put Cloudflare's free proxy in front for edge-cached static pages. It passes SSE through as long as heartbeats are sent.

### 7. Cost (verify on fly.io/pricing, figures moved in 2026)

- Fly's old free allowance (3 × shared-cpu-1x 256 MB) is **legacy, for grandfathered orgs only**. New usage is billed from the first second. If your org is grandfathered, the site machine fits in the allowance.
- A shared-cpu-1x 256 MB machine running all month costs about **$1.94–$2.19**. Suspended, it's billed only for its rootfs and snapshot storage, which is cents. A church site that's idle most of the week should land **under $1/month**. NativeAOT keeps it comfortably within 256 MB, probably about 30–60 MB RSS.
- **This org's invoices under $5 are still waived** (confirmed by the owner). The waiver applies to the **whole invoice**, so the real budget is: `philasdb-bible` + `psdb-livestream` + `philasdb-site` + volumes + IPs together < $5/month. That leaves plenty of room as long as each app suspends or stops when idle. The main thing to watch is `philasdb-bible`'s 1 GB/2 CPU machine: running nonstop it would use most of the $5 by itself.
- A 1 GB volume for bulletins (see "Bulletins without a deploy") adds about $0.15/month.
- **Free fallback:** if the waiver ever ends, generate the static pages with the same F# views to GitHub Pages and put only the dynamic endpoints on Fly (option B below).

## Architecture options

### A. New Fly app `philasdb-site` (recommended)

```
browser ──HTML/CSS/img──▶ philasdb-site (F#, Oxpecker, NativeAOT, ewr, suspend)
   │  ◀──SSE (Datastar)───┘   ▲            │
   │                          │ POST /internal/live (shared secret, Fly private network)
   └──HLS (only while live)──▶ psdb-livestream ──┘
                                            philasdb-site ──(cached, 1 unit/hr)──▶ YouTube Data API
```

- The livestream server sends **one** notification when its state changes (live → offline and back): a POST to `http://philasdb-site.flycast/internal/live` with a shared secret from a Fly secret. Flycast wakes the site if it's suspended. The site keeps the state in memory and pushes it to every connected browser.
- When the site boots, it asks `psdb-livestream.internal` for status. `.internal` addresses **don't** auto-start machines, so if nothing answers, the stream is offline and the RTMP server stays asleep.
- One origin means no CORS for pages or SSE. Only the HLS endpoint keeps CORS for `philasdb.org`. The YouTube key lives in `fly secrets`.

### B. Static site on GitHub Pages + dynamic endpoints on Fly ($0 option)

The same F# views run as a build-time generator that writes `dist/` for GitHub Pages. The header and sermons load their dynamic pieces with Datastar `@get` from a small Fly app over CORS. You get a CDN and no cold starts, but two deploy targets and cross-origin SSE. Choose this if Fly's bill matters more than simplicity.

### C. Fold the website into the livestream process (not recommended)

It's tempting, because live status would be in-process and you'd run one machine. But **pushing a bulletin on Saturday morning would redeploy the RTMP server in the middle of the service**, and website traffic would keep the ingest server running all week. Keep them as separate apps that can share code in a monorepo.

## Proposed design for option A

**Project layout** (mirrors `bible`, so it can move into a monorepo unchanged):

```
PhilaSDB.Site/            F# web app (Oxpecker, PublishAot, InvariantGlobalization)
  Domain.fs               Bulletin v1/v2 types + migration (port of types.ts / bulletinMigration.ts)
  Scripture.fs            Passage lookup (PhilaSDB.Bible.Data / ReferenceParser)
  Views/*.fs              One module per page/component (port of each .astro)
  Pages.fs                Renders static pages at startup → in-memory precompressed cache
  LiveStatus.fs           State + broadcast (Channel per subscriber), /internal/live webhook
  Sermons.fs              YouTube playlistItems client, IMemoryCache, stale-while-revalidate
  Program.fs              Routes, compression, static assets, health check
  wwwroot/                datastar.js (vendored), pico.min.css (vendored), hls.light.min.js, images, CSS
  data/bulletins/*.json   Moved as-is; same editing workflow
PhilaSDB.Site.Tests/      Bulletin parsing, reference rendering, page snapshot tests
apphost.cs                File-based Aspire apphost (as in bible), replaces apphost.mts
Dockerfile, fly.toml      Copied from bible / livestream
```

**Key decisions**

- **JSON** with `System.Text.Json` through a source-generated `JsonSerializerContext` (AOT-safe), or `JsonDocument` like the bible app. Bulletins are read once at startup.
- **Bulletins** move out of the image and into storage the server can write to, so publishing one doesn't need a commit or a deploy. See the next section.
- **Images:** they're already sized and mostly webp, so serve them as static files with `<img width height loading="lazy">`. `MapStaticAssets` adds fingerprinting, immutable caching and build-time Brotli/gzip with no Node involved. Convert `CS26-Slide.jpg` (450 KB) to webp once by hand.
- **Livestream page:** Datastar patches the offline overlay or the `<video>` element. The player is native `<video>` on Safari and iOS, with a vendored `hls.light.min.js` elsewhere. That drops video.js and its CSS.
- **Sitemap** (`/sitemap.xml`) and canonical/OG tags are generated from the same route table.
- **Analytics:** the gtag snippet stays as-is (no npm involved).
- **Tooling:** Fantomas (`dotnet tool`) replaces vite-plus/oxlint. Dependabot moves from npm to `nuget` and `github-actions`. Delete `package.json`, `pnpm-*`, `.node-version`, `vite.config.ts`, `.vite-hooks`, `apphost.mts` and `src/philasdb.media-server`.

**Changes in `psdb-livestream`** (small): add a state-change callback in `Streams.fs` that POSTs to `SITE_WEBHOOK_URL` with `SITE_WEBHOOK_SECRET`, drop `/api/status` CORS once the site no longer calls it from browsers, and keep HLS CORS.

## Bulletins without a deploy

Today, publishing a bulletin means committing a JSON file and waiting for a site rebuild. With a server, bulletins can be content the app owns, not source code.

### Storage: SQLite on a Fly volume (recommended)

- One table: `bulletins (date TEXT PRIMARY KEY, json TEXT, updated_at, updated_by)`, plus a `bulletin_revisions` table that keeps every saved version, so a bad edit can be undone. This is the same pattern as `remote.db` in the bible app (`Microsoft.Data.Sqlite`, which is AOT-safe and already proven there).
- **Seeding:** on first boot, if the table is empty, import the ~90 existing JSON files that ship in the image. After that the database is the source of truth.
- **Rendering:** bulletins are still pre-rendered. A save validates the JSON, renders the HTML once, and replaces the cached page (and `/bulletin`, the archive and the sitemap if needed). Readers never wait on rendering. Optionally, an SSE patch can update a bulletin page that's open on someone's phone during the service.
- **Backups:** Fly takes daily volume snapshots (kept 5 days by default, configurable). On top of that, add a `GET /admin/bulletins/export` (zip of JSON) and, optionally, a nightly job that commits changed bulletins back to this repo through the GitHub API. Then git stays a complete history and backup without being in the publishing path.
- **Why not the alternatives:** Tigris/S3 object storage works but adds a client library and another account for ~100 KB of data. Making the admin editor commit to GitHub and redeploy keeps the 2–3 minute delay you're trying to remove.
- **Caveat:** a volume ties the app to one machine in one region. That's already the plan, and the bible app runs the same way, with `suspend`.

### Admin, in two phases

1. **Upload (small, first):** `/admin` page with a file input and a date. You keep writing the same JSON you write today, then upload it instead of committing. The server parses it into the v2 types and checks it before saving: every scripture reference resolves in the KJV, every hymn number exists in `hymnals.json`, dates are valid. It shows a rendered **preview** and you click **Publish**. Mistakes get caught before anyone sees them, which the current build doesn't do (a bad reference renders as nothing).
2. **Editor (later):** a Datastar form for the v2 structure: Sabbath School fields, a repeatable announcements list, and an ordered worship-service list (add, reorder, delete items). The preview updates live over SSE as you type, and scripture text and hymn titles fill in as references are entered. "Copy last week's bulletin" covers most weeks. This is the same server-rendered Datastar style as the bible app's remote controller, so it needs no JS framework.

### Authentication

Only one or two people will ever edit, so keep it small but real:

- **Start:** a single admin password stored as a PBKDF2 hash in `fly secrets` (`Rfc2898DeriveBytes`, built into .NET). Sign-in sets an ASP.NET Core cookie (`HttpOnly`, `Secure`, `SameSite=Strict`), and the login endpoint is rate-limited with the built-in rate limiter. Cookie auth, data protection (keys kept on the volume) and antiforgery all work under NativeAOT.
- **Upgrade:** passkeys (WebAuthn), so there's no password to leak, with one credential per editor. Or GitHub OAuth limited to an allowlist of usernames. Check either one for AOT/trim warnings before you adopt it.
- All `/admin` and `/internal` routes stay off the sitemap and are marked `noindex`. Every save records who saved it (`updated_by`).

## Migration plan

1. **Quick fix now, on the current site:** move the sermons fetch behind a tiny endpoint on the livestream app (or the new site app), then rotate and restrict the YouTube key. This doesn't depend on anything else.
2. Scaffold `PhilaSDB.Site` from the bible app's csproj, Dockerfile and CI. Port the layout, header, footer and home/about/giving.
3. Port the bulletins (types, v1→v2 migration, scripture, hymns). Add snapshot tests comparing rendered bulletin text with the current Astro output for every JSON file.
4. Add the sermons page (server-side YouTube) and the live status SSE + webhook, then add the webhook to `psdb-livestream`.
5. Move bulletins to SQLite on a volume (seeded from the JSON files), then add `/admin` sign-in and validated upload with preview. The full editor comes after the cutover.
6. Deploy to `philasdb-site.fly.dev` and compare against the live site (Lighthouse, page weight, a test stream end to end).
7. Switch DNS: point `philasdb.org` at Fly (`fly certs add philasdb.org`), remove `CNAME` and the Pages workflow.
8. Later: merge `bible`, `livestream` and `philasdb.org` into one repo with a shared `Directory.Packages.props`, one `global.json` (all three are already on .NET 11) and one Aspire apphost running all three apps.

## Open questions

- Who besides you will publish bulletins? That decides whether a shared password is enough or per-person passkeys are worth doing first.
- Is a ~300 ms resume for the first visitor after idle acceptable, or should the site stay running all the time (about $2/month) or sit behind Cloudflare?
- Do the 2025 concert pages need to stay at their URLs, or can old events be archived?
