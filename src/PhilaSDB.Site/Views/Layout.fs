/// The page shell every page shares: head (analytics, SEO tags, Pico CSS), header with navigation, footer.
module PhilaSDB.Site.Views.Layout

open Oxpecker.ViewEngine
open PhilaSDB.Site.Assets

let siteUrl = "https://philasdb.org"
let siteName = "Philadelphia Seventh Day Baptist Church"

/// The livestream server (philasdb/livestream). Pages still poll its status from the browser for now.
let livestreamStatusUrl = "https://psdb-livestream.fly.dev/api/status"
let livestreamHlsUrl = "https://psdb-livestream.fly.dev/hls/live/demo/output.m3u8"

type Page =
    { Title: string
      /// Site path ("/about"); the canonical and og:url links are built from it
      Path: string
      Description: string
      /// Stylesheets and inline styles, after the shared ones
      Head: HtmlElement list
      /// Scripts at the end of the body
      Scripts: HtmlElement list }

let page title path =
    { Title = title
      Path = path
      Description = siteName
      Head = []
      Scripts = [] }

let private analytics =
    Fragment() {
        script(src = "https://www.googletagmanager.com/gtag/js?id=G-MZMXKFJVK9").attr ("async", "")

        script () {
            raw
                """window.dataLayer = window.dataLayer || [];
function gtag() { dataLayer.push(arguments); }
gtag("js", new Date());
gtag("config", "G-MZMXKFJVK9");"""
        }
    }

let private header (assets: Fingerprints) =
    Fragment() {
        header (
            style =
                "position: fixed; top: 0; width:100%; background-color: white; z-index: 1000; padding: 4px; box-shadow: 0 2px 5px rgba(0,0,0,0.1);"
        ) {
            nav (class' = "navbar-full") {
                ul () {
                    li () {
                        a (href = "/") {
                            strong () {
                                "PHILADELPHIA 7"
                                sup () { "th" }
                                " DAY BAPTIST CHURCH"
                            }
                        }
                    }
                }

                ul () {
                    li () {
                        a(href = "https://www.youtube.com/@PhiladelphiaSDBChurch", target = "_blank", rel = "noopener noreferrer")
                            .attr ("aria-label", "Visit our YouTube channel") {
                            "YouTube"
                        }
                    }

                    li () {
                        a(href = "https://www.facebook.com/PhilaSDB/", target = "_blank", rel = "noopener noreferrer")
                            .attr ("aria-label", "Visit our Facebook page") {
                            "Facebook"
                        }
                    }
                }

                ul () {
                    li () { a (href = "/") { "HOME" } }
                    li () { a (href = "/about") { "ABOUT" } }
                    // Filled in by livestream-link.js; the plain link is there until then (and without JavaScript)
                    li () {
                        raw $"""<livestream-link url="{livestreamStatusUrl}"><a href="/livestream"><span class="live-indicator">⚪</span> <span class="live-label">LIVESTREAM</span></a></livestream-link>"""
                    }
                    li () { a (href = "/sermons") { "SERMONS" } }
                    li () { a (href = "/giving") { "GIVING" } }
                }
            }

            nav (class' = "navbar-mobile") {
                ul () {
                    li () {
                        strong () {
                            "PHILADELPHIA 7"
                            sup () { "th" }
                            " DAY BAPTIST CHURCH"
                        }
                    }
                }

                ul () {
                    li () {
                        details (class' = "dropdown") {
                            summary () {
                                div (class' = "hamburger-icon") {
                                    span ()
                                    span ()
                                    span ()
                                }
                            }

                            ul().attr ("dir", "rtl") {
                                li () { a (href = "/") { "HOME" } }
                                li () { a (href = "/about") { "ABOUT" } }
                                li () { a (href = "/livestream") { "LIVESTREAM" } }
                                li () { a (href = "/sermons") { "SERMONS" } }
                                li () { a (href = "/giving") { "GIVING" } }
                            }
                        }
                    }
                }
            }
        }

        script(src = assets.Url "/js/livestream-link.js", type' = "module")
    }

let private footer =
    footer (class' = "container") {
        div (style = "text-align: center;") {
            address () {
                div () { "6841 Ardleigh St, Philadelphia, PA 19119" }
                div () { a (href = "tel:+12673352987") { "(267) 335-2987" } }
                div () { a (href = "mailto:philasdb@gmail.com") { "philasdb@gmail.com" } }
                div () { raw "&copy; Philadelphia Seventh Day Baptist Church, All Rights Reserved" }
            }
        }
    }

let canonicalUrl (path: string) = if path = "/" then siteUrl + "/" else siteUrl + path

/// The main layout, as the Astro site's MainLayout
let render (assets: Fingerprints) (page: Page) (content: HtmlElement) =
    let pageTitle = $"{page.Title} | {siteName}"
    let canonical = canonicalUrl page.Path

    html(lang = "en").attr ("data-theme", "light") {
        head () {
            analytics
            meta (charset = "UTF-8")
            meta (name = "viewport", content = "width=device-width, initial-scale=1")
            meta (name = "color-scheme", content = "light dark")
            meta (name = "description", content = page.Description)
            meta().attr("property", "og:type").attr ("content", "website")
            meta().attr("property", "og:title").attr ("content", pageTitle)
            meta().attr("property", "og:description").attr ("content", page.Description)
            meta().attr("property", "og:url").attr ("content", canonical)
            meta (name = "author", content = siteName)

            meta (
                name = "keywords",
                content =
                    "Philadelphia Seventh Day Baptist Church, SDB, Sabbath worship, Mt. Airy, Saturday worship, Bible study, Philadelphia church, Christian, seventh day baptist"
            )

            link (rel = "canonical", href = canonical)
            link (rel = "icon", type' = "image/x-icon", href = assets.Url "/favicon.ico")

            link (
                rel = "stylesheet",
                href = "https://cdn.jsdelivr.net/npm/@picocss/pico@2.1.1/css/pico.min.css",
                integrity = "sha384-L1dWfspMTHU/ApYnFiMz2QID/PlP1xCW9visvBdbEkOLkSSWsP6ZJWhPw6apiXxU",
                crossorigin = "anonymous"
            )

            title () { pageTitle }
            link (rel = "stylesheet", href = assets.Url "/css/global.css")
            style () { raw "html, body { margin: 0; width: 100%; height: 100%; }" }

            for element in page.Head do
                element
        }

        body () {
            header assets
            main (class' = "container", style = "margin-top: 80px;") { content }
            footer

            for element in page.Scripts do
                element
        }
    }

/// The breadcrumb trail at the top of most pages: Home › … › current page
let breadcrumb (trail: (string * string) list) (current: string) =
    nav().attr ("aria-label", "breadcrumb") {
        ul () {
            li () { a (href = "/") { "Home" } }

            for href', text in trail do
                li () { a (href = href') { text } }

            li () { current }
        }
    }
