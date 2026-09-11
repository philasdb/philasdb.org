export interface Announcement {
  startDate: string;
  endDate?: string;
  time?: string;
  description: string;
  additionalInfo?: string;
  location?: string;
  link?: { href: string; name: string };
}

export interface SabbathSchoolLesson {
  topic: string;
  study: string;
  background: string;
  devotional: string;
}

export interface Song {
  title: string;
  hymnal?: string;
  hymnNumber?: string;
  artist?: string;
  additionalInfo?: string;
  url?: string;
}

export interface Message {
  title: string;
  speaker: string;
}

export interface Communion {
  name: string;
  description: string;
  songs: number[];
}

// Worship item types for flexible service order (v2)
export type WorshipItem =
  | { type: "worshipLeader"; name: string }
  | { type: "thoughtOfTheDay"; reference: string; presentedBy?: string }
  | { type: "song"; label: string; song: number | Song; presentedBy?: string }
  | {
      type: "text";
      label: string;
      content?: string;
      italic?: boolean;
      small?: boolean;
      presentedBy?: string;
    }
  | {
      type: "scripture";
      label: string;
      reference: string;
      showPassage?: boolean;
      presentedBy?: string;
    }
  | { type: "message"; message: Message }
  | { type: "communion"; communion: Communion }
  | { type: "custom"; label: string; content?: string; song?: number | Song; presentedBy?: string }
  | {
      type: "tithesAndOffering";
      song: number | Song;
      scriptureReading?: string;
      specialMusic?: number | Song;
      presentedBy?: string;
    }
  | { type: "youth" };

// Legacy bulletin format (v1)
export interface BulletinV1 {
  version?: 1;
  date: string;
  sabbathSchool: SabbathSchoolLesson;
  announcements: Announcement[];
  notes: string[];
  worshipLeader?: string;
  thoughtOfTheDay: string;
  openingSong: number | Song;
  responsiveReading: string;
  tithesAndOfferingSong: number | Song;
  scriptureReading?: string;
  specialMusic?: number | Song;
  message?: Message;
  communion?: Communion;
  closingSong?: number | Song;
  youth?: boolean;
  itemAfterMessage?: string;
  itemAfterMessageSong?: number | Song;
}

// New flexible bulletin format (v2)
export interface BulletinV2 {
  version: 2;
  date: string;
  sabbathSchool: SabbathSchoolLesson;
  announcements: Announcement[];
  notes: string[];
  worshipService: WorshipItem[];
}

export type Bulletin = BulletinV1 | BulletinV2;
