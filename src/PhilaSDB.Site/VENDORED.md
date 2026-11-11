# Vendored scripts

| File | Source | Version | License |
|---|---|---|---|
| `wwwroot/css/pico.min.css` | `css/pico.min.css` from the [Pico CSS](https://picocss.com) npm tarball (`https://registry.npmjs.org/@picocss/pico/-/pico-2.1.1.tgz`); same file the site loaded from jsDelivr (SRI `sha384-L1dWfspMTHU/ApYnFiMz2QID/PlP1xCW9visvBdbEkOLkSSWsP6ZJWhPw6apiXxU`) | 2.1.1 | MIT |
| `wwwroot/js/hls.light.min.js` | `dist/hls.light.min.js` from the [hls.js](https://github.com/video-dev/hls.js) npm tarball (`https://registry.npmjs.org/hls.js/-/hls.js-1.7.3.tgz`) | 1.7.3 | Apache-2.0 |

hls.js is only loaded on `/livestream`, and only once a stream is live. Browsers without Media Source Extensions
(older iPhones) play the stream natively instead. To update it, download the tarball for the new version, check it, and replace the file.
