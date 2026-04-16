import json

from .schema import ScenePackage, SchemaValidationError, parse_scene_package


def load_scene_package(filepath: str) -> ScenePackage:
	with open(filepath, "r", encoding="utf-8") as handle:
		payload = json.load(handle)

	if not isinstance(payload, dict):
		raise SchemaValidationError("LR1Tools package root must be a JSON object.")

	return parse_scene_package(payload, filepath)
