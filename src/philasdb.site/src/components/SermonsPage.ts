import { Temporal } from "@js-temporal/polyfill";
import { LitElement, html } from "lit";
import { customElement, state } from "lit/decorators.js";

const YOUTUBE_API_BASE =
  "https://content-youtube.googleapis.com/youtube/v3/search?type=video&order=date&channelId=UCtQ_XJELDHnjEolMjmIVpHw&maxResults=50&part=snippet&key=AIzaSyCGsp2PD5EuQgFkonuKQM-9ieCr2bhCY0M";
const YOUTUBE_LIVESTREAMS_URL = YOUTUBE_API_BASE + "&eventType=completed";
const YOUTUBE_UPLOADS_URL = YOUTUBE_API_BASE;

interface SermonItem {
  id: { videoId: string };
  snippet: {
    title: string;
    publishedAt: string;
    thumbnails: {
      high: { url: string };
    };
  };
}

@customElement("sermons-page")
export class SermonsPage extends LitElement {
  @state()
  private sermons: SermonItem[] = [];

  @state()
  private loading = true;

  // Use light DOM to allow global styles (PicoCSS) to apply
  createRenderRoot() {
    return this;
  }

  connectedCallback() {
    super.connectedCallback();
    void this.fetchSermons();
  }

  private async fetchSermons() {
    try {
      const [livestreamsRes, uploadsRes] = await Promise.all([
        fetch(YOUTUBE_LIVESTREAMS_URL),
        fetch(YOUTUBE_UPLOADS_URL),
      ]);
      const [livestreamsData, uploadsData] = await Promise.all([
        livestreamsRes.json(),
        uploadsRes.json(),
      ]);
      const seen = new Set<string>();
      const combined: SermonItem[] = [];
      for (const item of [...(livestreamsData.items || []), ...(uploadsData.items || [])]) {
        if (!seen.has(item.id.videoId)) {
          seen.add(item.id.videoId);
          combined.push(item);
        }
      }
      combined.sort((a, b) =>
        Temporal.Instant.compare(
          Temporal.Instant.from(b.snippet.publishedAt),
          Temporal.Instant.from(a.snippet.publishedAt),
        ),
      );
      this.sermons = combined;
    } catch (error) {
      console.error("Error fetching sermons:", error);
    } finally {
      this.loading = false;
    }
  }

  private decodeHtml(text: string): string {
    const textarea = document.createElement("textarea");
    textarea.innerHTML = text;
    return textarea.value;
  }

  private formatDate(dateString: string): string {
    return Temporal.Instant.from(dateString).toZonedDateTimeISO("UTC").toLocaleString("en-US", {
      year: "numeric",
      month: "long",
      day: "numeric",
    });
  }

  render() {
    if (this.loading) {
      return html`<div>Loading sermons...</div>`;
    }

    return html`
      <div
        class="sermons-list"
        style="display: grid; gap: 2rem; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));"
      >
        ${this.sermons.map(
          (item) => html`
            <a
              href="https://www.youtube.com/watch?v=${item.id.videoId}"
              target="_blank"
              rel="noopener noreferrer"
              style="text-decoration: none; color: inherit; border: 1px solid #eee; border-radius: 8px; overflow: hidden; background: #fff;"
            >
              <img
                src="${item.snippet.thumbnails.high.url}"
                alt="${item.snippet.title}"
                style="width: 100%; height: auto; display: block;"
              />
              <div style="padding: 1rem;">
                <h3 style="margin: 0 0 0.5rem 0; font-size: 1.1rem;">
                  ${this.decodeHtml(item.snippet.title)}
                </h3>
                <p style="margin: 0; color: #666;">${this.formatDate(item.snippet.publishedAt)}</p>
              </div>
            </a>
          `,
        )}
      </div>
    `;
  }
}

declare global {
  interface HTMLElementTagNameMap {
    "sermons-page": SermonsPage;
  }
}
