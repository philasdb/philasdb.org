/**
 * Bible Reader - Local Bible API replacement for Astro projects
 * Reads from local OSIS XML file to avoid 429 API rate limit errors
 */

import { readFileSync } from "fs";
import { DOMParser } from "@xmldom/xmldom";

export interface Verse {
  book_id: string;
  book_name: string;
  chapter: number;
  verse: number;
  text: string;
}

export interface ParsedReference {
  book: string;
  chapter: number;
  startVerse: number;
  endVerse: number;
}

export interface FormattedResponse {
  reference: string;
  verses: Verse[];
  text: string;
  translation_id: string;
  translation_name: string;
  translation_note: string;
  error?: string;
}

const bookAbbreviations: Record<string, string> = {
  Gen: "Genesis",
  Exod: "Exodus",
  Lev: "Leviticus",
  Num: "Numbers",
  Deut: "Deuteronomy",
  Josh: "Joshua",
  Judg: "Judges",
  Ruth: "Ruth",
  "1Sam": "1 Samuel",
  "2Sam": "2 Samuel",
  "1Kgs": "1 Kings",
  "2Kgs": "2 Kings",
  "1Chr": "1 Chronicles",
  "2Chr": "2 Chronicles",
  Ezra: "Ezra",
  Neh: "Nehemiah",
  Esth: "Esther",
  Job: "Job",
  Ps: "Psalms",
  Prov: "Proverbs",
  Eccl: "Ecclesiastes",
  Song: "Song of Solomon",
  Isa: "Isaiah",
  Jer: "Jeremiah",
  Lam: "Lamentations",
  Ezek: "Ezekiel",
  Dan: "Daniel",
  Hos: "Hosea",
  Joel: "Joel",
  Amos: "Amos",
  Obad: "Obadiah",
  Jonah: "Jonah",
  Mic: "Micah",
  Nah: "Nahum",
  Hab: "Habakkuk",
  Zeph: "Zephaniah",
  Hag: "Haggai",
  Zech: "Zechariah",
  Mal: "Malachi",
  Matt: "Matthew",
  Mark: "Mark",
  Luke: "Luke",
  John: "John",
  Acts: "Acts",
  Rom: "Romans",
  "1Cor": "1 Corinthians",
  "2Cor": "2 Corinthians",
  Gal: "Galatians",
  Eph: "Ephesians",
  Phil: "Philippians",
  Col: "Colossians",
  "1Thess": "1 Thessalonians",
  "2Thess": "2 Thessalonians",
  "1Tim": "1 Timothy",
  "2Tim": "2 Timothy",
  Titus: "Titus",
  Phlm: "Philemon",
  Heb: "Hebrews",
  Jas: "James",
  "1Pet": "1 Peter",
  "2Pet": "2 Peter",
  "1John": "1 John",
  "2John": "2 John",
  "3John": "3 John",
  Jude: "Jude",
  Rev: "Revelation",
};

