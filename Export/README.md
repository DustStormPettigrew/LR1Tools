# Track Scene JSON

The exporter writes a stable top-level package with:

- `schema`: currently `lr1tools.track-scene.v2`
- `exportType`: `Scene`, `Mesh`, `ObjectSet`, `PathSet`, `MaterialSet`, or `AssetBundle`
- root identity and provenance fields: `id`, `sourceId`, `name`, `sourceName`, `sourceFormat`, `sourcePath`, and optional `sourceIndex`
- root coordinate metadata: `coordinateSystem`, `handedness`, `rightAxis`, `upAxis`, `forwardAxis`, and `units`
- `materials`, `materialAnimations`, `meshes`, `objects`, `paths`, and `gradients`
- per-item identity and provenance fields where available: `id`, `sourceId`, `sourceName`, `sourceFormat`, `sourcePath`, and optional `sourceIndex`
- `metadata` dictionaries for native or unknown values that do not fit the normalized contract model

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
