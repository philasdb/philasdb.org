const LIVESTREAM_PATH = "/livestream";
const POLL_INTERVAL_MS = 10_000;

class LivestreamLink extends HTMLElement {
  private intervalId?: number;
  private isLive: boolean | null = null;
  private icon?: HTMLSpanElement;
  private label?: HTMLElement;

  connectedCallback() {
    this.ensureInitialized();
    this.updateDisplay();
    void this.checkLiveStatus();
    this.intervalId = window.setInterval(() => void this.checkLiveStatus(), POLL_INTERVAL_MS);
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
    const statusUrl = this.statusUrl;
    if (!statusUrl) {
      if (this.isLive !== false) {
        this.isLive = false;
        this.updateDisplay();
      }
      return;
    }

    try {
      const response = await fetch(statusUrl);
      const data = await response.json();
      const nextIsLive = Boolean(data?.isLive);
      if (this.isLive !== nextIsLive) {
        this.isLive = nextIsLive;
        this.updateDisplay();
      }
    } catch {
      if (this.isLive !== false) {
        this.isLive = false;
        this.updateDisplay();
      }
    }
  }

  private ensureInitialized() {
    if (!this.icon || !this.label) {
      const link = document.createElement("a");
      link.href = LIVESTREAM_PATH;

      const icon = document.createElement("span");
      icon.id = "live-stream-status-icon";
      icon.className = "live-indicator";
      icon.textContent = "⚪";

      const label = document.createElement("span");
      label.className = "live-label";
      label.textContent = "LIVESTREAM";

      link.append(icon, " ", label);
      this.replaceChildren(link);

      this.icon = icon;
      this.label = label;
    }
  }

  private updateDisplay() {
    if (!this.icon || !this.label) {
      return;
    }

    if (this.isLive === true) {
      this.icon.textContent = "🔴";
      this.icon.classList.add("live-indicator-red");
      this.label.classList.add("live-label-live");
      return;
    }

    this.icon.textContent = "⚪";
    this.icon.classList.remove("live-indicator-red");
    this.label.classList.remove("live-label-live");
  }
}

if (!customElements.get("livestream-link")) {
  customElements.define("livestream-link", LivestreamLink);
}

export {};

declare global {
  interface HTMLElementTagNameMap {
    "livestream-link": LivestreamLink;
  }
}
