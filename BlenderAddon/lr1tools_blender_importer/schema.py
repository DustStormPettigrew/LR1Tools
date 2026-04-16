from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional, Sequence, Tuple


SCHEMA_ID = "lr1tools.track-scene.v2"
VALID_EXPORT_TYPES = {
	"Scene",
	"Mesh",
	"ObjectSet",
	"PathSet",
	"MaterialSet",
	"AssetBundle",
}


class SchemaValidationError(ValueError):
	pass


@dataclass
class CoordinateMetadata:
	handedness: str = "RightHanded"
	right_axis: str = "+X"
	up_axis: str = "+Z"
	forward_axis: str = "-Y"
	units: str = "NativeLR1"


@dataclass
class TrackTransformData:
	position: Tuple[float, float, float] = (0.0, 0.0, 0.0)
	rotation: Tuple[float, float, float, float] = (0.0, 0.0, 0.0, 1.0)
	scale: Tuple[float, float, float] = (1.0, 1.0, 1.0)


@dataclass
class TrackGradientStopData:
	position: float = 0.0
	color: Tuple[float, float, float, float] = (1.0, 1.0, 1.0, 1.0)
	metadata: Dict[str, str] = field(default_factory=dict)


@dataclass
class TrackGradientData:
	id: Optional[str] = None
	source_id: Optional[str] = None
	name: str = ""
	source_name: Optional[str] = None
	source_format: Optional[str] = None
	source_path: Optional[str] = None
	source_index: Optional[int] = None
	stops: List[TrackGradientStopData] = field(default_factory=list)
	metadata: Dict[str, str] = field(default_factory=dict)


@dataclass
class TrackMaterialData:
	id: Optional[str] = None
	source_id: Optional[str] = None
	name: str = ""
	source_name: Optional[str] = None
	source_format: Optional[str] = None
	source_path: Optional[str] = None
	source_index: Optional[int] = None
	texture_name: str = ""
	alpha_texture_name: str = ""
	diffuse_color: Tuple[float, float, float, float] = (1.0, 1.0, 1.0, 1.0)
	opacity: float = 1.0
	double_sided: bool = False
	gradients: List[TrackGradientData] = field(default_factory=list)
	metadata: Dict[str, str] = field(default_factory=dict)


@dataclass
class TrackVertexData:
	position: Tuple[float, float, float] = (0.0, 0.0, 0.0)
	normal: Tuple[float, float, float] = (0.0, 0.0, 0.0)
	primary_tex_coord: Tuple[float, float] = (0.0, 0.0)
	color: Tuple[float, float, float, float] = (1.0, 1.0, 1.0, 1.0)
	metadata: Dict[str, str] = field(default_factory=dict)


@dataclass
class TrackMeshData:
	id: Optional[str] = None
	source_id: Optional[str] = None
	name: str = ""
	source_name: Optional[str] = None
	source_format: Optional[str] = None
	source_path: Optional[str] = None
	source_index: Optional[int] = None
	material_name: str = ""
	is_collision_mesh: bool = False
	vertices: List[TrackVertexData] = field(default_factory=list)
	indices: List[int] = field(default_factory=list)
	metadata: Dict[str, str] = field(default_factory=dict)


@dataclass
class TrackObjectData:
	id: Optional[str] = None
	source_id: Optional[str] = None
	name: str = ""
	source_name: Optional[str] = None
	source_format: Optional[str] = None
	source_path: Optional[str] = None
	source_index: Optional[int] = None
	mesh_name: str = ""
	material_name: str = ""
	path_name: str = ""
	visible: bool = True
	transform: TrackTransformData = field(default_factory=TrackTransformData)
	metadata: Dict[str, str] = field(default_factory=dict)


@dataclass
class TrackPathNodeData:
	position: Tuple[float, float, float] = (0.0, 0.0, 0.0)
	forward: Tuple[float, float, float] = (0.0, 0.0, 1.0)
	up: Tuple[float, float, float] = (0.0, 1.0, 0.0)
	width: float = 0.0
	metadata: Dict[str, str] = field(default_factory=dict)


@dataclass
class TrackPathData:
	id: Optional[str] = None
	source_id: Optional[str] = None
	name: str = ""
	source_name: Optional[str] = None
	source_format: Optional[str] = None
	source_path: Optional[str] = None
	source_index: Optional[int] = None
	closed: bool = False
	nodes: List[TrackPathNodeData] = field(default_factory=list)
	metadata: Dict[str, str] = field(default_factory=dict)