const bookNameToAbbreviation: Record<string, string> = {
  Genesis: "Gen",
  Exodus: "Exod",
  Leviticus: "Lev",
  Numbers: "Num",
  Deuteronomy: "Deut",
  Joshua: "Josh",
  Judges: "Judg",
  Ruth: "Ruth",
  "1 Samuel": "1Sam",
  "2 Samuel": "2Sam",
  "1 Kings": "1Kgs",
  "2 Kings": "2Kgs",
  "1 Chronicles": "1Chr",
  "2 Chronicles": "2Chr",
  Ezra: "Ezra",
  Nehemiah: "Neh",
  Esther: "Esth",
  Job: "Job",
  Psalms: "Ps",
  Psalm: "Ps",
  Proverbs: "Prov",
  Ecclesiastes: "Eccl",
  "Song of Solomon": "Song",
  Isaiah: "Isa",
  Jeremiah: "Jer",
  Lamentations: "Lam",
  Ezekiel: "Ezek",
  Daniel: "Dan",
  Hosea: "Hos",
  Joel: "Joel",
  Amos: "Amos",
  Obadiah: "Obad",
  Jonah: "Jonah",
  Micah: "Mic",
  Nahum: "Nah",
  Habakkuk: "Hab",
  Zephaniah: "Zeph",
  Haggai: "Hag",
  Zechariah: "Zech",
  Malachi: "Mal",
  Matthew: "Matt",
  Mark: "Mark",
  Luke: "Luke",
  John: "John",
  Acts: "Acts",
  Romans: "Rom",
  "1 Corinthians": "1Cor",
  "2 Corinthians": "2Cor",
  Galatians: "Gal",
  Ephesians: "Eph",
  Philippians: "Phil",
  Colossians: "Col",
  "1 Thessalonians": "1Thess",
  "2 Thessalonians": "2Thess",
  "1 Timothy": "1Tim",
  "2 Timothy": "2Tim",
  Titus: "Titus",
  Philemon: "Phlm",
  Hebrews: "Heb",
  James: "Jas",
  "1 Peter": "1Pet",
  "2 Peter": "2Pet",
  "1 John": "1John",
  "2 John": "2John",
  "3 John": "3John",
  Jude: "Jude",
  Revelation: "Rev",
};

class BibleReader {
  private dom: ReturnType<DOMParser["parseFromString"]>;

  constructor(osisFilePath: string) {
    const xmlContent = readFileSync(osisFilePath, "utf8");
    const parser = new DOMParser();
    this.dom = parser.parseFromString(xmlContent, "text/xml");
  }

  private cleanVerseText(text: string): string {
    return text
      .replace(/<[^>]*>/g, "")
      .replace(/\s+/g, " ")
      .trim();
  }

  getVerse(book: string, chapter: number | string, verse: number | string): Verse | null {
    try {
      const osisID = `${book}.${chapter}.${verse}`;
      const verseElements = this.dom.getElementsByTagName("verse");

      for (let i = 0; i < verseElements.length; i++) {
        const element = verseElements[i];
        if (element.getAttribute("osisID") === osisID) {
          let text = "";

          // eslint-disable-next-line @typescript-eslint/no-explicit-any -- xmldom Node types conflict with global DOM types
          let currentNode: any = element;
          while (currentNode) {
            if (currentNode.nodeType === 3) {
              text += currentNode.nodeValue;
            } else if (currentNode.nodeType === 1) {
              if (currentNode.tagName === "verse" && currentNode.getAttribute("eID")) {
                break;
              }
              text += currentNode.textContent || "";
            }
            currentNode = currentNode.nextSibling;
          }

          return {
            book_id: book,
            book_name: bookAbbreviations[book] || book,
            chapter: Number(chapter),
            verse: Number(verse),
            text: this.cleanVerseText(text),
          };
        }
      }

      return null;
    } catch (error) {
      console.error("Error getting verse:", error);
      return null;
    }
  }

  getChapter(book: string, chapter: number | string): Verse[] {
    try {
      const verses: Verse[] = [];
      const verseElements = this.dom.getElementsByTagName("verse");

      for (let i = 0; i < verseElements.length; i++) {
        const element = verseElements[i];
        const osisID = element.getAttribute("osisID");

        if (osisID && osisID.startsWith(`${book}.${chapter}.`)) {
          const verseNum = osisID.split(".")[2];
          const verse = this.getVerse(book, chapter, verseNum);
          if (verse) {
            verses.push(verse);
          }
        }
      }

      return verses.sort((a, b) => a.verse - b.verse);
    } catch (error) {
      console.error("Error getting chapter:", error);
      return [];
    }
  }

  getVerseRange(
    book: string,
    chapter: number | string,
    startVerse: number,
    endVerse: number,
  ): Verse[] {
    try {
      const verses: Verse[] = [];
      for (let v = startVerse; v <= endVerse; v++) {
        const verse = this.getVerse(book, chapter, v);
        if (verse) {
          verses.push(verse);
        }
      }
      return verses;
    } catch (error) {
      console.error("Error getting verse range:", error);
      return [];
    }
  }

