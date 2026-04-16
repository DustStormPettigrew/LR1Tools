import json
from pathlib import Path
from typing import Dict, Iterable, List

import bpy

from .conversion import CoordinateConverter
from .schema import ScenePackage, TrackMaterialData, TrackMeshData, TrackObjectData, TrackPathData


SECTION_LABELS = {
	"meshes": "Meshes",
	"objects": "Objects",
	"start_positions": "StartPositions",
	"checkpoints": "Checkpoints",
	"powerups": "Powerups",
	"hazards": "Hazards",
	"emitters": "Emitters",
	"paths": "Paths",
	"npc_paths": "NpcPaths",
}


def import_scene_package(context, package: ScenePackage, filepath: str) -> str:
	converter = CoordinateConverter(package.coordinate)
	root_name = package.name or Path(filepath).stem or "LR1ToolsImport"
	root_collection = bpy.data.collections.new(root_name)
	context.scene.collection.children.link(root_collection)
	_apply_package_properties(root_collection, package, filepath)

	section_collections = _create_section_collections(root_collection, package)
	materials_by_name = _create_materials(package.materials)
	meshes_by_name = _create_mesh_datablocks(package.meshes, materials_by_name, converter)

	created_objects = _create_track_objects(
		section_collections["objects"],
		package.objects,
		meshes_by_name,
		materials_by_name,
		converter)

	created_mesh_objects = 0
	if package.meshes and not package.objects:
		created_mesh_objects = _create_mesh_asset_objects(
			section_collections["meshes"],
			package.meshes,
			meshes_by_name)

	created_markers = 0
	created_markers += _create_marker_objects(section_collections["start_positions"], package.start_positions, converter, "ARROWS")
	created_markers += _create_marker_objects(section_collections["checkpoints"], package.checkpoints, converter, "CUBE")
	created_markers += _create_marker_objects(section_collections["powerups"], package.powerups, converter, "SPHERE")
	created_markers += _create_marker_objects(section_collections["hazards"], package.hazards, converter, "CUBE")
	created_markers += _create_marker_objects(section_collections["emitters"], package.emitters, converter, "PLAIN_AXES")

	created_paths = _create_curve_objects(section_collections["paths"], package.paths, converter)
	created_paths += _create_curve_objects(section_collections["npc_paths"], package.npc_paths, converter)

	return (
		"Imported {0} ({1}): {2} mesh datablock(s), {3} object instance(s), "
		"{4} mesh asset object(s), {5} marker empty(ies), {6} curve object(s), "
		"{7} material(s).".format(
			root_collection.name,
			package.export_type,
			len(package.meshes),
			created_objects,
			created_mesh_objects,
			created_markers,
			created_paths,
			len(package.materials)))


def _create_section_collections(root_collection, package: ScenePackage):
	required_sections: List[str] = []
	if package.export_type == "Scene":
		required_sections = list(SECTION_LABELS.keys())
	else:
		if package.meshes:
			required_sections.append("meshes")
		if package.objects:
			required_sections.append("objects")
		if package.start_positions:
			required_sections.append("start_positions")
		if package.checkpoints:
			required_sections.append("checkpoints")
		if package.powerups:
			required_sections.append("powerups")
		if package.hazards:
			required_sections.append("hazards")
		if package.emitters:
			required_sections.append("emitters")
		if package.paths:
			required_sections.append("paths")
		if package.npc_paths:
			required_sections.append("npc_paths")

	collections = {}
	for key in SECTION_LABELS:
		if package.export_type != "Scene" and key not in required_sections:
			collections[key] = root_collection
			continue

		collections[key] = _create_child_collection(root_collection, SECTION_LABELS[key])

	return collections


def _create_child_collection(parent_collection, name: str):
	collection = bpy.data.collections.new(name)
	parent_collection.children.link(collection)
	return collection


def _create_materials(materials: Iterable[TrackMaterialData]) -> Dict[str, bpy.types.Material]:
	output: Dict[str, bpy.types.Material] = {}
	for material in materials:
		if not material.name:
			continue

		bl_material = bpy.data.materials.get(material.name)
		if bl_material is None:
			bl_material = bpy.data.materials.new(material.name)

		bl_material.use_nodes = False
		bl_material.diffuse_color = material.diffuse_color
		_apply_item_properties(bl_material, material)
		bl_material["lr1tools_texture_name"] = material.texture_name or ""
		bl_material["lr1tools_alpha_texture_name"] = material.alpha_texture_name or ""
		bl_material["lr1tools_opacity"] = float(material.opacity)
		bl_material["lr1tools_double_sided"] = bool(material.double_sided)
		output[material.name] = bl_material

	return output