@dataclass
class ScenePackage:
	schema: str
	export_type: str
	id: Optional[str] = None
	source_id: Optional[str] = None
	name: str = ""
	source_name: Optional[str] = None
	source_format: Optional[str] = None
	source_path: Optional[str] = None
	source_index: Optional[int] = None
	coordinate: CoordinateMetadata = field(default_factory=CoordinateMetadata)
	metadata: Dict[str, str] = field(default_factory=dict)
	materials: List[TrackMaterialData] = field(default_factory=list)
	meshes: List[TrackMeshData] = field(default_factory=list)
	objects: List[TrackObjectData] = field(default_factory=list)
	start_positions: List[TrackObjectData] = field(default_factory=list)
	checkpoints: List[TrackObjectData] = field(default_factory=list)
	powerups: List[TrackObjectData] = field(default_factory=list)
	hazards: List[TrackObjectData] = field(default_factory=list)
	emitters: List[TrackObjectData] = field(default_factory=list)
	paths: List[TrackPathData] = field(default_factory=list)
	npc_paths: List[TrackPathData] = field(default_factory=list)
	gradients: List[TrackGradientData] = field(default_factory=list)
	filepath: Optional[str] = None


def parse_scene_package(payload: Dict[str, Any], filepath: Optional[str] = None) -> ScenePackage:
	schema = _get_string(payload, "schema")
	if schema != SCHEMA_ID:
		raise SchemaValidationError(
			"Unsupported schema '{0}'. Expected '{1}'.".format(schema, SCHEMA_ID))

	export_type = _get_string(payload, "exportType")
	if export_type not in VALID_EXPORT_TYPES:
		raise SchemaValidationError(
			"Unsupported exportType '{0}'.".format(export_type))

	coordinate_payload = payload.get("coordinateSystem")
	coordinate = CoordinateMetadata(
		handedness=_coalesce_string(
			payload.get("handedness"),
			_dict_string(coordinate_payload, "handedness"),
			"RightHanded"),
		right_axis=_coalesce_string(
			payload.get("rightAxis"),
			_dict_string(coordinate_payload, "rightAxis"),
			"+X"),
		up_axis=_coalesce_string(
			payload.get("upAxis"),
			_dict_string(coordinate_payload, "upAxis"),
			"+Z"),
		forward_axis=_coalesce_string(
			payload.get("forwardAxis"),
			_dict_string(coordinate_payload, "forwardAxis"),
			"-Y"),
		units=_coalesce_string(
			payload.get("units"),
			_dict_string(coordinate_payload, "units"),
			"NativeLR1"),
	)

	return ScenePackage(
		schema=schema,
		export_type=export_type,
		id=_optional_string(payload.get("id")),
		source_id=_optional_string(payload.get("sourceId")),
		name=_coalesce_string(payload.get("name"), "", ""),
		source_name=_optional_string(payload.get("sourceName")),
		source_format=_optional_string(payload.get("sourceFormat")),
		source_path=_optional_string(payload.get("sourcePath")),
		source_index=_optional_int(payload.get("sourceIndex")),
		coordinate=coordinate,
		metadata=_string_dict(payload.get("metadata")),
		materials=_parse_material_list(payload.get("materials")),
		meshes=_parse_mesh_list(payload.get("meshes")),
		objects=_parse_object_list(payload.get("objects")),
		start_positions=_parse_object_list(payload.get("startPositions")),
		checkpoints=_parse_object_list(payload.get("checkpoints")),
		powerups=_parse_object_list(payload.get("powerups")),
		hazards=_parse_object_list(payload.get("hazards")),
		emitters=_parse_object_list(payload.get("emitters")),
		paths=_parse_path_list(payload.get("paths")),
		npc_paths=_parse_path_list(payload.get("npcPaths")),
		gradients=_parse_gradient_list(payload.get("gradients")),
		filepath=filepath,
	)


