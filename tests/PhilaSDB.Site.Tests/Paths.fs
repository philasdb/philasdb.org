/// The real content, read from the source tree so the tests check what ships
module PhilaSDB.Site.Tests.Paths

open System.IO

let private repo = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", ".."))
let site = Path.Combine(repo, "src", "PhilaSDB.Site")
let content = Path.Combine(site, "content")
let webRoot = Path.Combine(site, "wwwroot")
let kjv = Path.Combine(repo, "data", "eng-kjv.osis.xml")
