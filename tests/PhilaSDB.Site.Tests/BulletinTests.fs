module PhilaSDB.Site.Tests.BulletinTests

open System
open System.IO
open Xunit
open PhilaSDB.Site.Bulletins

[<Fact>]
let ``every bulletin in the repo reads`` () =
    let files = Directory.GetFiles(Path.Combine(Paths.content, "bulletins"), "*.json")
    let bulletins = loadFolder (Path.Combine(Paths.content, "bulletins"))
    Assert.Equal(files.Length, bulletins.Length)
    // Newest first
    Assert.Equal<DateOnly list>(bulletins |> List.map _.Date |> List.sortDescending, bulletins |> List.map _.Date)

[<Fact>]
let ``a v1 bulletin is migrated to the v2 order of service`` () =
    let bulletin =
        parse
            """{
  "date": "2025-03-29",
  "sabbathSchool": { "topic": "T", "study": "Leviticus 16:11-19", "background": "Leviticus 16", "devotional": "Psalm 40:9-17" },
  "announcements": [ { "description": "Youth Sabbath", "startDate": "2025-03-29" } ],
  "notes": [ "*Note*" ],
  "thoughtOfTheDay": "Genesis 2:2-3",
  "openingSong": 2,
  "responsiveReading": "Psalm 19",
  "tithesAndOfferingSong": 37,
  "youth": true,
  "message": { "title": "Seven Days of Creation", "speaker": "Sister Angel Plange" },
  "closingSong": { "title": "Custom", "artist": "Someone" }
}"""

    let expected =
        [ ThoughtOfTheDay("Genesis 2:2-3", None)
          SongItem("Opening Song", Hymn 2, None)
          Text("Welcome of New Visitors", None, false, false, None)
          Text("Opening Prayer", Some "*Prayer Requests*", true, true, None)
          Scripture("Responsive Reading", "Psalm 19", false, None)
          TithesAndOffering(Hymn 37, None)
          Youth
          MessageItem
              { Title = "Seven Days of Creation"
                Speaker = "Sister Angel Plange" }
          SongItem(
              "Closing Song",
              Custom
                  { Title = Some "Custom"
                    Hymnal = None
                    HymnalCode = None
                    HymnNumber = None
                    Artist = Some "Someone"
                    AdditionalInfo = None
                    Url = None },
              None
          )
          Text("Closing Prayer & Benediction", None, false, false, None) ]

    Assert.Equal<WorshipItem list>(expected, bulletin.WorshipService)
    Assert.Equal(DateOnly(2025, 3, 29), bulletin.Date)
    Assert.Equal("2025-03-29", bulletin.Slug)

[<Fact>]
let ``v1 special music without a tithes song gets its own items`` () =
    let bulletin =
        parse
            """{ "date": "2025-01-04", "sabbathSchool": {}, "announcements": [], "notes": [], "specialMusic": 5 }"""

    Assert.Contains(SongItem("Special Music", Hymn 5, None), bulletin.WorshipService)

[<Fact>]
let ``a v2 bulletin keeps its order and the old youth flag adds the youth item`` () =
    let bulletin =
        parse
            """{ "version": 2, "date": "2026-01-03", "sabbathSchool": {}, "announcements": [], "notes": [], "youth": true,
  "worshipService": [ { "type": "worshipLeader", "name": "A" }, { "type": "scripture", "label": "Reading", "reference": "John 3:16", "showPassage": true } ] }"""

    Assert.Equal<WorshipItem list>(
        [ WorshipLeader "A"; Scripture("Reading", "John 3:16", true, None); Youth ],
        bulletin.WorshipService
    )

[<Fact>]
let ``a bad file names the problem`` () =
    let error =
        Assert.Throws<BulletinFormatException>(fun () ->
            parse """{ "version": 2, "date": "2026-13-01", "sabbathSchool": {} }""" |> ignore)

    Assert.Contains("2026-13-01", error.Data0)
