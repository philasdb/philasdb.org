// /sermons: recent videos from the church's YouTube channel, newest first.
// Still fetched from the browser as before; moving this to the server (and off the public key) is a later step.
const API_BASE =
  "https://content-youtube.googleapis.com/youtube/v3/search?type=video&order=date&channelId=UCtQ_XJELDHnjEolMjmIVpHw&maxResults=50&part=snippet&key=AIzaSyCGsp2PD5EuQgFkonuKQM-9ieCr2bhCY0M";
const LIVESTREAMS_URL = API_BASE + "&eventType=completed";
const UPLOADS_URL = API_BASE;

const container = document.getElementById("sermons");
const dateFormat = new Intl.DateTimeFormat("en-US", {
  year: "numeric",
  month: "long",
  day: "numeric",
  timeZone: "UTC",
});

function decodeHtml(text) {
  const textarea = document.createElement("textarea");
  textarea.innerHTML = text;
  return textarea.value;
}

function card(item) {
  const link = document.createElement("a");
  link.href = `https://www.youtube.com/watch?v=${encodeURIComponent(item.id.videoId)}`;
  link.target = "_blank";
  link.rel = "noopener noreferrer";
  link.style.cssText =
    "text-decoration: none; color: inherit; border: 1px solid #eee; border-radius: 8px; overflow: hidden; background: #fff;";

  const title = decodeHtml(item.snippet.title);
  const img = document.createElement("img");
  img.src = item.snippet.thumbnails.high.url;
  img.alt = title;
  img.loading = "lazy";
  img.style.cssText = "width: 100%; height: auto; display: block;";

  const body = document.createElement("div");
  body.style.padding = "1rem";
  const heading = document.createElement("h3");
  heading.style.cssText = "margin: 0 0 0.5rem 0; font-size: 1.1rem;";
  heading.textContent = title;
  const date = document.createElement("p");
  date.style.cssText = "margin: 0; color: #666;";
  date.textContent = dateFormat.format(new Date(item.snippet.publishedAt));
  body.append(heading, date);

  link.append(img, body);
  return link;
}

async function load() {
  let items = [];
  try {
    const responses = await Promise.all([fetch(LIVESTREAMS_URL), fetch(UPLOADS_URL)]);
    const [livestreams, uploads] = await Promise.all(responses.map((r) => r.json()));
    const seen = new Set();
    for (const item of [...(livestreams.items || []), ...(uploads.items || [])]) {
      if (!seen.has(item.id.videoId)) {
        seen.add(item.id.videoId);
        items.push(item);
      }
    }
    items.sort((a, b) => Date.parse(b.snippet.publishedAt) - Date.parse(a.snippet.publishedAt));
  } catch (error) {
    console.error("Error fetching sermons:", error);
  }

  const list = document.createElement("div");
  list.className = "sermons-list";
  list.style.cssText =
    "display: grid; gap: 2rem; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));";
  list.append(...items.map(card));
  container.replaceChildren(list);
}

void load();
