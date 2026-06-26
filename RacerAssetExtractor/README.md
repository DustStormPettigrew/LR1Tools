# LR1 Racer Asset Extractor

Exports native GDB models as LR1Tools scene JSON, resolvable material textures as PNG sidecars,
and all PARTDB character images: hats, bodies, legs, default faces, and snapshot-face variants.

Run:

`dotnet run --project LR1Tools\RacerAssetExtractor\LR1RacerAssetExtractor.csproj -- "D:\Games\LEGO Racers" "D:\Games\LEGO Racers\RacerEditorAssets"`

The output `racer-assets.json` also includes CSET brick rosters and their authored color palette names.
