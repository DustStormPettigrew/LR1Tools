bl_info = {
	"name": "LR1Tools Scene Package Importer",
	"author": "OpenAI Codex",
	"version": (0, 1, 4),
	"blender": (3, 6, 0),
	"location": "File > Import > LR1Tools Scene Package (.json)",
	"description": "Import LR1Tools scene packages and individual asset packages.",
	"category": "Import-Export",
}

try:
	import bpy
	from bpy.props import StringProperty
	from bpy.types import Operator
	from bpy_extras.io_utils import ImportHelper
except ImportError:
	bpy = None


if bpy is not None:
	from .builder import import_scene_package
	from .loader import SchemaValidationError, load_scene_package

	class IMPORT_SCENE_OT_lr1tools_json(Operator, ImportHelper):
		bl_idname = "import_scene.lr1tools_json"
		bl_label = "Import LR1Tools Scene Package"
		bl_options = {"REGISTER", "UNDO"}

		filename_ext = ".json"
		filter_glob: StringProperty(default="*.json", options={"HIDDEN"})

		def execute(self, context):
			try:
				package = load_scene_package(self.filepath)
				summary = import_scene_package(context, package, self.filepath)
				self.report({"INFO"}, summary)
				return {"FINISHED"}
			except SchemaValidationError as exc:
				self.report({"ERROR"}, str(exc))
			except Exception as exc:
				self.report({"ERROR"}, "LR1Tools import failed: {0}".format(exc))

			return {"CANCELLED"}


	def menu_func_import(self, context):
		self.layout.operator(
			IMPORT_SCENE_OT_lr1tools_json.bl_idname,
			text="LR1Tools Scene Package (.json)")


	CLASSES = (
		IMPORT_SCENE_OT_lr1tools_json,
	)


	def register():
		for cls in CLASSES:
			bpy.utils.register_class(cls)

		bpy.types.TOPBAR_MT_file_import.append(menu_func_import)


	def unregister():
		bpy.types.TOPBAR_MT_file_import.remove(menu_func_import)

		for cls in reversed(CLASSES):
			bpy.utils.unregister_class(cls)

else:
	def register():
		raise RuntimeError("This addon must be loaded inside Blender.")


	def unregister():
		pass