def _create_mesh_datablocks(
	meshes: Iterable[TrackMeshData],
	materials_by_name: Dict[str, bpy.types.Material],
	converter: CoordinateConverter) -> Dict[str, bpy.types.Mesh]:
	output: Dict[str, bpy.types.Mesh] = {}
	for mesh_data in meshes:
		if not mesh_data.name:
			continue

		vertices = [tuple(converter.convert_position(vertex.position)) for vertex in mesh_data.vertices]
		faces = []
		for index in range(0, len(mesh_data.indices) - 2, 3):
			faces.append((
				mesh_data.indices[index],
				mesh_data.indices[index + 1],
				mesh_data.indices[index + 2]))

		mesh = bpy.data.meshes.new(mesh_data.name)
		mesh.from_pydata(vertices, [], faces)
		mesh.update()
		_apply_item_properties(mesh, mesh_data)
		mesh["lr1tools_is_collision_mesh"] = bool(mesh_data.is_collision_mesh)

		if mesh_data.material_name and mesh_data.material_name in materials_by_name and not mesh.materials:
			mesh.materials.append(materials_by_name[mesh_data.material_name])

		output[mesh_data.name] = mesh

	return output


def _create_track_objects(
	collection,
	objects: Iterable[TrackObjectData],
	meshes_by_name: Dict[str, bpy.types.Mesh],
	materials_by_name: Dict[str, bpy.types.Material],
	converter: CoordinateConverter) -> int:
	count = 0
	for object_data in objects:
		bl_object = _create_scene_object(object_data, meshes_by_name, materials_by_name, converter)
		collection.objects.link(bl_object)
		count += 1
	return count


def _create_scene_object(
	object_data: TrackObjectData,
	meshes_by_name: Dict[str, bpy.types.Mesh],
	materials_by_name: Dict[str, bpy.types.Material],
	converter: CoordinateConverter):
	mesh_data = meshes_by_name.get(object_data.mesh_name or "")

	if mesh_data is not None:
		bl_object = bpy.data.objects.new(object_data.name or mesh_data.name, mesh_data)
		if object_data.material_name and object_data.material_name in materials_by_name and not mesh_data.materials:
			mesh_data.materials.append(materials_by_name[object_data.material_name])
	else:
		bl_object = bpy.data.objects.new(object_data.name or "LR1Object", None)
		bl_object.empty_display_type = "PLAIN_AXES"
		bl_object.empty_display_size = 0.5

	_apply_object_transform(bl_object, object_data, converter)
	_apply_item_properties(bl_object, object_data)
	bl_object["lr1tools_mesh_name"] = object_data.mesh_name or ""
	bl_object["lr1tools_material_name"] = object_data.material_name or ""
	bl_object["lr1tools_path_name"] = object_data.path_name or ""
	bl_object.hide_viewport = not object_data.visible
	bl_object.hide_render = not object_data.visible
	return bl_object


def _create_mesh_asset_objects(collection, meshes: Iterable[TrackMeshData], meshes_by_name: Dict[str, bpy.types.Mesh]) -> int:
	count = 0
	for mesh_data in meshes:
		mesh = meshes_by_name.get(mesh_data.name)
		if mesh is None:
			continue

		bl_object = bpy.data.objects.new(mesh_data.name, mesh)
		_apply_item_properties(bl_object, mesh_data)
		collection.objects.link(bl_object)
		count += 1

	return count


def _create_marker_objects(collection, markers: Iterable[TrackObjectData], converter: CoordinateConverter, display_type: str) -> int:
	count = 0
	for marker in markers:
		bl_object = bpy.data.objects.new(marker.name or "LR1Marker", None)
		bl_object.empty_display_type = display_type
		bl_object.empty_display_size = 0.5
		_apply_object_transform(bl_object, marker, converter)
		_apply_item_properties(bl_object, marker)
		bl_object["lr1tools_marker_type"] = marker.metadata.get("NativeType", "")
		collection.objects.link(bl_object)
		count += 1
	return count


