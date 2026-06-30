import { createStore, defineSlice } from "@videojs/store";
import Hls from "hls.js";
import { LitElement, html } from "lit";
import { customElement, property } from "lit/decorators.js";

const DEFAULT_STATUS_URL = "https://psdb-livestream.fly.dev/api/status";
const DEFAULT_HLS_URL = "https://psdb-livestream.fly.dev/hls/live/demo/output.m3u8";
const STREAM_CHECK_INTERVAL_MS = 5000;

const livestreamSlice = defineSlice<HTMLVideoElement>()({
  state: ({ set, target }) => ({
    isChecking: true,
    isLive: false,
    sourceLoaded: false,
    hasPlaybackError: false,
    paused: true,
    readyState: 0,
    errorCode: null as number | null,

    setChecking(isChecking: boolean) {
      set({ isChecking });
    },

    setAvailability(isLive: boolean) {
      set({ isChecking: false, isLive });
    },

    setSourceLoaded(sourceLoaded: boolean) {
      set({ sourceLoaded });
    },

    setPlaybackError(hasPlaybackError: boolean) {
      set({ hasPlaybackError });
    },

    syncFromMedia() {
      const media = target();
      set({
        paused: media.paused,
        readyState: media.readyState,
        errorCode: media.error?.code ?? null,
      });
    },

    clearSource() {
      const media = target();
      media.pause();
      media.removeAttribute("src");
      media.load();

      set({
        sourceLoaded: false,
        hasPlaybackError: false,
        paused: media.paused,
        readyState: media.readyState,
        errorCode: null,
      });
    },

    setNativeSource(src: string) {
      const media = target();
      media.src = src;
      media.load();

      set({
        sourceLoaded: true,
        hasPlaybackError: false,
        readyState: media.readyState,
        errorCode: null,
      });
    },

    async autoplayMuted() {
      const media = target();
      media.muted = true;
      media.autoplay = true;
      media.playsInline = true;

      try {
        await media.play();
      } catch (error) {
        console.warn("Autoplay was prevented:", error);
      }

      set({
        paused: media.paused,
        readyState: media.readyState,
      });
    },
  }),

  attach({ target, signal, set }) {
    const sync = () =>
      set({
        paused: target.paused,
        readyState: target.readyState,
        errorCode: target.error?.code ?? null,
      });

    const onError = () =>
      set({
        sourceLoaded: false,
        hasPlaybackError: true,
        paused: target.paused,
        readyState: target.readyState,
        errorCode: target.error?.code ?? null,
      });

    sync();

    target.addEventListener("loadedmetadata", sync, { signal });
    target.addEventListener("canplay", sync, { signal });
    target.addEventListener("play", sync, { signal });
    target.addEventListener("pause", sync, { signal });
    target.addEventListener("playing", sync, { signal });
    target.addEventListener("emptied", sync, { signal });
    target.addEventListener("error", onError, { signal });
  },
});

@customElement("livestream-player")
export class LivestreamPlayer extends LitElement {
  @property({ type: String })
  statusUrl = DEFAULT_STATUS_URL;

  @property({ type: String })
  hlsUrl = DEFAULT_HLS_URL;

  @property({ type: String, attribute: "poster-src" })
  posterSrc = "";

  private readonly store = createStore<HTMLVideoElement>()(livestreamSlice);

  private hasCompletedInitialCheck = false;
  private statusIntervalId?: number;
  private detachStore?: () => void;
  private unsubscribeStore?: () => void;
  private hls?: Hls;

  createRenderRoot() {
    return this;
  }

  firstUpdated() {
    const video = this.videoElement;

    if (!video) {
      return;
    }

    this.detachStore = this.store.attach(video);
    this.unsubscribeStore = this.store.subscribe(() => this.requestUpdate());

    void this.refreshStreamState();
    this.statusIntervalId = window.setInterval(() => {
      void this.refreshStreamState();
    }, STREAM_CHECK_INTERVAL_MS);
  }

  disconnectedCallback() {
    super.disconnectedCallback();
    this.cleanupStatusCheck();
    this.cleanupHls();
    this.unsubscribeStore?.();
    this.detachStore?.();
  }

  private get videoElement(): HTMLVideoElement | null {
    return this.querySelector("video");
  }

  private cleanupStatusCheck() {
    if (this.statusIntervalId !== undefined) {
      window.clearInterval(this.statusIntervalId);
      this.statusIntervalId = undefined;
    }
  }

  private cleanupHls() {
    this.hls?.destroy();
    this.hls = undefined;
  }

