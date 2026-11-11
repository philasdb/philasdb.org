/// Bulletins: the JSON format (v1 and v2), reading it, and migrating v1 to v2.
module PhilaSDB.Site.Bulletins

open System
open System.Globalization
open System.IO
open System.Text.Json

type Song =
    { Title: string option
      Hymnal: string option
      /// Looks the hymn up in this hymnal (a code from hymnals.json), like a plain hymn number does
      HymnalCode: string option
      HymnNumber: string option
      Artist: string option
      AdditionalInfo: string option
      Url: string option }

/// A song is a hymn number (in the default hymnal) or written out
type SongRef =
    | Hymn of int
    | Custom of Song

type Link = { Href: string; Name: string }

type Announcement =
    { StartDate: DateOnly
      EndDate: DateOnly option
      Time: string option
      Description: string
      AdditionalInfo: string option
      Location: string option
      Link: Link option }

type SabbathSchool =
    { Topic: string
      Study: string
      Background: string
      Devotional: string }

type Message = { Title: string; Speaker: string }

type Communion =
    { Description: string option
      Songs: SongRef list }

type WorshipItem =
    | WorshipLeader of name: string
    | ThoughtOfTheDay of reference: string * presentedBy: string option
    | SongItem of label: string * song: SongRef * presentedBy: string option
    | Text of label: string * content: string option * italic: bool * small: bool * presentedBy: string option
    | Scripture of label: string * reference: string * showPassage: bool * presentedBy: string option
    | MessageItem of Message
    | CommunionItem of Communion
    | CustomItem of label: string * content: string option * song: SongRef option * presentedBy: string option
    | TithesAndOffering of song: SongRef * presentedBy: string option
    | Youth

/// A bulletin in the current (v2) shape; v1 files are migrated when they're read
type Bulletin =
    { Date: DateOnly
      SabbathSchool: SabbathSchool
      Announcements: Announcement list
      Notes: string list
      WorshipService: WorshipItem list }

    /// "2026-11-07": the bulletin's URL segment
    member this.Slug = this.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)

// ─── Reading JSON ──────────────────────────────────────────────────────────
// JsonDocument rather than reflection-based deserialization, which isn't trim/AOT safe and doesn't fit F#
// unions. JavaScript's truthiness decided what the Astro site showed, so empty strings count as missing.

exception BulletinFormatException of string

let private fail message = raise (BulletinFormatException message)

let private tryProp (name: string) (el: JsonElement) =
    match el.ValueKind with
    | JsonValueKind.Object ->
        match el.TryGetProperty name with
        | true, value when value.ValueKind <> JsonValueKind.Null -> Some value
        | _ -> None
    | _ -> None

let private optString name el =
    tryProp name el
    |> Option.bind (fun v ->
        match v.ValueKind with
        | JsonValueKind.String -> Option.ofObj (v.GetString())
        | JsonValueKind.Number -> Some(v.GetRawText())
        | _ -> None)
    |> Option.filter (fun s -> s <> "")

let private reqString name el =
    match optString name el with
    | Some s -> s
    | None -> fail $"missing \"{name}\""

let private optBool name el =
    match tryProp name el with
    | Some v when v.ValueKind = JsonValueKind.True -> true
    | _ -> false

let private parseDate (text: string) =
    match DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None) with
    | true, date -> date
    | false, _ -> fail $"\"{text}\" is not a date (yyyy-MM-dd)"

let private songRef (el: JsonElement) =
    match el.ValueKind with
    | JsonValueKind.Number -> Hymn(el.GetInt32())
    // A bare string has been used for a song title
    | JsonValueKind.String ->
        Custom
            { Title = Option.ofObj (el.GetString())
              Hymnal = None
              HymnalCode = None
              HymnNumber = None
              Artist = None
              AdditionalInfo = None
              Url = None }
    | JsonValueKind.Object ->
        Custom
            { Title = optString "title" el
              Hymnal = optString "hymnal" el
              HymnalCode = optString "hymnalCode" el
              HymnNumber = optString "hymnNumber" el
              Artist = optString "artist" el
              AdditionalInfo = optString "additionalInfo" el
              Url = optString "url" el }
    | kind -> fail $"a song must be a hymn number or an object, not {kind}"

let private optSong name el =
    tryProp name el
    |> Option.filter (fun v -> not (v.ValueKind = JsonValueKind.Number && v.GetInt32() = 0))
    |> Option.map songRef