def _create_curve_objects(collection, paths: Iterable[TrackPathData], converter: CoordinateConverter) -> int:
	count = 0
	for path in paths:
		curve_data = bpy.data.curves.new(path.name or "LR1Path", type="CURVE")
		curve_data.dimensions = "3D"
		spline = curve_data.splines.new("POLY")
		node_count = max(len(path.nodes), 1)
		spline.points.add(node_count - 1)

		for index, node in enumerate(path.nodes):
			position = converter.convert_position(node.position)
			spline.points[index].co = (position.x, position.y, position.z, 1.0)

		spline.use_cyclic_u = bool(path.closed)
		curve_object = bpy.data.objects.new(path.name or curve_data.name, curve_data)
		_apply_item_properties(curve_object, path)
		curve_object["lr1tools_node_count"] = len(path.nodes)
		collection.objects.link(curve_object)
		count += 1
	return count


def _apply_object_transform(bl_object, object_data: TrackObjectData, converter: CoordinateConverter):
	transform = converter.convert_transform(object_data.transform)
	bl_object.location = transform["location"]
	bl_object.rotation_mode = "QUATERNION"
	bl_object.rotation_quaternion = _resolve_object_rotation(object_data, converter, transform["rotation"])
	bl_object.scale = transform["scale"]


def _resolve_object_rotation(object_data: TrackObjectData, converter: CoordinateConverter, fallback_rotation: bpy.types.Quaternion):
	forward, up = _get_forward_up_vectors(object_data)
	if forward is not None and up is not None:
		return converter.convert_forward_up_rotation(forward, up)
	return fallback_rotation


def _get_forward_up_vectors(object_data: TrackObjectData):
	metadata = object_data.metadata or {}
	forward = _parse_vector3_string(metadata.get("RotationForward"))
	up = _parse_vector3_string(metadata.get("RotationUp"))
	if forward is not None and up is not None:
		return forward, up

	forward = _parse_vector3_string(metadata.get("OrientationForward"))
	up = _parse_vector3_string(metadata.get("OrientationUp"))
	if forward is not None and up is not None:
		return forward, up

	return None, None


def _parse_vector3_string(value: str):
	if not value:
		return None

	parts = [part.strip() for part in value.split(",")]
	if len(parts) != 3:
		return None

	try:
		return tuple(float(part) for part in parts)
	except ValueError:
		return None


def _apply_package_properties(collection, package: ScenePackage, filepath: str):
	collection["lr1tools_schema"] = package.schema
	collection["lr1tools_export_type"] = package.export_type
	collection["lr1tools_id"] = package.id or package.name or ""
	collection["lr1tools_source_id"] = package.source_id or ""
	collection["lr1tools_source_name"] = package.source_name or ""
	collection["lr1tools_source_format"] = package.source_format or ""
	collection["lr1tools_source_path"] = package.source_path or filepath or ""
	collection["lr1tools_source_index"] = -1 if package.source_index is None else int(package.source_index)
	collection["lr1tools_coordinate_system"] = json.dumps({
		"handedness": package.coordinate.handedness,
		"rightAxis": package.coordinate.right_axis,
		"upAxis": package.coordinate.up_axis,
		"forwardAxis": package.coordinate.forward_axis,
		"units": package.coordinate.units,
	}, sort_keys=True)
	collection["lr1tools_metadata_json"] = json.dumps(package.metadata, sort_keys=True)


def _apply_item_properties(id_block, item):
	metadata = getattr(item, "metadata", {}) or {}
	id_block["lr1tools_id"] = getattr(item, "id", None) or getattr(item, "name", "") or ""
	id_block["lr1tools_source_id"] = getattr(item, "source_id", None) or ""
	id_block["lr1tools_source_name"] = getattr(item, "source_name", None) or ""
	id_block["lr1tools_source_format"] = getattr(item, "source_format", None) or ""
	id_block["lr1tools_source_path"] = getattr(item, "source_path", None) or ""
	id_block["lr1tools_source_index"] = -1 if getattr(item, "source_index", None) is None else int(item.source_index)
	id_block["lr1tools_metadata_json"] = json.dumps(metadata, sort_keys=True)
