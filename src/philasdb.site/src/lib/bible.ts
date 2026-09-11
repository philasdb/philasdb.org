import BibleReader from "./bible-reader";

const bible = new BibleReader("./eng-kjv.osis.xml");

import type { Verse } from "./bible-reader";

export interface Passage {
  reference: string;
  verses: Array<Pick<Verse, "book_name" | "chapter" | "verse" | "text">>;
  text: string;
}

export async function getPassage(
  reference: string,
  _translation: string = "kjv",
): Promise<Passage | null> {
  try {
    const data = bible.getReference(reference);

    if (data.length === 0) {
      console.error(`No verses found for reference: ${reference}`);
      return null;
    }

    const formattedData = bible.formatResponse(data, reference);

    if (formattedData.verses.length === 0) {
      console.error(`Invalid formatted data for reference: ${reference}`);
      return null;
    }

    const verses = formattedData.verses.map((v) => ({
      book_name: v.book_name,
      chapter: v.chapter,
      verse: v.verse,
      text: v.text.trim(),
    }));

    return {
      reference: formattedData.reference || reference,
      verses,
      text: formattedData.text.trim(),
    };
  } catch (error) {
    console.error(`Error fetching Bible passage: ${error}`);
    return null;
  }
}
