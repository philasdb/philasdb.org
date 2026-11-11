/// Hymnals (hymnals.json): looks up hymn titles and hymnary.org links by number.
module PhilaSDB.Site.Hymnals

open System.Collections.Generic
open System.IO
open System.Text.Json

type Hymn = { Number: int; Title: string; Url: string }

type Hymnal =
    { Code: string
      Name: string
      Hymns: IReadOnlyDictionary<int, Hymn> }

/// Bulletins give a bare number for hymns from this hymnal
let defaultCode = "HHOF1980"

type Hymnals(hymnals: Hymnal list) =
    let byCode = hymnals |> List.map (fun h -> h.Code, h) |> dict

    member _.TryFind(code: string) =
        match byCode.TryGetValue code with
        | true, hymnal -> Some hymnal
        | false, _ -> None

    member this.TryHymn(code: string, number: int) =
        this.TryFind code
        |> Option.bind (fun hymnal ->
            match hymnal.Hymns.TryGetValue number with
            | true, hymn -> Some(hymnal, hymn)
            | false, _ -> None)

let load (path: string) =
    use doc = JsonDocument.Parse(File.ReadAllText path)

    Hymnals
        [ for hymnal in doc.RootElement.EnumerateArray() do
              { Code = hymnal.GetProperty("Code").GetString()
                Name = hymnal.GetProperty("Name").GetString()
                Hymns =
                  readOnlyDict
                      [ for hymn in hymnal.GetProperty("Hymns").EnumerateArray() do
                            let number = hymn.GetProperty("Number").GetInt32()

                            number,
                            { Number = number
                              Title = hymn.GetProperty("Title").GetString()
                              Url = hymn.GetProperty("Url").GetString() } ] } ]
