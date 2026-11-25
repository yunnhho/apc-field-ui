# New images (icons, buttons, lamps)

Only when existing images cannot do the job.

1. Create SVGs in the project's `assets-src/`.
   - Naming `{btn|lamp|win|icon|logo}_{name}_{normal|hover|pressed|disabled}.svg` (lamps and logos have no state).
   - `<svg width="..">` is required. Model them on the existing SVGs in `/c/apc-field-ui/assets-src/`.
2. If `/c/apc-field-ui/tools/node_modules` is missing, run `cd /c/apc-field-ui/tools && npm install` once. Then:
   `node /c/apc-field-ui/tools/render-assets.mjs --out <project>/themes/<customer>/assets --override <project>/assets-src`
3. Images replacing an existing name are picked up by the controls automatically. New names: `ApcTheme.GetImage("icon_xxx_normal")`.

Never edit the repository's `/c/apc-field-ui/assets-src/*.svg` or `/c/apc-field-ui/themes/default`.
