const LIVESTREAM_PATH = "/livestream";
const POLL_INTERVAL_MS = 10_000;

class LivestreamLink extends HTMLElement {
  private intervalId?: number;
  private isLive = false;

  connectedCallback() {
    this.render();
    this.checkLiveStatus();
    this.intervalId = window.setInterval(() => this.checkLiveStatus(), POLL_INTERVAL_MS);
  }

  disconnectedCallback() {
    if (this.intervalId) {
      clearInterval(this.intervalId);
    }
  }

  private get statusUrl() {
    return this.getAttribute("url") ?? "";
  }

  private async checkLiveStatus() {
    try {
      const response = await fetch(this.statusUrl);
      const data = await response.json();
      const nextIsLive = Boolean(data?.isLive);
      if (this.isLive !== nextIsLive) {
        this.isLive = nextIsLive;
        this.render();
      }
    } catch {
      if (this.isLive) {
        this.isLive = false;
        this.render();
      }
    }
  }

  private render() {
    this.innerHTML = `
      <a href="${LIVESTREAM_PATH}">
        <span
          id="live-stream-status-icon"
          class="live-indicator${this.isLive ? " live-indicator-red" : ""}"
        >
          ${this.isLive ? "🔴" : "⚪"}
        </span>
        ${this.isLive ? "<strong>LIVESTREAM</strong>" : "<span>LIVESTREAM</span>"}
      </a>
    `;
  }
}

if (!customElements.get("livestream-link")) {
  customElements.define("livestream-link", LivestreamLink);
}

declare global {
  interface HTMLElementTagNameMap {
    "livestream-link": LivestreamLink;
  }
}
