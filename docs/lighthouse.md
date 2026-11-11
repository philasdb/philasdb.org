# Lighthouse: Astro site vs. the F# site

Lighthouse 13.6.0 run against both implementations, mobile (Moto G Power, slow 4G, simulated throttling) and
desktop presets, **median of 5 runs** per page. Run on 2026-11-11.

- **Old:** the Astro build from `main`, served like GitHub Pages (gzip, 10-minute cache).
- **New:** the Native AOT build of `PhilaSDB.Site`.

The sandbox the runs were made in can't reach the live site or the PageSpeed Insights API, so both sites ran
locally. Pico CSS was served from a local HTTPS stand-in for `cdn.jsdelivr.net` (the same file, which matches the
site's SRI hash) so the old site still paid for a cross-origin request. Google Analytics, Google Maps, Tithe.ly,
PayPal, YouTube and the livestream server weren't reachable, the same for both sites. In production those add the
same cost to both, so compare the two columns with each other rather than with PageSpeed Insights.

## Results

Performance score · first contentful paint (ms) · largest contentful paint (ms) · bytes transferred (KB)

### Mobile

| Page | Old (Astro) | New (F#) |
| :-- | --: | --: |
| `/` | 99 · 1381 · 1654 · 38 | **100 · 1381 · 1424 · 37** |
| `/bulletin` | 100 · 1356 · 1356 · 18 | **100 · 752 · 752 · 16** |
| `/bulletin/2026-11-07` | 100 · 609 · 609 · 5 | 100 · 611 · 611 · 4 |
| `/livestream` | 99 · 1360 · 1957 · 98 | **100 · 754 · 1804 · 83** |
| `/sermons` | 100 · 1358 · 1358 · 69 | **100 · 752 · 752 · 17** |
| `/giving` | 100 · 1422 · 1507 · 31 | 100 · 754 · 1686 · 19 |

### Desktop

| Page | Old (Astro) | New (F#) |
| :-- | --: | --: |
| `/` | 100 · 392 · 406 · 46 | 100 · 391 · 401 · 41 |
| `/bulletin` | 100 · 365 · 365 · 18 | **100 · 203 · 203 · 16** |
| `/bulletin/2026-11-07` | 100 · 168 · 168 · 5 | 100 · 166 · 166 · 4 |
| `/livestream` | 100 · 374 · 487 · 98 | **100 · 247 · 483 · 83** |
| `/sermons` | 100 · 371 · 371 · 69 | **100 · 201 · 201 · 17** |
| `/giving` | 100 · 391 · 391 · 31 | **100 · 205 · 205 · 26** |

Total blocking time is 0 ms on every page for the new site (the old printable bulletin had 72 ms on mobile), and
layout shift is 0–0.007.

| Category (6 pages) | Old | New |
| :-- | :-- | :-- |
| Accessibility | 100, 95, 96, 100, 100, 93 | the same |
| Best practices (mobile) | 92, 96, 100, 96, 96, 92 | 96, 96, 100, 96, 96, 96 |
| SEO | 100, 91, 80, 100, 100, 100 | 100, 100, 92, 100, 100, 100 |

SEO is higher because the bulletin pages now have real canonical URLs (they were `https://philasdb.org/undefined`
or missing). Accessibility is unchanged: the remaining findings (link touch-target size on `/bulletin`, contrast and
heading order on `/giving`, no `<main>` and no description on the printable bulletin) are in both sites and are
content/design choices worth a separate look.

The one page where the new site isn't ahead is mobile LCP on `/giving` (+180 ms). The LCP element there is the
Cash App QR code, a 5–11 KB AVIF in both sites. The old site's results on that page are bimodal (1504 or 1657 ms)
and close to the third-party requests (Tithe.ly, PayPal) that fail instantly in this environment, so treat it as
within the noise until it's measured against the deployed site.

## What it took

The first port matched the old site's text exactly but was slower on mobile: home LCP 2.1 s vs 1.65 s and 109 KB vs
38 KB, and first paint about 150 ms later on most pages. Four changes fixed that and then went past the old site:

1. **Responsive AVIF/WebP images.** Astro's `<Picture>` made AVIF files at several widths; the port served the
   original WebP files (67 KB hero). `scripts/images.py` now makes the sizes (`wwwroot/images/sized/`) and the
   views use `<picture>` with `srcset`, so phones get a 17 KB hero.
2. **Inlined the small stylesheets** (`global.css`, `bulletin.css`), as Astro did, instead of making them a
   render-blocking request.
3. **Vendored Pico CSS** (it isn't being updated upstream). Serving it from the site's own origin removes a DNS
   lookup and TLS connection before the first paint: about 600 ms on mobile for every page with the main layout.
4. **Compressed static files once at the highest level**, like the pages, instead of per request at the fastest
   level: Pico CSS 10.1 KB (18.5 KB before; 11.6 KB from jsDelivr), the favicon 1.9 KB (15 KB), hls.js 100 KB.

It also showed that the header's live-status request to `psdb-livestream.fly.dev` could compete with the first
paint (up to ~0.8 s in some mobile runs), so it now starts after the page has loaded. That request goes away
entirely with the planned server-sent events.

## Running it again

PageSpeed Insights (<https://pagespeed.web.dev>) runs the same Lighthouse audits against a public URL, and is the
easiest way to check the deployed site, including the third-party scripts. Compare `https://philasdb.org` (GitHub
Pages) with `https://philasdb-site.fly.dev` once it's deployed. A Fly machine that has been suspended adds a few
hundred ms to the first request, so run it twice.
