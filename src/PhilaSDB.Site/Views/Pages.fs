/// The site's fixed pages, ported from the Astro components and pages one for one.
module PhilaSDB.Site.Views.Pages

open Oxpecker.ViewEngine
open PhilaSDB.Site.Assets
open PhilaSDB.Site.Views.Layout

// ─── Home ──────────────────────────────────────────────────────────────────

let private welcome =
    section (id = "welcome") {
        h1 () { "WELCOME" }

        p () {
            "Welcome to Philadelphia Seventh Day Baptist Church! We're a Bible-centered "
            a(href = "https://www.seventhdaybaptist.org/", target = "_blank") { "Seventh Day Baptist" }

            " church in the Mt. Airy neighborhood of Philadelphia. We invite you to worship with us and join us for fellowship."
        }
    }

let private hero (assets: Fingerprints) =
    section (id = "hero") {
        div (style = "position: relative; width: 100%; height: auto; aspect-ratio: 16/9;") {
            responsive assets "cover_image" [ 480; 768; 925 ] "(max-width: 925px) 100vw, 925px"
            <| img (
                src = assets.Url "/images/cover_image.webp",
                alt = "Philadelphia Seventh Day Baptist Church",
                width = 925,
                height = 520,
                loading = "eager",
                fetchpriority = "high",
                decoding = "async",
                style = "width: 100%; height: 100%; object-fit: cover;"
            )
        }

        blockquote () {
            div () {
                i () {
                    sup () { strong () { "8" } }
                    " Remember the sabbath day, to keep it holy. "
                    strong () { "- Exodus 20:8" }
                }
            }
        }
    }

let private pastor (assets: Fingerprints) =
    section (id = "pastor") {
        h2 () { "PASTOR KENROY CRUICKSHANK" }

        div (class' = "pastor-info") {
            div (class' = "pastor-bio") {
                blockquote () {
                    responsive assets "pastor" [ 180; 270 ] "180px"
                    <| img (
                        src = assets.Url "/images/pastor.webp",
                        alt = "Pastor",
                        width = 180,
                        height = 180,
                        loading = "lazy",
                        decoding = "async"
                    )

                    p () {
                        i () {
                            "\"When I look back to over 35 years ago, it seems like only yesterday that I got off the plane and I was met by my wife Mauleen. As I arrived in Ambler, it was as if I was in a strange world. I could not understand the people and they could not understand me. However, I believed deep in my heart that I was called to start a Seventh Day Baptist Church in this area. The ministry is part of me and the conviction was deep in my spirit. Over the years, the Lord has blessed me with two children and a church. I accept the challenge to seek out new opportunities and ways of leading others to Christ in and around Philadelphia. As pastor, I invite you to come and worship with us. - "
                            b () { "Pastor Kenroy Cruickshank" }
                        }
                    }
                }
            }
        }
    }

let private services =
    let withTime (name: string) (time: string) =
        tr () {
            td () {
                div () { name }
                " "
                div () { b () { time } }
            }
        }

    section (id = "services") {
        h2 () { "SERVICES" }
        a (href = "/bulletin") { "Latest Bulletin" }

        div (class' = "grid") {
            div () {
                table () {
                    thead () { tr () { th () { "Sabbath (Saturday)" } } }

                    tbody () {
                        tr () {
                            td () {
                                "Sabbath School  "
                                b () { "@ 11:00AM" }
                            }
                        }

                        tr () { td () { "Praise & Worship" } }
                        tr () { td () { "Divine Worship" } }
                        tr () { td () { "Lunch" } }
                        tr () { td () { "Vespers" } }
                    }
                }
            }

            div () {
                table () {
                    thead () { tr () { th () { "Additional Services" } } }

                    tbody () {
                        withTime "Prayer & Bible Study" "Wednesdays, 6-7:30PM"
                        withTime "Prayer Meeting" "First Monday of the Month @ 7:00PM on Zoom"

                        withTime
                            "Women's Meeting"
                            "Every Monday @ 7:00PM on Zoom (Except for the first Monday of the month)"
                    }
                }
            }
        }
    }

