import { createBuilder } from "./.modules/aspire.js";

const builder = await createBuilder();

await builder
  .addViteApp("psdb-site", "./src/philasdb.site", { runScriptName: "dev" })
  .withPnpm()
  .withExternalHttpEndpoints();

await builder.build().run();
