# Track Scene JSON

The exporter writes a stable top-level package with:

- `schema`: currently `lr1tools.track-scene.v1`
- `name` and `sourceName`
- `materials`, `meshes`, `objects`, `paths`, and `gradients`
- `metadata` dictionaries for native or unknown values that do not fit the normalized contract model

Vector and quaternion data from the contract layer are serialized as JSON arrays:

- positions, normals, directions, and scales: `[x, y, z]`
- UVs: `[x, y]`
- rotations: `[x, y, z, w]`
- colors: `[r, g, b, a]`

This keeps the export schema Blender-friendly without coupling it to Blender APIs.
