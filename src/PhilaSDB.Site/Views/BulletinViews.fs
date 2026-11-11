/// Bulletin pages: the latest bulletin, one bulletin by date (printable), and the archive.
module PhilaSDB.Site.Views.BulletinViews

open System
open System.Globalization
open Oxpecker.ViewEngine
open PhilaSDB.Scripture
open PhilaSDB.Site.Assets
open PhilaSDB.Site.Bulletins
open PhilaSDB.Site.Hymnals
open PhilaSDB.Site.Views.Layout

/// What a bulletin needs to look things up while it renders
type Context =
    { Bible: Bible
      Hymnals: Hymnals
      Assets: Fingerprints }

let private formatDate (format: string) (date: DateOnly) =
    date.ToString(format, CultureInfo.InvariantCulture)

/// "November 07, 2026"
let longDate = formatDate "MMMM dd, yyyy"

let private bibleUrl (reference: string) =
    "https://philasdb-bible.fly.dev/passage?search=" + Uri.EscapeDataString reference

let private scripture (title: string) (reference: string) =
    div (class' = "scripture") {
        b () { title + ":" }
        " "
        a(href = bibleUrl reference, target = "_blank") { reference }
    }

/// The verses, numbered, in italics. Nothing if the reference doesn't resolve.
let private passage (ctx: Context) (reference: string) : HtmlElement =
    match ctx.Bible.Lookup reference with
    | [] -> Fragment()
    | verses ->
        em () {
            for v in verses do
                sup () { string v.Verse }
                v.Text
        }

let private presenter (presentedBy: string option) : HtmlElement =
    match presentedBy with
    | Some name -> div () { i () { name } }
    | None -> Fragment()

type private ResolvedSong =
    { Title: string option
      Hymnal: string option
      HymnNumber: string option
      Url: string option
      Artist: string option
      AdditionalInfo: string option }

let private resolve (ctx: Context) (song: SongRef) =
    let fromHymnal code number =
        ctx.Hymnals.TryHymn(code, number)
        |> Option.map (fun (hymnal, hymn) ->
            { Title = Some hymn.Title
              Hymnal = Some hymnal.Name
              HymnNumber = Some(string hymn.Number)
              Url = Some hymn.Url
              Artist = None
              AdditionalInfo = None })

    match song with
    | Hymn number -> fromHymnal defaultCode number
    | Custom s ->
        let looked =
            match s.HymnalCode, s.HymnNumber |> Option.bind (fun n -> match Int32.TryParse n with | true, v -> Some v | _ -> None) with
            | Some code, Some number -> fromHymnal code number
            | _ -> None

        match looked with
        | Some hymn ->
            Some
                { hymn with
                    Artist = s.Artist
                    AdditionalInfo = s.AdditionalInfo }
        | None ->
            Some
                { Title = s.Title
                  Hymnal = s.Hymnal
                  HymnNumber = s.HymnNumber
                  Url = s.Url
                  Artist = s.Artist
                  AdditionalInfo = s.AdditionalInfo }

let private songLink (url: string option) =
    match url with
    | Some url -> a (href = url, target = "_blank")
    | None -> a ()

let private song (ctx: Context) (reference: SongRef) : HtmlElement =
    match resolve ctx reference with
    | None -> Fragment()
    | Some s ->
        div (class' = "song") {
            let title = s.Title |> Option.defaultValue ""

            match s.Url, s.HymnNumber with
            | Some _, None -> songLink s.Url { title }
            | _ -> em () { title }

            match s.HymnNumber with
            | Some number ->
                div () {
                    songLink s.Url {
                        match s.Hymnal with
                        | Some hymnal -> $"{hymnal} #{number}"
                        | None -> $"#{number}"
                    }
                }
            | None -> ()

            match s.Artist with
            | Some artist -> div () { $"by {artist}" }
            | None -> ()

            match s.AdditionalInfo with
            | Some info -> div () { $"by {info}" }
            | None -> ()
        }

