module PhilaSDB.Site.Tests.ScriptureTests

open Xunit
open PhilaSDB.Scripture

let private bible = lazy (Bible.Load Paths.kjv)

[<Fact>]
let ``parses a verse range`` () =
    let expected =
        [ { BookId = "Ps"
            Chapter = 27
            StartVerse = 1
            EndVerse = 14 } ]

    Assert.Equal(Some expected, Reference.parse "Psalm 27:1-14")

[<Fact>]
let ``parses several references and numbered books`` () =
    let parsed = Reference.parse "1 Samuel 4:12; John 3:16-17"
    Assert.Equal(Some [ "1Sam", 4, 12, 12; "John", 3, 16, 17 ], parsed |> Option.map (List.map (fun r -> r.BookId, r.Chapter, r.StartVerse, r.EndVerse)))

[<Theory>]
[<InlineData "1 Samuel 4:2-7:2">] // across chapters
[<InlineData "Psalm 100">] // a whole chapter
[<InlineData "Isaiah 6:1-8;38:1-5">] // the second part has no book
[<InlineData "Hezekiah 1:1">] // not a book
let ``references it can't resolve give nothing`` (reference: string) =
    Assert.Empty(bible.Value.Lookup reference)

[<Fact>]
let ``loads every verse with its text`` () =
    // 31,102 canonical verses plus the Apocrypha this edition includes
    Assert.True(bible.Value.VerseCount > 31_000)

    let verse = bible.Value.Lookup "Psalm 27:5" |> List.exactlyOne
    Assert.Equal("Psalms", verse.BookName)

    Assert.Equal(
        "For in the time of trouble he shall hide me in his pavilion: in the secret of his tabernacle shall he hide me; he shall set me up upon a rock.",
        verse.Text
    )

[<Fact>]
let ``keeps words joined across markup`` () =
    // "is" is a <transChange> (a word the translators added) between two text nodes
    let verse = bible.Value.Lookup "Psalm 27:1" |> List.exactlyOne
    Assert.StartsWith("The LORD is my light and my salvation;", verse.Text)

[<Fact>]
let ``a range returns the verses in order`` () =
    let verses = bible.Value.Lookup "John 3:16-18"
    Assert.Equal<int list>([ 16; 17; 18 ], verses |> List.map _.Verse)
