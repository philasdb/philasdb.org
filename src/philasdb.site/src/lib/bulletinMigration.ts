import type { Bulletin, BulletinV1, BulletinV2, WorshipItem } from "./types";

/**
 * Migrates a bulletin from v1 format to v2 format.
 * If already v2, returns as-is.
 */
export function migrateBulletinToV2(bulletin: Bulletin): BulletinV2 {
  // Already v2, but check if it has the old youth field
  if (bulletin.version === 2) {
    const v2 = bulletin as BulletinV2;
    // If it has youth, add it to worshipService and remove
    if ("youth" in v2 && v2.youth) {
      const newV2 = { ...v2 };
      delete (newV2 as any).youth;
      newV2.worshipService = [...newV2.worshipService, { type: "youth" }];
      return newV2;
    }
    return v2;
  }

  // Cast to v1 for migration
  const v1 = bulletin as BulletinV1;
  const worshipService: WorshipItem[] = [];

  // Build worship service array in the typical order
  if (v1.worshipLeader) {
    worshipService.push({
      type: "worshipLeader",
      name: v1.worshipLeader,
    });
  }

  if (v1.thoughtOfTheDay) {
    worshipService.push({
      type: "thoughtOfTheDay",
      reference: v1.thoughtOfTheDay,
    });
  }

  if (v1.openingSong) {
    worshipService.push({
      type: "song",
      label: "Opening Song",
      song: v1.openingSong,
    });
  }

  // Welcome of New Visitors (always present)
  worshipService.push({
    type: "text",
    label: "Welcome of New Visitors",
  });

  // Prayer Requests and Opening Prayer
  worshipService.push({
    type: "text",
    label: "Opening Prayer",
    content: "*Prayer Requests*",
    italic: true,
    small: true,
  });

  if (v1.responsiveReading) {
    worshipService.push({
      type: "scripture",
      label: "Responsive Reading",
      reference: v1.responsiveReading,
    });
  }

  // Tithes and Offering section
  if (v1.tithesAndOfferingSong) {
    worshipService.push({
      type: "tithesAndOffering",
      song: v1.tithesAndOfferingSong,
      scriptureReading: v1.scriptureReading,
      specialMusic: v1.specialMusic,
    });
  }

  // Youth section
  if (v1.youth) {
    worshipService.push({
      type: "youth",
    });
  }

  // Special Music (if not already included in tithes and offering)
  if (v1.specialMusic && !v1.tithesAndOfferingSong) {
    worshipService.push({
      type: "text",
      label: "Special Music",
    });
    worshipService.push({
      type: "song",
      label: "Special Music",
      song: v1.specialMusic,
    });
  }

  // Message
  if (v1.message) {
    worshipService.push({
      type: "message",
      message: v1.message,
    });
  }

  // Communion
  if (v1.communion) {
    worshipService.push({
      type: "communion",
      communion: v1.communion,
    });
  }

  // Item after message (custom content)
  if (v1.itemAfterMessage) {
    worshipService.push({
      type: "custom",
      label: v1.itemAfterMessage,
      song: v1.itemAfterMessageSong,
    });
  }

  // Closing song
  if (v1.closingSong) {
    worshipService.push({
      type: "song",
      label: "Closing Song",
      song: v1.closingSong,
    });
  }

  // Closing Prayer & Benediction (always present)
  worshipService.push({
    type: "text",
    label: "Closing Prayer & Benediction",
  });

  return {
    version: 2,
    date: v1.date,
    sabbathSchool: v1.sabbathSchool,
    announcements: v1.announcements,
    notes: v1.notes,
    worshipService,
  };
}
