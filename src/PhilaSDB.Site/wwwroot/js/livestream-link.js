// Header link to /livestream that shows a red dot while the stream is live.
// Polls the livestream server's status; this moves to server-sent events (Datastar) in a later step.
const POLL_INTERVAL_MS = 10_000;

class LivestreamLink extends HTMLElement {
  #intervalId;
  #isLive = null;

  connectedCallback() {
    this.#render();
    void this.#check();
    this.#intervalId = setInterval(() => void this.#check(), POLL_INTERVAL_MS);
  }

  disconnectedCallback() {
    clearInterval(this.#intervalId);
  }

  async #check() {
    const url = this.getAttribute("url");
    let isLive = false;
    if (url) {
      try {
        const response = await fetch(url);
        const data = await response.json();
        isLive = Boolean(data?.isLive);
      } catch {
        isLive = false;
      }
    }
    if (this.#isLive !== isLive) {
      this.#isLive = isLive;
      this.#render();
    }
  }

  #render() {
    let icon = this.querySelector(".live-indicator");
    let label = this.querySelector(".live-label");
    if (!icon || !label) {
      const link = document.createElement("a");
      link.href = "/livestream";
      icon = document.createElement("span");
      icon.id = "live-stream-status-icon";
      icon.className = "live-indicator";
      label = document.createElement("span");
      label.className = "live-label";
      label.textContent = "LIVESTREAM";
      link.append(icon, " ", label);
      this.replaceChildren(link);
    }
    const live = this.#isLive === true;
    icon.textContent = live ? "🔴" : "⚪";
    icon.classList.toggle("live-indicator-red", live);
    label.classList.toggle("live-label-live", live);
  }
}

if (!customElements.get("livestream-link")) {
  customElements.define("livestream-link", LivestreamLink);
}
