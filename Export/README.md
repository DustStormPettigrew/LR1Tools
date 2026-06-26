# Track Scene JSON

The exporter writes a stable top-level package with:

- `schema`: currently `lr1tools.track-scene.v2`
- `exportType`: `Scene`, `Mesh`, `ObjectSet`, `PathSet`, `MaterialSet`, or `AssetBundle`
- root identity and provenance fields: `id`, `sourceId`, `name`, `sourceName`, `sourceFormat`, `sourcePath`, and optional `sourceIndex`
- root coordinate metadata: `coordinateSystem`, `handedness`, `rightAxis`, `upAxis`, `forwardAxis`, and `units`
- `materials`, `textures`, `materialAnimations`, `meshes`, `objects`, `paths`, and `gradients`
- per-item identity and provenance fields where available: `id`, `sourceId`, `sourceName`, `sourceFormat`, `sourcePath`, and optional `sourceIndex`
- `metadata` dictionaries for native or unknown values that do not fit the normalized contract model

Scene objects may also include optional external animation reference fields:

- `animationRef`: external object/skeletal animation package identifier where a direct native reference is known
- `materialAnimationRef`: external material animation package identifier where ownership is unambiguous
- `animationSourceName` and `animationSourcePath`: optional provenance for the referenced external animation asset

These fields are additive and remain optional. When LR1Tools cannot prove the exact linkage, candidate values stay in object `metadata` instead of being normalized into the explicit reference fields.

Materials may also include structured texture references:

- `textureRef` and `alphaTextureRef` preserve resolved source paths and optional exported image paths
- root-level `textures` records carry decoded texture metadata such as `width`, `height`, `format`, `hasAlpha`, and `paletteColorCount` when known
- the original native source path remains preserved even if decoded image export fails

Vector and quaternion data from the contract layer are serialized as JSON arrays:

- positions, normals, directions, and scales: `[x, y, z]`
- UVs: `[x, y]`
- rotations: `[x, y, z, w]`
- colors: `[r, g, b, a]`

The coordinate system is exported explicitly and remains tool-neutral:

- handedness: `RightHanded`
- axes: `+X` right, `+Z` up, `-Y` forward
- units: native LR1 world units

The schema is intended for reuse by any downstream importer, not a specific DCC or engine.

Texture export behavior:

- LR BMP metadata is read through `LibLR1.BMP`
- decoded image output is optional and opt-in for the tester export flow
- when enabled, decoded images are written next to the JSON as `<SceneName>_Textures/*.png`
- set `LR1TOOLS_EXPORT_TEXTURE_IMAGES=1` or `ExportTextureImages: true` in `localGameFiles.json` to enable sidecar PNG export
- `MDB`/`TDB` material references are used to resolve texture names against the current track folder, nearby `COMMON` folders, installation-level `COMMON`, and optional explicit `TexturePaths` from tester configuration or explicit `.BMP`/`.TGA` inputs
- missing source textures or unsupported source image formats are recorded in metadata instead of failing the whole scene export

# Track Animation JSON

The animation exporter writes a separate top-level package with:

- `schema`: `lr1tools.animation.v1`
- `exportType`: `AnimationSet`
- root identity and provenance fields: `id`, `sourceId`, `name`, `sourceName`, `sourceFormat`, `sourcePath`, and optional `sourceIndex`
- root coordinate metadata: `coordinateSystem`, `handedness`, `rightAxis`, `upAxis`, `forwardAxis`, and `units`
- `clips`, each with generic `channels`, `target`, and `keyframes`
- `metadata` dictionaries at every level for native fields, unresolved semantics, and raw summaries that do not fit the normalized contract directly

The animation contract is intentionally tool-neutral:

- no Blender-specific types or API assumptions
- channel targets are generic (`type`, `name`, `path`, `slot`)
- keyframes can carry `frameIndex`, `time`, scalar values, string values, `vector2Value`, `vector3Value`, or `quaternionValue`

`MAB` export behavior:

- exports material animation clips and frame-swap sequences where the native data is available
- preserves raw native fields such as animation offsets, animation lengths, frame counts, speed, material frame indices, and variable material targets in `metadata`
- leaves loop behavior as `Unknown` when the source format does not define enough semantics to normalize it safely

`ADB` export behavior:

- exports per-animation clip records from the parsed `ADB` metadata dictionary
- derives channel/keyframe slices only when pointer-table and sample-array ranges are valid
- preserves pointer offsets, lengths, initial position/quaternion, and other native fields in `metadata`
- keeps interpolation and higher-level target semantics conservative when the format meaning is not yet fully understood

The animation export is separate from track scene export. Scene/world placement data can continue to use native `WDB`/`RAB` references such as `ADB` and `MAB` names to associate separately exported animation assets downstream.
