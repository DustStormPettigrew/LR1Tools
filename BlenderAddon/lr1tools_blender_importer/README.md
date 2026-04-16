# LR1Tools Blender Importer

Install the `lr1tools_blender_importer` folder as a standard Blender addon.

Supported package schema:

- `lr1tools.track-scene.v2`

Supported export types:

- `Scene`
- `Mesh`
- `ObjectSet`
- `PathSet`
- `MaterialSet`
- `AssetBundle`

The addon keeps LR1Tools JSON parsing separate from Blender object creation:

- `loader.py`: file IO and schema entry
- `schema.py`: tool-neutral JSON parsing and validation
- `conversion.py`: centralized coordinate and transform conversion
- `builder.py`: Blender collections, meshes, curves, empties, and custom properties