let private announcement (a': Announcement) =
    div (class' = "announcement") {
        b () {
            formatDate "MMMM d" a'.StartDate

            match a'.Time with
            | Some time -> span () { $" @ {time}" }
            | None -> ()

            match a'.EndDate with
            | Some endDate -> span () { " - " + formatDate "%d" endDate }
            | None -> ()
        }

        div () { a'.Description }

        match a'.AdditionalInfo with
        | Some info -> i () { info }
        | None -> ()

        match a'.Location with
        | Some location -> div () { address () { small () { location } } }
        | None -> ()

        match a'.Link with
        | Some link -> div (class' = "hide-on-print") { a (href = link.Href, target = "_blank") { link.Name } }
        | None -> ()
    }

let private bold (italic: bool) (text: string) =
    b () {
        if italic then i () { text } else text
    }

let private worshipItem (ctx: Context) (item: WorshipItem) : HtmlElement =
    match item with
    | WorshipLeader name ->
        div (class' = "item") {
            b () { "Worship Leader:" }
            $" {name}"
        }
    | ThoughtOfTheDay(reference, presentedBy) ->
        div (class' = "item") {
            scripture "Thought of the Day" reference
            passage ctx reference
            presenter presentedBy
        }
    | SongItem(label, s, presentedBy) ->
        div (class' = "item") {
            div () { b () { label } }
            song ctx s
            presenter presentedBy
        }
    | Text(label, content, italic, isSmall, presentedBy) ->
        div (class' = "item") {
            match content with
            | Some content -> div () { if isSmall then small () { bold italic content } else bold italic content }
            | None -> ()

            div () { b () { label } }
            presenter presentedBy
        }
    | Scripture(label, reference, showPassage, presentedBy) ->
        div (class' = "item") {
            scripture label reference
            if showPassage then passage ctx reference
            presenter presentedBy
        }
    | TithesAndOffering(s, presentedBy) ->
        Fragment() {
            div (class' = "item") {
                div () {
                    b () { "Tithes and Offering" }

                    span (class' = "hide-on-print donate-link") {
                        a (href = "/giving", target = "_blank") { "Giving (Click Here)" }
                    }
                }

                song ctx s
            }

            div (class' = "item") {
                div () { b () { "Prayer for Tithes and Offering" } }
                presenter presentedBy
            }
        }
    | Youth ->
        div (class' = "item") {
            div () { small () { b () { i () { "*Children and young people separate into Young Leaders Church*" } } } }
        }
    | MessageItem message ->
        div (class' = "item") {
            div () { b () { "Message" } }
            em () { message.Title }
            div () { b () { message.Speaker } }
        }
    | CommunionItem communion ->
        div (class' = "item") {
            div () {
                b () { "Communion" }

                match communion.Description with
                | Some description -> small () { em () { description } }
                | None -> ()
            }

            div () {
                small () { em () { "Participation is open to all who are in good relationship with the Lord" } }
            }

            for s in communion.Songs do
                song ctx s
        }
    | CustomItem(label, content, s, presentedBy) ->
        div (class' = "item") {
            div () {
                match s with
                | Some s -> song ctx s
                | None -> ()

                b () { label }

                match content with
                | Some content -> div () { content }
                | None -> ()

                presenter presentedBy
            }
        }

/// The bulletin itself: Sabbath School and announcements on the left, the order of service on the right
let private bulletinBody (ctx: Context) (bulletin: Bulletin) =
    div (class' = "container") {
        div (class' = "left-column") {
            h1 () { formatDate "MMMM d, yyyy" bulletin.Date }

            section (class' = "sabbath-school") {
                h2 () { "Sabbath School @ 11am" }

                div () {
                    b () { "Topic:" }
                    $" \"{bulletin.SabbathSchool.Topic}\""
                }

                scripture "Study" bulletin.SabbathSchool.Study
                scripture "Background" bulletin.SabbathSchool.Background
                scripture "Devotional" bulletin.SabbathSchool.Devotional
            }

            section (class' = "announcements") {
                h2 () { "Announcements" }

                for a' in bulletin.Announcements do
                    announcement a'
            }

            for note in bulletin.Notes do
                div (class' = "notes") { b () { note } }
        }

        div (class' = "right-column") {
            section () {
                div (class' = "item") { h2 () { "Divine Worship" } }

                for item in bulletin.WorshipService do
                    worshipItem ctx item
            }
        }
    }

/// /bulletin/{date}: the bulletin alone on the page, for printing and sharing
let standalone (ctx: Context) (bulletin: Bulletin) =
    html().attr ("lang", "en") {
        head () {
            meta (charset = "UTF-8")
            meta (name = "viewport", content = "width=device-width, initial-scale=1.0")
            title () { $"Philadelphia Seventh Day Baptist Church | Bulletin | {longDate bulletin.Date}" }
            link (rel = "canonical", href = canonicalUrl $"/bulletin/{bulletin.Slug}")
            link (rel = "icon", type' = "image/x-icon", href = ctx.Assets.Url "/favicon.ico")
            link (rel = "stylesheet", href = ctx.Assets.Url "/css/bulletin.css")
        }

        body () { bulletinBody ctx bulletin }
    }

/// /bulletin: the latest bulletin inside the site's layout
let latest (ctx: Context) (bulletin: Bulletin option) =
    let bulletinCss = link (rel = "stylesheet", href = ctx.Assets.Url "/css/bulletin.css")

    match bulletin with
    | Some bulletin ->
        render
            ctx.Assets
            { page (longDate bulletin.Date) "/bulletin" with
                Head = [ bulletinCss ] }
            (div () {
                div () { breadcrumb [] "Bulletin" }
                bulletinBody ctx bulletin
                div () { a (href = "/bulletin/archive") { "Bulletin Archive" } }
            })
    | None ->
        render
            ctx.Assets
            (page "Bulletin" "/bulletin")
            (article () {
                h1 () { "No bulletins available" }
                p () { "Check back later for upcoming service bulletins." }
            })

/// /bulletin/archive: every bulletin, newest first
let archive (ctx: Context) (bulletins: Bulletin list) =
    render
        ctx.Assets
        (page "Bulletin Archive" "/bulletin/archive")
        (Fragment() {
            breadcrumb [ "/bulletin", "Bulletin" ] "Archive"

            section (class' = "bulletin-archive") {
                h1 () { "Bulletin Archive" }

                ul (class' = "bulletin-list") {
                    for bulletin in bulletins do
                        li () {
                            a(href = $"/bulletin/{bulletin.Slug}", target = "_blank") {
                                formatDate "MMMM d, yyyy" bulletin.Date
                            }
                        }
                }
            }
        })
