import { defineCollection, z } from "astro:content";
import { glob, file } from "astro/loaders";

const bulletins = defineCollection({
  loader: glob({ pattern: "**/*.json", base: "./src/data/bulletins" }),
});

export const collections = { bulletins };
