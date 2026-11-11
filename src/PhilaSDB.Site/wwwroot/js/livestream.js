// /livestream: show the offline overlay until the stream is live, then play it.
// hls.js is loaded on demand, only once the stream is live.
// Polls the livestream server's status; this moves to server-sent events (Datastar) in a later step.
const POLL_INTERVAL_MS = 5_000;

const root = document.getElementById("livestream");
const statusUrl = root.dataset.statusUrl;
const hlsUrl = root.dataset.hlsUrl;
const hlsScript = root.dataset.hlsScript;
const overlay = document.getElementById("offline-overlay");
const video = document.getElementById("livestream-video");

let started = false;

async function isLive() {
  try {
    const response = await fetch(statusUrl);
    const data = await response.json();
    return data.isLive === true;
  } catch (error) {
    console.error("Error checking stream status:", error);
    return false;
  }
}

function loadScript(src) {
  return new Promise((resolve, reject) => {
    const script = document.createElement("script");
    script.src = src;
    script.onload = resolve;
    script.onerror = reject;
    document.head.append(script);
  });
}

async function startPlayer() {
  started = true;
  video.muted = true;
  video.addEventListener("error", () => {
    console.log("Player error detected");
    overlay.style.display = "flex";
  });

  // hls.js wherever Media Source Extensions exist (including Safari and iPadOS). Only browsers without them,
  // such as older iPhones, use native HLS: some Chromium builds claim native HLS support but can't play it.
  const hasMse = "MediaSource" in window || "ManagedMediaSource" in window;
  if (hasMse) {
    await loadScript(hlsScript);
  }
  if (hasMse && window.Hls?.isSupported()) {
    const hls = new window.Hls({ liveDurationInfinity: true });
    hls.on(window.Hls.Events.ERROR, (_event, data) => {
      if (data.fatal) {
        console.log("Player error detected");
        overlay.style.display = "flex";
      }
    });
    hls.loadSource(hlsUrl);
    hls.attachMedia(video);
  } else if (video.canPlayType("application/vnd.apple.mpegurl")) {
    video.src = hlsUrl;
  } else {
    console.error("This browser can't play HLS");
    return;
  }

  video.play().catch(() => {
    // Autoplay can be refused; the controls are there to start it
  });
}

async function update() {
  if (await isLive()) {
    overlay.style.display = "none";
    if (!started) {
      await startPlayer();
    }
  } else {
    overlay.style.display = "flex";
  }
}

void update();
setInterval(() => void update(), POLL_INTERVAL_MS);