  private async refreshStreamState() {
    if (!this.hasCompletedInitialCheck) {
      this.store.setChecking(true);
    }

    try {
      const response = await fetch(this.statusUrl);
      const data = (await response.json()) as { isLive?: boolean };
      const isLive = data.isLive === true;

      this.store.setAvailability(isLive);
      this.hasCompletedInitialCheck = true;

      if (isLive) {
        await this.ensurePlayback();
      } else {
        this.stopPlayback();
      }
    } catch (error) {
      console.error("Error checking stream status:", error);
      this.store.setAvailability(false);
      this.hasCompletedInitialCheck = true;
      this.stopPlayback();
    }
  }

  private async ensurePlayback() {
    const video = this.videoElement;

    if (!video) {
      return;
    }

    if (this.store.sourceLoaded && !this.store.hasPlaybackError) {
      await this.store.autoplayMuted();
      return;
    }

    this.cleanupHls();

    if (
      video.canPlayType("application/vnd.apple.mpegurl") ||
      video.canPlayType("application/x-mpegURL")
    ) {
      this.store.setNativeSource(this.hlsUrl);
      await this.store.autoplayMuted();
      return;
    }

    if (!Hls.isSupported()) {
      this.store.setPlaybackError(true);
      return;
    }

    const hls = new Hls({
      enableWorker: true,
      lowLatencyMode: true,
    });

    this.hls = hls;
    this.store.setPlaybackError(false);
    this.store.setSourceLoaded(false);

    hls.on(Hls.Events.MEDIA_ATTACHED, () => {
      hls.loadSource(this.hlsUrl);
    });

    hls.on(Hls.Events.MANIFEST_PARSED, async () => {
      this.store.setSourceLoaded(true);
      this.store.setPlaybackError(false);
      await this.store.autoplayMuted();
    });

    hls.on(Hls.Events.ERROR, (_event, data) => {
      console.error("HLS playback error:", data);

      if (data.fatal) {
        this.store.setPlaybackError(true);
        this.store.setSourceLoaded(false);
        this.cleanupHls();
      }
    });

    hls.attachMedia(video);
  }

  private stopPlayback() {
    this.cleanupHls();

    if (!this.videoElement) {
      return;
    }

    this.store.clearSource();
    this.store.syncFromMedia();
  }

  private get overlayHeading() {
    if (this.store.hasPlaybackError) {
      return "Livestream Unavailable";
    }

    if (this.store.isLive && !this.store.sourceLoaded) {
      return "Connecting to Livestream";
    }

    return "Livestream Currently Offline";
  }

  private get overlayMessage() {
    if (this.store.hasPlaybackError) {
      return "We detected a playback problem and will keep checking for the livestream.";
    }

    if (this.store.isChecking) {
      return "Checking for livestream...";
    }

    if (this.store.isLive && !this.store.sourceLoaded) {
      return "Preparing the live video stream now.";
    }

    return "We will automatically reconnect when the livestream begins.";
  }

  render() {
    const showOverlay =
      !this.store.isLive || !this.store.sourceLoaded || this.store.hasPlaybackError;

    return html`
      <section style="position: relative;">
        <div
          style=${`position: absolute; top: 0; left: 0; width: 100%; height: 100%; display: ${
            showOverlay ? "flex" : "none"
          }; z-index: 10; background: rgba(0, 0, 0, 0.85); border-radius: 8px;`}
        >
          <div
            style="display: flex; flex-direction: column; align-items: center; justify-content: center; height: 100%; width: 100%; padding: 2rem; text-align: center; color: white;"
          >
            <h2 style="margin-bottom: 1rem; color: white;">${this.overlayHeading}</h2>
            <p style="font-size: 1.2rem; margin: 0.5rem 0; color: white; font-weight: 500;">
              Join us live on <strong style="color: white;">Saturdays at 11:00 AM EST</strong>
            </p>
            <p style="margin: 0.5rem 0; color: white; opacity: 0.95;">
              Sabbath School, Praise & Worship, and Divine Worship
            </p>
            <p style="margin: 1.5rem 0;">
              <a
                href="/sermons"
                role="button"
                style="background-color: #0066cc; color: white; font-weight: 600;"
                >Watch Previous Sermons</a
              >
            </p>
            <p style="font-size: 0.85rem; opacity: 0.7; margin-top: 1rem;">
              ${this.overlayMessage}
            </p>
          </div>
        </div>

        <video
          style="position: relative; width: 100%; height: auto; aspect-ratio: 16/9;"
          controls
          preload="auto"
          playsinline
          poster=${this.posterSrc}
        >
          Your browser does not support HTML5 video.
        </video>
      </section>
    `;
  }
}

declare global {
  interface HTMLElementTagNameMap {
    "livestream-player": LivestreamPlayer;
  }
}