def _parse_material_list(value: Any) -> List[TrackMaterialData]:
	items: List[TrackMaterialData] = []
	for entry in _as_list(value):
		items.append(TrackMaterialData(
			id=_optional_string(_dict_value(entry, "id")),
			source_id=_optional_string(_dict_value(entry, "sourceId")),
			name=_coalesce_string(_dict_value(entry, "name"), "", ""),
			source_name=_optional_string(_dict_value(entry, "sourceName")),
			source_format=_optional_string(_dict_value(entry, "sourceFormat")),
			source_path=_optional_string(_dict_value(entry, "sourcePath")),
			source_index=_optional_int(_dict_value(entry, "sourceIndex")),
			texture_name=_coalesce_string(_dict_value(entry, "textureName"), "", ""),
			alpha_texture_name=_coalesce_string(_dict_value(entry, "alphaTextureName"), "", ""),
			diffuse_color=_float_tuple(_dict_value(entry, "diffuseColor"), 4, (1.0, 1.0, 1.0, 1.0)),
			opacity=_float_value(_dict_value(entry, "opacity"), 1.0),
			double_sided=bool(_dict_value(entry, "doubleSided", False)),
			gradients=_parse_gradient_list(_dict_value(entry, "gradients")),
			metadata=_string_dict(_dict_value(entry, "metadata")),
		))
	return items


def _parse_mesh_list(value: Any) -> List[TrackMeshData]:
	items: List[TrackMeshData] = []
	for entry in _as_list(value):
		items.append(TrackMeshData(
			id=_optional_string(_dict_value(entry, "id")),
			source_id=_optional_string(_dict_value(entry, "sourceId")),
			name=_coalesce_string(_dict_value(entry, "name"), "", ""),
			source_name=_optional_string(_dict_value(entry, "sourceName")),
			source_format=_optional_string(_dict_value(entry, "sourceFormat")),
			source_path=_optional_string(_dict_value(entry, "sourcePath")),
			source_index=_optional_int(_dict_value(entry, "sourceIndex")),
			material_name=_coalesce_string(_dict_value(entry, "materialName"), "", ""),
			is_collision_mesh=bool(_dict_value(entry, "isCollisionMesh", False)),
			vertices=_parse_vertex_list(_dict_value(entry, "vertices")),
			indices=_int_list(_dict_value(entry, "indices")),
			metadata=_string_dict(_dict_value(entry, "metadata")),
		))
	return items


def _parse_object_list(value: Any) -> List[TrackObjectData]:
	items: List[TrackObjectData] = []
	for entry in _as_list(value):
		items.append(TrackObjectData(
			id=_optional_string(_dict_value(entry, "id")),
			source_id=_optional_string(_dict_value(entry, "sourceId")),
			name=_coalesce_string(_dict_value(entry, "name"), "", ""),
			source_name=_optional_string(_dict_value(entry, "sourceName")),
			source_format=_optional_string(_dict_value(entry, "sourceFormat")),
			source_path=_optional_string(_dict_value(entry, "sourcePath")),
			source_index=_optional_int(_dict_value(entry, "sourceIndex")),
			mesh_name=_coalesce_string(_dict_value(entry, "meshName"), "", ""),
			material_name=_coalesce_string(_dict_value(entry, "materialName"), "", ""),
			path_name=_coalesce_string(_dict_value(entry, "pathName"), "", ""),
			visible=bool(_dict_value(entry, "visible", True)),
			transform=_parse_transform(_dict_value(entry, "transform")),
			metadata=_string_dict(_dict_value(entry, "metadata")),
		))
	return items


def _parse_path_list(value: Any) -> List[TrackPathData]:
	items: List[TrackPathData] = []
	for entry in _as_list(value):
		items.append(TrackPathData(
			id=_optional_string(_dict_value(entry, "id")),
			source_id=_optional_string(_dict_value(entry, "sourceId")),
			name=_coalesce_string(_dict_value(entry, "name"), "", ""),
			source_name=_optional_string(_dict_value(entry, "sourceName")),
			source_format=_optional_string(_dict_value(entry, "sourceFormat")),
			source_path=_optional_string(_dict_value(entry, "sourcePath")),
			source_index=_optional_int(_dict_value(entry, "sourceIndex")),
			closed=bool(_dict_value(entry, "closed", False)),
			nodes=_parse_path_node_list(_dict_value(entry, "nodes")),
			metadata=_string_dict(_dict_value(entry, "metadata")),
		))
	return items


def _parse_gradient_list(value: Any) -> List[TrackGradientData]:
	items: List[TrackGradientData] = []
	for entry in _as_list(value):
		items.append(TrackGradientData(
			id=_optional_string(_dict_value(entry, "id")),
			source_id=_optional_string(_dict_value(entry, "sourceId")),
			name=_coalesce_string(_dict_value(entry, "name"), "", ""),
			source_name=_optional_string(_dict_value(entry, "sourceName")),
			source_format=_optional_string(_dict_value(entry, "sourceFormat")),
			source_path=_optional_string(_dict_value(entry, "sourcePath")),
			source_index=_optional_int(_dict_value(entry, "sourceIndex")),
			stops=_parse_gradient_stop_list(_dict_value(entry, "stops")),
			metadata=_string_dict(_dict_value(entry, "metadata")),
		))
	return items