let private location =
    section (id = "location") {
        h2 () { "VISIT US" }

        p () {
            "We are located at the corner of Gorgas and Ardleigh St in Mt. Airy, Philadelphia a few blocks away from Stenton Avenue and a short walk away from the Stenton Station on the Chestnut Hill East Line. Our services start at "
            strong () { "11:00 AM" }
            ", beginning with Sabbath School on Saturdays."
        }

        address () {
            div () { "6841 Ardleigh St" }
            div () { "Philadelphia, PA 19119" }
            div () { a (href = "tel:+12673352987") { "(267) 335-2987" } }
            div () { a (href = "mailto:philasdb@gmail.com") { "philasdb@gmail.com" } }
        }

        iframe(
            title = "Philadelphia Seventh Day Baptist Church Location",
            style = "width: 100%; height: 450px; border: 0;",
            loading = "lazy",
            src =
                "https://www.google.com/maps/embed/v1/place?q=place_id:ChIJx2YWY5W5xokRrhQ-T3jbglk&key=AIzaSyCmUq3PwmL4LFtfQABxKIbAl-4yxgn4J8M"
        )
            .attr ("allowfullscreen", "")
    }

let home (assets: Fingerprints) =
    render
        assets
        (page "Home" "/")
        (Fragment() {
            welcome
            hero assets
            pastor assets
            services
            location
        })

// ─── About ─────────────────────────────────────────────────────────────────

let about (assets: Fingerprints) =
    render
        assets
        (page "About" "/about")
        (Fragment() {
            breadcrumb [] "About"

            section (id = "beliefs") {
                h1 () { "OUR BELIEFS" }

                p () {
                    "At Philadelphia Seventh Day Baptist Church we consider liberty of thought under the guidance of the Holy Spirit to be essential to Christian belief and practice. Therefore we encourage the unhindered study and open discussion of Scripture. We uphold the individual's freedom of conscience in seeking to determine and obey the will of God. The following statement is not intended to be exhaustive, but is an expression of our common belief, which is derived from our understanding of scripture:"
                }

                blockquote () {
                    div () {
                        i () {
                            sup () { "17" }
                            " Now the Lord is that Spirit: and where the Spirit of the Lord is, there is liberty. "
                            sup () { "18" }

                            " But we all, with open face beholding as in a glass the glory of the Lord, are changed into the same image from glory to glory, even as by the Spirit of the Lord. "

                            strong () { "- 2 Corinthians 3:17-18" }
                        }
                    }

                    div () {
                        i () {
                            sup () { "15" }

                            " Study to shew thyself approved unto God, a workman that needeth not to be ashamed, rightly dividing the word of truth. "

                            strong () { "- 2 Timothy 2:15" }
                        }
                    }
                }

                h3 () { "MORE" }

                div () {
                    a(href = "https://www.seventhdaybaptist.org/statement-of-belief/", target = "_blank") {
                        "Statement of Belief"
                    }
                }

                div () { a(href = "https://www.seventhdaybaptist.org/salvation/", target = "_blank") { "Salvation" } }
                div () { a(href = "https://www.seventhdaybaptist.org/sabbath/", target = "_blank") { "Sabbath" } }
            }
        })

// ─── Giving ────────────────────────────────────────────────────────────────

let giving (assets: Fingerprints) =
    render
        assets
        (page "Giving" "/giving")
        (Fragment() {
            breadcrumb [] "Giving"
            h1 (style = "text-align: center;") { "GIVING" }

            p (style = "text-align: center; font-size: 1.2em;") {
                "Thank you for your generosity! Your contributions help us to continue our mission and serve our community."
            }

            hr ()

            div (class' = "grid") {
                div (id = "giving-tithely") {
                    h3 () { "Tithe.ly" }

                    button(
                        class' = "tithely-give-button",
                        style =
                            "background-color: #00DB72; font-family: inherit; font-weight: bold; font-size: 19px; padding: 15px 70px; border-radius: 4px; cursor: pointer; background-image: none; color: white; text-shadow: none; display: inline-block; float: none; border: none;"
                    )
                        .attr ("data-form", "6620135f-5d42-11ee-90fc-1260ab546d11") {
                        "Give"
                    }

                    script (src = "https://static.tithely.com/give/give.js", defer = true)
                }

                div (id = "giving-cashapp") {
                    h3 () { "Cash App" }
                    div () { a(href = "https://cash.app/$philasdb", target = "_blank") { "$PhilaSDB" } }

                    a (href = "https://cash.app/$philasdb", target = "_blank") {
                        responsive assets "cashapp" [ 256; 288 ] "256px"
                        <| img (
                            id = "cashapp",
                            class' = "centered",
                            src = assets.Url "/images/cashapp.webp",
                            alt = "Cash App QR code",
                            width = 256,
                            height = 256,
                            loading = "eager",
                            decoding = "async"
                        )
                    }
                }

                div (id = "giving-paypal") {
                    h3 () { "PayPal" }

                    div (id = "donate-button-container") {
                        div (id = "donate-button")
                        script(src = "https://www.paypalobjects.com/donate/sdk/donate-sdk.js").attr ("charset", "UTF-8")

                        script () {
                            raw
                                """PayPal.Donation.Button({
  env: "production",
  hosted_button_id: "ZQF5AXVD49T78",
  image: {
    src: "https://www.paypalobjects.com/en_US/i/btn/btn_donate_LG.gif",
    alt: "Donate with PayPal button",
    title: "PayPal - The safer, easier way to pay online!",
  },
}).render("#donate-button");"""
                        }
                    }
                }
            }
        })

