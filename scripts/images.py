#!/usr/bin/env python3
"""Make the AVIF and WebP sizes of the photos the site shows (src/PhilaSDB.Site/wwwroot/images/sized/).

Run it after adding or replacing a photo, and commit the results:
    python3 -m pip install pillow   # once; Pillow 11.2 or later writes AVIF
    python3 scripts/images.py

The pages pick a size with srcset, so phones download the small ones. The quality settings give files about
the size the Astro site's image pipeline made (checked with Lighthouse; see docs/lighthouse.md). The site won't start if a size a page uses is missing,
and `dotnet test` checks that too.
"""
from pathlib import Path
from PIL import Image

IMAGES = Path(__file__).resolve().parent.parent / "src" / "PhilaSDB.Site" / "wwwroot" / "images"
OUT = IMAGES / "sized"

# Photo: widths to make. Keep in step with the views (Views/Pages.fs).
SIZES = {
    "cover_image.webp": [480, 768, 925],
    "pastor.webp": [180, 270],
    "cashapp.webp": [150, 256, 288],
}

FORMATS = {"avif": {"quality": 45, "speed": 2}, "webp": {"quality": 80, "method": 6}}


def main():
    OUT.mkdir(exist_ok=True)
    for name, widths in SIZES.items():
        with Image.open(IMAGES / name) as original:
            original = original.convert("RGB")
            for width in widths:
                height = round(original.height * width / original.width)
                resized = original if width == original.width else original.resize((width, height), Image.LANCZOS)
                for fmt, options in FORMATS.items():
                    target = OUT / f"{Path(name).stem}-{width}.{fmt}"
                    resized.save(target, fmt.upper(), **options)
                    print(f"{target.relative_to(IMAGES)}  {target.stat().st_size / 1024:.1f} KB")


if __name__ == "__main__":
    main()
