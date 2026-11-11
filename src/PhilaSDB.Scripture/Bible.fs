/// The KJV text, loaded from an OSIS file, and lookups by reference ("Psalm 27:1-14; John 3:16").
namespace PhilaSDB.Scripture

open System
open System.Collections.Generic
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Xml

type Verse =
    { BookId: string
      BookName: string
      Chapter: int
      Verse: int
      Text: string }

/// One parsed reference: a verse or a range of verses within a chapter
type VerseRange =
    { BookId: string
      Chapter: int
      StartVerse: int
      EndVerse: int }

module Reference =
    // "1 Samuel 4:12-22", "Psalm 27:5", "John 3:16a-18b". Ranges that cross chapters aren't supported.
    let private pattern =
        Regex(@"^([1-3]?\s*[A-Za-z]+(?:\s+[A-Za-z]+)*)\s+(\d+):(\d+)[a-z]?(?:-(\d+)[a-z]?)?$", RegexOptions.CultureInvariant)

    let private parseOne (reference: string) =
        let m = pattern.Match reference

        if not m.Success then
            None
        else
            Books.tryOsisId (m.Groups[1].Value.Trim())
            |> Option.map (fun bookId ->
                let start = int m.Groups[3].Value

                { BookId = bookId
                  Chapter = int m.Groups[2].Value
                  StartVerse = start
                  EndVerse = if m.Groups[4].Success then int m.Groups[4].Value else start })

    /// Parse "A; B; C". None if any part isn't a reference we can read.
    let parse (references: string) : VerseRange list option =
        let parts =
            references.Split(';', StringSplitOptions.TrimEntries ||| StringSplitOptions.RemoveEmptyEntries)
            |> List.ofArray

        if parts.IsEmpty then
            None
        else
            let parsed = parts |> List.map parseOne

            if parsed |> List.forall Option.isSome then
                Some(parsed |> List.choose id)
            else
                None

/// Verse text keyed by OSIS id ("Gen.1.1")
type Bible private (verses: IReadOnlyDictionary<string, string>) =

    static let collapseWhitespace (text: string) =
        let sb = StringBuilder(text.Length)
        let mutable lastWasSpace = true

        for c in text do
            if Char.IsWhiteSpace c then
                if not lastWasSpace then
                    sb.Append ' ' |> ignore
                    lastWasSpace <- true
            else
                sb.Append c |> ignore
                lastWasSpace <- false

        sb.ToString().TrimEnd()

    member _.VerseCount = verses.Count

    member _.TryVerse(bookId: string, chapter: int, verse: int) =
        match verses.TryGetValue $"{bookId}.{chapter}.{verse}" with
        | true, text ->
            Some
                { BookId = bookId
                  BookName = Books.name bookId
                  Chapter = chapter
                  Verse = verse
                  Text = text }
        | false, _ -> None

    /// The verses a reference names, in order. Empty if it can't be parsed or names no verses.
    member this.Lookup(reference: string) : Verse list =
        match Reference.parse reference with
        | None -> []
        | Some ranges ->
            [ for range in ranges do
                  for v in range.StartVerse .. range.EndVerse do
                      match this.TryVerse(range.BookId, range.Chapter, v) with
                      | Some verse -> verse
                      | None -> () ]

    /// Read every verse from an OSIS file whose verses are milestones (<verse sID/> … <verse eID/>), as
    /// eBible.org publishes them. Streams the file, so it takes a fraction of a second for the whole Bible.
    static member Load(stream: Stream) =
        let verses = Dictionary<string, string>(32_000)
        let settings = XmlReaderSettings(DtdProcessing = DtdProcessing.Ignore, IgnoreComments = true)
        use reader = XmlReader.Create(stream, settings)
        let text = StringBuilder()
        let mutable current: string = null

        while reader.Read() do
            match reader.NodeType with
            | XmlNodeType.Element when reader.LocalName = "verse" ->
                match reader.GetAttribute "sID", reader.GetAttribute "eID" with
                | null, null -> ()
                | null, _ when not (isNull current) ->
                    verses[current] <- collapseWhitespace (text.ToString())
                    current <- null
                | null, _ -> ()
                | _ ->
                    current <- reader.GetAttribute "osisID"
                    text.Clear() |> ignore
            | XmlNodeType.Text
            | XmlNodeType.Whitespace
            | XmlNodeType.SignificantWhitespace when not (isNull current) -> text.Append reader.Value |> ignore
            | _ -> ()

        Bible verses

    static member Load(path: string) =
        use stream = File.OpenRead path
        Bible.Load stream