// ─── Tyler Gahagan's book wish list ────────────────────────────────────────

let private tylerStyles =
    """.wish-list { padding: 2rem 1rem 4rem; }
.wish-list-card { box-sizing: border-box; max-width: 680px; margin: 0 auto; padding: clamp(2rem, 7vw, 4rem); border: 1px solid #e5e9ed; border-radius: 1.25rem; background: linear-gradient(145deg, #fff 55%, #f4f7f8); box-shadow: 0 16px 48px rgb(28 48 68 / 8%); text-align: center; }
.book-mark { display: grid; width: 4rem; height: 4rem; margin: 0 auto 1.5rem; place-items: center; border-radius: 50%; background: #edf3f4; color: #315f63; }
.book-mark svg { width: 2rem; height: 2rem; }
.intro { color: #344b5a; font-size: 1.15rem; line-height: 1.7; }
.wish-list-card > p:not(.link-note) { max-width: 520px; margin-right: auto; margin-left: auto; line-height: 1.75; }
.registry-link { display: inline-flex; align-items: center; justify-content: center; gap: 0.6rem; margin-top: 1rem; padding: 0.9rem 1.35rem; border-radius: 0.5rem; background: #315f63; color: #fff; font-weight: 700; text-decoration: none; transition: background-color 150ms ease; }
.registry-link:hover { background: #254a4e; color: #fff; }
.registry-link:focus-visible { outline: 3px solid #b27a32; outline-offset: 3px; }
.link-note { margin: 1rem 0 0; color: #586771; font-size: 0.9rem; }"""

let tylerGahagan (assets: Fingerprints) =
    render
        assets
        { page "Tyler Gahagan's Book Wish List" "/tyler-gahagan" with
            Description =
                "Support Philadelphia Seventh Day Baptist Church member Tyler Gahagan as he begins pastoral ministry classes. View his book wish list."
            Head = [ style () { raw tylerStyles } ] }
        (Fragment() {
            breadcrumb [] "Tyler's Book Wish List"

            div (class' = "wish-list") {
                section(class' = "wish-list-card").attr ("aria-labelledby", "page-title") {
                    div(class' = "book-mark").attr ("aria-hidden", "true") {
                        raw
                            """<svg viewBox="0 0 48 48" fill="none"><path d="M24 14.5c-4.4-3.1-10.2-3.7-16-1.8v22c5.8-1.9 11.6-1.3 16 1.8m0-22c4.4-3.1 10.2-3.7 16-1.8v22c-5.8-1.9-11.6-1.3-16 1.8m0-22v22" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"/></svg>"""
                    }

                    p (class' = "intro") {
                        "Tyler Gahagan, a member of our church, has begun pastoral ministry classes. These books will support his studies and preparation for ministry."
                    }

                    p () {
                        "If you'd like to encourage Tyler along the way, you can browse his book wish list on Amazon. Every gift is completely optional, and your support is appreciated."
                    }

                    a (
                        class' = "registry-link",
                        href = "https://www.amazon.com/registries/gl/guest-view/KXFF62GWNUEW",
                        target = "_blank",
                        rel = "noopener noreferrer"
                    ) {
                        "View Tyler's book wish list on Amazon "
                        span().attr ("aria-hidden", "true") { "↗" }
                    }

                    p (class' = "link-note") {
                        "The list opens on Amazon. You can choose whether or not to purchase anything."
                    }
                }
            }
        })

