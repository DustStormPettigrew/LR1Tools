from mathutils import Matrix, Quaternion, Vector

from .schema import CoordinateMetadata, TrackTransformData


def _axis_vector(token: str) -> Vector:
	normalized = (token or "").strip().upper()
	axis_map = {
		"+X": Vector((1.0, 0.0, 0.0)),
		"-X": Vector((-1.0, 0.0, 0.0)),
		"+Y": Vector((0.0, 1.0, 0.0)),
		"-Y": Vector((0.0, -1.0, 0.0)),
		"+Z": Vector((0.0, 0.0, 1.0)),
		"-Z": Vector((0.0, 0.0, -1.0)),
	}
	return axis_map.get(normalized, Vector((0.0, 0.0, 0.0)))


def _basis_matrix(right: str, up: str, forward: str) -> Matrix:
	return Matrix((
		_axis_vector(right),
		_axis_vector(up),
		_axis_vector(forward),
	)).transposed()


def _normalize_or_default(vector: Vector, default: Vector) -> Vector:
	if vector.length_squared == 0.0:
		return default.copy()
	return vector.normalized()


class CoordinateConverter:
	def __init__(self, coordinate: CoordinateMetadata):
		self.coordinate = coordinate or CoordinateMetadata()
		self._source_basis = _basis_matrix(
			self.coordinate.right_axis,
			self.coordinate.up_axis,
			self.coordinate.forward_axis)
		# Blender convention: +X right, +Z up, -Y forward.
		# LR1 native data uses the same convention, so with correct metadata
		# the conversion matrix becomes identity.
		self._target_basis = _basis_matrix("+X", "+Z", "-Y")
		self._source_to_blender = self._target_basis @ self._source_basis.inverted()
		self._blender_to_source = self._source_to_blender.inverted()

	def convert_position(self, values) -> Vector:
		return self._source_to_blender @ Vector(values)

	def convert_direction(self, values) -> Vector:
		direction = self._source_to_blender @ Vector(values)
		if direction.length_squared == 0.0:
			return direction
		return direction.normalized()

	def convert_scale(self, values) -> Vector:
		scale = self._source_to_blender @ Vector(values)
		return Vector((abs(scale.x), abs(scale.y), abs(scale.z)))

	def convert_quaternion(self, values) -> Quaternion:
		x, y, z, w = values
		source_quaternion = Quaternion((w, x, y, z))
		source_rotation = source_quaternion.to_matrix()
		target_rotation = self._source_to_blender @ source_rotation @ self._blender_to_source
		return target_rotation.to_quaternion()

	def convert_forward_up_rotation(self, forward_values, up_values) -> Quaternion:
		source_forward = _normalize_or_default(Vector(forward_values), Vector((1.0, 0.0, 0.0)))
		source_up = _normalize_or_default(Vector(up_values), Vector((0.0, 0.0, 1.0)))
		source_right = source_up.cross(source_forward)
		source_right = _normalize_or_default(source_right, Vector((0.0, 1.0, 0.0)))
		source_ortho_up = source_forward.cross(source_right)
		source_ortho_up = _normalize_or_default(source_ortho_up, Vector((0.0, 0.0, 1.0)))

		# Match LR1TrackEditor.CreateWorldMatrix:
		# local +X = forward, local +Y = right, local +Z = up.
		source_rotation = Matrix((
			source_forward,
			source_right,
			source_ortho_up,
		)).transposed()
		target_rotation = self._source_to_blender @ source_rotation @ self._blender_to_source
		return target_rotation.to_quaternion()

	def convert_transform(self, transform: TrackTransformData):
		return {
			"location": self.convert_position(transform.position),
			"rotation": self.convert_quaternion(transform.rotation),
			"scale": self.convert_scale(transform.scale),
		}