let private list name (read: JsonElement -> 'T) el =
    match tryProp name el with
    | Some v when v.ValueKind = JsonValueKind.Array -> [ for item in v.EnumerateArray() -> read item ]
    | _ -> []

let private announcement el =
    { StartDate = parseDate (reqString "startDate" el)
      EndDate = optString "endDate" el |> Option.map parseDate
      Time = optString "time" el
      Description = reqString "description" el
      AdditionalInfo = optString "additionalInfo" el
      Location = optString "location" el
      Link =
        tryProp "link" el
        |> Option.map (fun l ->
            { Href = reqString "href" l
              Name = reqString "name" l }) }

let private message el =
    { Title = optString "title" el |> Option.defaultValue ""
      Speaker = optString "speaker" el |> Option.defaultValue "" }

let private communion el =
    { Description = optString "description" el
      Songs = list "songs" songRef el }

let private worshipItem (el: JsonElement) =
    let presentedBy = optString "presentedBy" el

    match reqString "type" el with
    | "worshipLeader" -> WorshipLeader(reqString "name" el)
    | "thoughtOfTheDay" -> ThoughtOfTheDay(reqString "reference" el, presentedBy)
    | "song" ->
        match optSong "song" el with
        | Some song -> SongItem(optString "label" el |> Option.defaultValue "", song, presentedBy)
        | None -> fail "a song item needs a \"song\""
    | "text" ->
        Text(
            optString "label" el |> Option.defaultValue "",
            optString "content" el,
            optBool "italic" el,
            optBool "small" el,
            presentedBy
        )
    | "scripture" ->
        Scripture(
            optString "label" el |> Option.defaultValue "",
            reqString "reference" el,
            optBool "showPassage" el,
            presentedBy
        )
    | "message" -> MessageItem(message (tryProp "message" el |> Option.defaultWith (fun () -> fail "missing \"message\"")))
    | "communion" ->
        CommunionItem(communion (tryProp "communion" el |> Option.defaultWith (fun () -> fail "missing \"communion\"")))
    | "custom" -> CustomItem(reqString "label" el, optString "content" el, optSong "song" el, presentedBy)
    | "tithesAndOffering" ->
        match optSong "song" el with
        | Some song -> TithesAndOffering(song, presentedBy)
        | None -> fail "a tithesAndOffering item needs a \"song\""
    | "youth" -> Youth
    | other -> fail $"unknown worship item type \"{other}\""

/// The v1 format had fixed fields; this builds the same order of service the Astro site's migration did
let private migrateV1 (el: JsonElement) =
    let tithesSong = optSong "tithesAndOfferingSong" el
    let specialMusic = optSong "specialMusic" el

    [ match optString "worshipLeader" el with
      | Some name -> WorshipLeader name
      | None -> ()
      match optString "thoughtOfTheDay" el with
      | Some reference -> ThoughtOfTheDay(reference, None)
      | None -> ()
      match optSong "openingSong" el with
      | Some song -> SongItem("Opening Song", song, None)
      | None -> ()
      Text("Welcome of New Visitors", None, false, false, None)
      Text("Opening Prayer", Some "*Prayer Requests*", true, true, None)
      match optString "responsiveReading" el with
      | Some reference -> Scripture("Responsive Reading", reference, false, None)
      | None -> ()
      match tithesSong with
      | Some song -> TithesAndOffering(song, None)
      | None -> ()
      if optBool "youth" el then
          Youth
      match specialMusic, tithesSong with
      | Some song, None ->
          Text("Special Music", None, false, false, None)
          SongItem("Special Music", song, None)
      | _ -> ()
      match tryProp "message" el with
      | Some m -> MessageItem(message m)
      | None -> ()
      match tryProp "communion" el with
      | Some c -> CommunionItem(communion c)
      | None -> ()
      match optString "itemAfterMessage" el with
      | Some label -> CustomItem(label, None, optSong "itemAfterMessageSong" el, None)
      | None -> ()
      match optSong "closingSong" el with
      | Some song -> SongItem("Closing Song", song, None)
      | None -> ()
      Text("Closing Prayer & Benediction", None, false, false, None) ]

let parse (json: string) : Bulletin =
    use doc = JsonDocument.Parse(json, JsonDocumentOptions(AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip))
    let root = doc.RootElement

    let version =
        match tryProp "version" root with
        | Some v when v.ValueKind = JsonValueKind.Number -> v.GetInt32()
        | _ -> 1

    let worshipService =
        match version with
        | 1 -> migrateV1 root
        | 2 ->
            let items = list "worshipService" worshipItem root
            // Some v2 files still have v1's top-level "youth" flag
            if optBool "youth" root then items @ [ Youth ] else items
        | other -> fail $"unknown bulletin version {other}"

    let sabbathSchool =
        tryProp "sabbathSchool" root
        |> Option.defaultWith (fun () -> fail "missing \"sabbathSchool\"")

    { Date = parseDate (reqString "date" root)
      SabbathSchool =
        { Topic = optString "topic" sabbathSchool |> Option.defaultValue ""
          Study = optString "study" sabbathSchool |> Option.defaultValue ""
          Background = optString "background" sabbathSchool |> Option.defaultValue ""
          Devotional = optString "devotional" sabbathSchool |> Option.defaultValue "" }
      Announcements = list "announcements" announcement root
      Notes =
        list "notes" (fun n -> if n.ValueKind = JsonValueKind.String then n.GetString() else n.GetRawText()) root
      WorshipService = worshipService }

/// Every *.json file in a folder, newest first. A file that can't be read stops startup with its name.
let loadFolder (folder: string) : Bulletin list =
    Directory.GetFiles(folder, "*.json")
    |> Array.map (fun path ->
        try
            parse (File.ReadAllText path)
        with
        | BulletinFormatException message -> failwith $"{Path.GetFileName path}: {message}"
        | :? JsonException as e -> failwith $"{Path.GetFileName path}: {e.Message}")
    |> Array.sortByDescending _.Date
    |> List.ofArray