// ─── Concerts ──────────────────────────────────────────────────────────────

let private concertCard (title: string) (intro: string) (date: string) (tickets: string) (rest: HtmlElement) =
    section (
        style =
            "max-width: 700px; margin: 2rem auto; background: #f7f2ff; border-radius: 1.5rem; box-shadow: 0 4px 24px rgba(80,0,80,0.08); overflow: hidden;"
    ) {
        div (style = "padding: 2rem;") {
            h1 (
                style =
                    "color: #6c2eb7; font-size: 2.2rem; margin-bottom: 0.5rem; font-family: 'Montserrat', sans-serif; font-weight: 700;"
            ) {
                title
            }

            p (style = "font-size: 1.1rem; color: #222; margin-bottom: 1.5rem;") { intro }

            div (style = "margin-bottom: 1.5rem;") {
                div (style = "margin-bottom: 0.75rem;") {
                    strong () { "Date:" }
                    " "
                    span (style = "color: #6c2eb7;") { date }
                }

                div (style = "margin-bottom: 0.75rem;") {
                    strong () { "Time:" }
                    " "
                    span (style = "color: #6c2eb7;") { "4:00 PM" }
                }

                div (style = "margin-bottom: 0.75rem;") {
                    strong () { "Location:" }
                    " 6841 Ardleigh Street, Philadelphia, PA 19119"
                }
            }

            div (
                style =
                    "background: #f8f9fa; border-radius: 0.75rem; padding: 1rem; margin-bottom: 1.5rem; border-left: 3px solid #6c2eb7;"
            ) {
                div (style = "margin-bottom: 0.5rem;") {
                    strong () { "Tickets:" }
                    " "
                    tickets
                }

                div (style = "color: #666; font-size: 0.9rem;") { "Refreshments available for purchase" }
            }

            rest
        }
    }