  parseReference(ref: string): ParsedReference {
    const match = ref.match(
      /^([1-3]?\s*[A-Za-z]+(?:\s+[A-Za-z]+)*)\s+(\d+):(\d+)[a-z]?(?:-(\d+)[a-z]?)?$/,
    );
    if (!match) {
      throw new Error(`Invalid reference format: ${ref}`);
    }

    const [, bookName, chapter, startVerse, endVerse] = match;
    const book = this.convertBookNameToAbbreviation(bookName.trim());

    return {
      book,
      chapter: parseInt(chapter),
      startVerse: parseInt(startVerse),
      endVerse: endVerse ? parseInt(endVerse) : parseInt(startVerse),
    };
  }

  parseReferences(refString: string): ParsedReference[] {
    const refs = refString
      .split(";")
      .map((ref) => ref.trim())
      .filter((ref) => ref.length > 0);

    if (refs.length === 0) {
      throw new Error(`No valid references found in: ${refString}`);
    }

    return refs.map((ref) => this.parseReference(ref));
  }

  convertBookNameToAbbreviation(fullName: string): string {
    return bookNameToAbbreviation[fullName] || fullName;
  }

  getReference(ref: string): Verse[] {
    try {
      const parsedRefs = this.parseReferences(ref);
      const allVerses: Verse[] = [];

      for (const parsed of parsedRefs) {
        let verses: Verse[];
        if (parsed.startVerse === parsed.endVerse) {
          const verse = this.getVerse(parsed.book, parsed.chapter, parsed.startVerse);
          verses = verse ? [verse] : [];
        } else {
          verses = this.getVerseRange(
            parsed.book,
            parsed.chapter,
            parsed.startVerse,
            parsed.endVerse,
          );
        }
        allVerses.push(...verses);
      }

      return allVerses;
    } catch (error) {
      console.error("Error getting reference:", error);
      return [];
    }
  }

  getRandomVerse(): Verse | null {
    try {
      const verseElements = this.dom.getElementsByTagName("verse");
      const verseIds: string[] = [];

      for (let i = 0; i < verseElements.length; i++) {
        const osisID = verseElements[i].getAttribute("osisID");
        if (osisID && osisID.includes(".")) {
          verseIds.push(osisID);
        }
      }

      if (verseIds.length === 0) return null;

      const randomOsisID = verseIds[Math.floor(Math.random() * verseIds.length)];
      const [book, chapter, verse] = randomOsisID.split(".");

      return this.getVerse(book, chapter, verse);
    } catch (error) {
      console.error("Error getting random verse:", error);
      return null;
    }
  }

  getBooks(): { id: string; name: string }[] {
    try {
      const books: { id: string; name: string }[] = [];
      const bookElements = this.dom.getElementsByTagName("div");

      for (let i = 0; i < bookElements.length; i++) {
        const element = bookElements[i];
        if (element.getAttribute("type") === "book") {
          const osisID = element.getAttribute("osisID");
          if (osisID) {
            books.push({
              id: osisID,
              name: bookAbbreviations[osisID] || osisID,
            });
          }
        }
      }

      return books;
    } catch (error) {
      console.error("Error getting books:", error);
      return [];
    }
  }

  formatResponse(verses: Verse[], reference: string = ""): FormattedResponse {
    if (!verses || !Array.isArray(verses) || verses.length === 0) {
      return {
        reference,
        verses: [],
        text: "",
        translation_id: "KJV",
        translation_name: "King James Version",
        translation_note: "Public Domain",
        error: "Verses not found",
      };
    }

    const text = verses.map((v) => v.text).join(" ");

    return {
      reference,
      verses,
      text,
      translation_id: "KJV",
      translation_name: "King James Version",
      translation_note: "Public Domain",
    };
  }
}

export default BibleReader;
