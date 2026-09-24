# THIRD-PARTY REFERENCE ART — DO NOT SHIP

Sprites and the LilitaOne font in this folder were extracted from an AssetRipper export of **PixelFlow**
(`~/Downloads/PixelFlow/ExportedProject`). They are here only so the UI can be laid out against the mock-ups
while Percas art is produced. We do not own them.

- `ThirdPartyArtBuildGuard` fails every **release** build that references anything under `_ThirdPartyReference/`
  (development builds are allowed, for internal testing).
- The prefabs pick art through `UiSkin` (Game/Editor). Delete this folder, run
  **NinetyNine/Setup/Rebuild UI Prefabs**, and every prefab falls back to the generated placeholder art;
  or drop Percas art in with the same file names.
- `Sprites/sprites.json` keeps each sprite's 9-slice border; `ReferenceArtImport` applies it on import.