/// A colored button-style link that darkens on hover
let private buttonLink (href': string) (color: string) (hover: string) (text: string) =
    a(
        href = href',
        style =
            $"display: inline-block; background: {color}; color: white; padding: 0.75rem 1.5rem; border-radius: 0.5rem; text-decoration: none; font-weight: 600; transition: background-color 0.3s;"
    )
        .attr("onmouseover", $"this.style.backgroundColor='{hover}'")
        .attr ("onmouseout", $"this.style.backgroundColor='{color}'") {
        text
    }

let concert2026 (assets: Fingerprints) =
    render
        assets
        (page "Annual Fundraising Concert 2026" "/concert/2026")
        (concertCard
            "Annual Fundraising Concert"
            "Join us for an afternoon of praise, worship, and inspiration at Philadelphia 7th Day Baptist Church!"
            "September 26, 2026"
            "$20 for adults, $10 for children 15 and under"
            (div (
                style =
                    "background: #f0f8ff; border-radius: 1rem; padding: 1.5rem; border: 2px solid #e6f3ff; text-align: center;"
             ) {
                h2 (style = "color: #6c2eb7; font-size: 1.3rem; margin-bottom: 0.75rem;") { "RSVP by September 5th" }

                p (style = "margin-bottom: 1rem;") {
                    "Please let us know you're coming, and feel free to share this invitation with friends and family!"
                }

                buttonLink
                    "mailto:philasdb@gmail.com?subject=Annual%20Fundraising%20Concert%20RSVP"
                    "#6c2eb7"
                    "#5a2596"
                    "RSVP via Email"
            }))

let concert2025 (assets: Fingerprints) =
    let stream (heading: string) (icon: string) (color: string) (last: bool) (body: HtmlElement) =
        div (
            style =
                $"""background: #fff; border-radius: 0.75rem; padding: 1.25rem; {if last then "" else "margin-bottom: 1rem; "}box-shadow: 0 2px 8px rgba(0,0,0,0.1);"""
        ) {
            h3 (
                style = $"color: {color}; font-size: 1.1rem; margin-bottom: 0.75rem; display: flex; align-items: center;"
            ) {
                span (style = "margin-right: 0.5rem;") { icon }
                " "
                heading
            }

            body
        }

    let phone (number: string) (tel: string) =
        a (
            href = tel,
            style = "color: #2d5aa0; text-decoration: none; font-family: monospace; font-size: 0.9rem;"
        ) {
            number
        }

    render
        assets
        (page "Annual Fundraising Concert 2025" "/concert/2025")
        (concertCard
            "Annual Fundraising Concert"
            "Join us for an inspiring evening of music and fellowship at Philadelphia 7th Day Baptist Church!"
            "4th October 2025"
            "$20 (17+), $10 (5-16), Free (under 5)"
            (div (
                style =
                    "background: #f0f8ff; border-radius: 1rem; padding: 1.5rem; margin-bottom: 1.5rem; border: 2px solid #e6f3ff;"
             ) {
                h2 (style = "color: #6c2eb7; font-size: 1.4rem; margin-bottom: 1rem; text-align: center;") {
                    "Join Us Online"
                }

                stream
                    "Zoom Meeting"
                    "📹"
                    "#2d5aa0"
                    false
                    (Fragment() {
                        div (style = "margin-bottom: 0.75rem;") {
                            strong () { "Meeting ID:" }
                            " 839 4792 5443"
                            br ()
                            strong () { "Passcode:" }
                            " 945577"
                        }

                        div (style = "margin-bottom: 1rem;") {
                            strong () { "One-tap mobile:" }
                            br ()
                            phone "+1 (929) 436-2866" "tel:+19294362866,,83947925443#,,,,*945577#"
                            br ()
                            phone "+1 (301) 715-8592" "tel:+13017158592,,83947925443#,,,,*945577#"
                        }

                        buttonLink
                            "https://us02web.zoom.us/j/83947925443?pwd=Yy0OAWxeb14a80NqjYBpa4JmXft5da.1"
                            "#2d5aa0"
                            "#1e3d6b"
                            "Join Zoom Meeting"
                    })

                stream "Our Website Stream" "🌐" "#6c2eb7" false (buttonLink "/livestream" "#6c2eb7" "#5a2596" "Watch on Our Website")

                stream
                    "YouTube Live"
                    "📺"
                    "#cc0000"
                    false
                    (buttonLink "https://youtube.com/live/6LLhq5QE7LQ?feature=share" "#cc0000" "#a00000" "Watch on YouTube")

                stream
                    "Facebook Live"
                    "📘"
                    "#1877f2"
                    true
                    (buttonLink "https://www.facebook.com/PhilaSDB/live_videos/" "#1877f2" "#0d5bb8" "Watch on Facebook")
            }))

let private menuStyles =
    """* { margin: 0; padding: 0; box-sizing: border-box; }
body { font-family: 'Arial', sans-serif; padding: 1rem; max-width: 800px; margin: 0 auto; background: white; }
.menu-container { padding: 1rem; }
.section { margin-bottom: 1.25rem; }
.section-title { color: #000; font-size: 1.4rem; margin-bottom: 0.6rem; border-bottom: 2px solid #000; padding-bottom: 0.3rem; font-weight: bold; }
.menu-item { display: flex; justify-content: space-between; padding: 0.5rem 0; border-bottom: 1px dashed #000; }
.menu-item:last-child { border-bottom: none; }
.item-name { font-size: 1.15rem; font-weight: 600; color: #000; }
.item-description { font-size: 0.95rem; color: #000; margin-top: 0.15rem; font-style: italic; }
.item-price { font-size: 1.15rem; font-weight: bold; color: #000; white-space: nowrap; margin-left: 1rem; }
.cashapp-section { text-align: center; margin-top: 1.25rem; padding-top: 1rem; border-top: 2px solid #000; }
.cashapp-title { font-size: 1rem; color: #000; margin-bottom: 0.4rem; font-weight: bold; }
.cashapp-qr { max-width: 150px; margin: 0 auto; }
.cashapp-qr img { max-width: 100%; height: auto; }
@media print { body { padding: 0.5rem; } .menu-container { border-width: 2px; padding: 1rem; } }"""

/// The printable menu for the 2025 concert: a page of its own, without the site's layout
let concert2025Menu (assets: Fingerprints) =
    let item (name: string) (description: string option) (price: string) =
        div (class' = "menu-item") {
            div () {
                div (class' = "item-name") { name }

                match description with
                | Some d -> div (class' = "item-description") { d }
                | None -> ()
            }

            div (class' = "item-price") { price }
        }

    let sides = Some "Served with choice of sides"

    html().attr ("lang", "en") {
        head () {
            meta (charset = "UTF-8")
            meta (name = "viewport", content = "width=device-width, initial-scale=1.0")
            title () { "Concert Menu - October 4th, 2025" }
            style () { raw menuStyles }
        }

        body () {
            div (class' = "menu-container") {
                div (class' = "section") {
                    h2 (class' = "section-title") { "Platters" }
                    item "Chicken Platter" sides "$_____"
                    item "Fish Platter" sides "$_____"
                    item "Curry Goat Platter" sides "$_____"
                }

                div (class' = "section") {
                    h2 (class' = "section-title") { "Appetizers" }
                    item "Fish Fritters" None "$_____"
                }

                div (class' = "section") {
                    h2 (class' = "section-title") { "Desserts" }
                    item "Banana or Sweet Potato Bread " None "$_____"
                    item "Banana or Sweet Potato Bread (Half)" None "$_____"
                }

                div (class' = "section") {
                    h2 (class' = "section-title") { "Beverages" }
                    item "Drinks & Water" None "$1.00"
                }

                div (class' = "cashapp-section") {
                    p (class' = "cashapp-title") { "Pay with Cash App" }

                    div (class' = "cashapp-qr") {
                        responsive assets "cashapp" [ 150; 288 ] "150px"
                        <| img (
                            src = assets.Url "/images/cashapp.webp",
                            alt = "Cash App Payment",
                            width = 288,
                            height = 288,
                            decoding = "async"
                        )
                    }
                }
            }
        }
    }

// ─── Livestream and sermons ────────────────────────────────────────────────

let livestream (assets: Fingerprints) =
    render
        assets
        { page "Livestream" "/livestream" with
            Scripts = [ script (src = assets.Url "/js/livestream.js", type' = "module") ] }
        (Fragment() {
            breadcrumb [] "Livestream"

            section(id = "livestream", style = "position: relative;")
                .attr("data-status-url", livestreamStatusUrl)
                .attr("data-hls-url", livestreamHlsUrl)
                .attr ("data-hls-script", assets.Url "/js/hls.light.min.js") {
                div (
                    id = "offline-overlay",
                    style =
                        "position: absolute; top: 0; left: 0; width: 100%; height: 100%; display: none; z-index: 10; background: rgba(0, 0, 0, 0.85); border-radius: 8px;"
                ) {
                    div (
                        style =
                            "display: flex; flex-direction: column; align-items: center; justify-content: center; height: 100%; padding: 2rem; text-align: center; color: white;"
                    ) {
                        h2 (style = "margin-bottom: 1rem; color: white;") { "Livestream Currently Offline" }

                        p (style = "font-size: 1.2rem; margin: 0.5rem 0; color: white; font-weight: 500;") {
                            "Join us live on "
                            strong (style = "color: white;") { "Saturdays at 11:00 AM EST" }
                        }

                        p (style = "margin: 0.5rem 0; color: white; opacity: 0.95;") {
                            "Sabbath School, Praise & Worship, and Divine Worship"
                        }

                        p (style = "margin: 1.5rem 0;") {
                            a(href = "/sermons", style = "background-color: #0066cc; color: white; font-weight: 600;")
                                .attr ("role", "button") {
                                "Watch Previous Sermons"
                            }
                        }

                        p (style = "font-size: 0.85rem; opacity: 0.7; margin-top: 1rem;") { "Checking for livestream..." }
                    }
                }

                video (
                    id = "livestream-video",
                    style = "position: relative; width: 100%; height: auto; aspect-ratio: 16/9; background: #000;",
                    controls = true,
                    playsinline = true,
                    preload = "none",
                    poster = assets.Url "/images/cover_image.webp"
                ) {
                    p () { "To view this video please enable JavaScript." }
                }
            }
        })

let sermons (assets: Fingerprints) =
    render
        assets
        { page "Sermons" "/sermons" with
            Scripts = [ script (src = assets.Url "/js/sermons.js", type' = "module") ] }
        (Fragment() {
            h1 () { "Sermons" }
            p () { "Welcome to our Sermons page! Here you can find a collection of our recent sermons." }
            div (id = "sermons") { "Loading sermons..." }
        })

// ─── Not found ─────────────────────────────────────────────────────────────

let notFound (assets: Fingerprints) =
    render
        assets
        (page "Page Not Found" "/404")
        (Fragment() {
            h1 () { "Page not found" }

            p () {
                "We couldn't find that page. "
                a (href = "/") { "Go to the home page" }
                " or see the "
                a (href = "/bulletin") { "latest bulletin" }
                "."
            }
        })
