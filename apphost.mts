import { createBuilder } from "./.aspire/modules/aspire.mjs";

const builder = await createBuilder();

await builder
  .addViteApp("psdb-site", "./src/philasdb.site", { runScriptName: "dev" })
  .withPnpm()
  .withExternalHttpEndpoints();

await builder.build().run();
