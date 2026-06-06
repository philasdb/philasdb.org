import { Temporal } from "@js-temporal/polyfill";

const DATE_FORMATTERS: Record<string, Intl.DateTimeFormatOptions> = {
  "MMMM dd, yyyy": { month: "long", day: "2-digit", year: "numeric" },
  "MMMM d, yyyy": { month: "long", day: "numeric", year: "numeric" },
  "MMMM d": { month: "long", day: "numeric" },
  d: { day: "numeric" },
};

export function compareDateStrings(left: string, right: string): number {
  return Temporal.PlainDate.compare(Temporal.PlainDate.from(left), Temporal.PlainDate.from(right));
}

export default function formatDate(dateString: string, formatString: string): string {
  const options = DATE_FORMATTERS[formatString];

  if (!options) {
    throw new Error(`Unsupported date format: ${formatString}`);
  }

  return Temporal.PlainDate.from(dateString).toLocaleString("en-US", options);
}