def _parse_vertex_list(value: Any) -> List[TrackVertexData]:
	items: List[TrackVertexData] = []
	for entry in _as_list(value):
		items.append(TrackVertexData(
			position=_float_tuple(_dict_value(entry, "position"), 3, (0.0, 0.0, 0.0)),
			normal=_float_tuple(_dict_value(entry, "normal"), 3, (0.0, 0.0, 0.0)),
			primary_tex_coord=_float_tuple(_dict_value(entry, "primaryTexCoord"), 2, (0.0, 0.0)),
			color=_float_tuple(_dict_value(entry, "color"), 4, (1.0, 1.0, 1.0, 1.0)),
			metadata=_string_dict(_dict_value(entry, "metadata")),
		))
	return items


def _parse_path_node_list(value: Any) -> List[TrackPathNodeData]:
	items: List[TrackPathNodeData] = []
	for entry in _as_list(value):
		items.append(TrackPathNodeData(
			position=_float_tuple(_dict_value(entry, "position"), 3, (0.0, 0.0, 0.0)),
			forward=_float_tuple(_dict_value(entry, "forward"), 3, (0.0, 0.0, 1.0)),
			up=_float_tuple(_dict_value(entry, "up"), 3, (0.0, 1.0, 0.0)),
			width=_float_value(_dict_value(entry, "width"), 0.0),
			metadata=_string_dict(_dict_value(entry, "metadata")),
		))
	return items


def _parse_gradient_stop_list(value: Any) -> List[TrackGradientStopData]:
	items: List[TrackGradientStopData] = []
	for entry in _as_list(value):
		items.append(TrackGradientStopData(
			position=_float_value(_dict_value(entry, "position"), 0.0),
			color=_float_tuple(_dict_value(entry, "color"), 4, (1.0, 1.0, 1.0, 1.0)),
			metadata=_string_dict(_dict_value(entry, "metadata")),
		))
	return items


def _parse_transform(value: Any) -> TrackTransformData:
	entry = value if isinstance(value, dict) else {}
	return TrackTransformData(
		position=_float_tuple(entry.get("position"), 3, (0.0, 0.0, 0.0)),
		rotation=_float_tuple(entry.get("rotation"), 4, (0.0, 0.0, 0.0, 1.0)),
		scale=_float_tuple(entry.get("scale"), 3, (1.0, 1.0, 1.0)),
	)


def _as_list(value: Any) -> List[Any]:
	return list(value) if isinstance(value, list) else []


def _dict_value(entry: Any, key: str, default: Any = None) -> Any:
	if isinstance(entry, dict):
		return entry.get(key, default)
	return default


def _dict_string(entry: Any, key: str) -> Optional[str]:
	if not isinstance(entry, dict):
		return None
	return _optional_string(entry.get(key))


def _get_string(entry: Dict[str, Any], key: str) -> str:
	value = _optional_string(entry.get(key))
	if value is None:
		raise SchemaValidationError("Missing required string field '{0}'.".format(key))
	return value


def _optional_string(value: Any) -> Optional[str]:
	if value is None:
		return None
	if isinstance(value, str):
		return value
	return str(value)


def _optional_int(value: Any) -> Optional[int]:
	if value is None:
		return None
	try:
		return int(value)
	except (TypeError, ValueError):
		return None


def _coalesce_string(*values: Any) -> str:
	for value in values:
		parsed = _optional_string(value)
		if parsed is not None:
			return parsed
	return ""


def _float_value(value: Any, default: float) -> float:
	try:
		return float(value)
	except (TypeError, ValueError):
		return default


def _float_tuple(value: Any, size: int, default: Sequence[float]) -> Tuple[float, ...]:
	if not isinstance(value, (list, tuple)):
		return tuple(default)

	items = list(value[:size])
	while len(items) < size:
		items.append(default[len(items)])

	return tuple(_float_value(items[index], default[index]) for index in range(size))


def _int_list(value: Any) -> List[int]:
	output: List[int] = []
	for entry in _as_list(value):
		try:
			output.append(int(entry))
		except (TypeError, ValueError):
			continue
	return output


def _string_dict(value: Any) -> Dict[str, str]:
	if not isinstance(value, dict):
		return {}

	output: Dict[str, str] = {}
	for key, entry in value.items():
		output[str(key)] = "" if entry is None else str(entry)
	return output
