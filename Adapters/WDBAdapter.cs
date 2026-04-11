using LR1Tools.Contracts;
using LibLR1;
using LibLR1.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace LR1Tools.Adapters
{
	public static class WDBAdapter
	{
		public static TrackScene ToScene(WDB p_source, string p_sceneName = null)
		{
			TrackScene scene = new TrackScene();
			scene.Name = string.IsNullOrEmpty(p_sceneName) ? "WDBScene" : p_sceneName;
			scene.SourceName = "WDB";

			if (p_source == null)
			{
				return scene;
			}

			scene.Metadata["SourceFormat"] = "WDB";
			AdapterCommon.AddArrayMetadata(scene.Metadata, "TDB", p_source.TDBs);
			AdapterCommon.AddArrayMetadata(scene.Metadata, "MDB", p_source.MDBs);
			AdapterCommon.AddArrayMetadata(scene.Metadata, "ADB", p_source.ADBs);
			AdapterCommon.AddArrayMetadata(scene.Metadata, "GDB", p_source.GDBs);
			AdapterCommon.AddArrayMetadata(scene.Metadata, "GDB2", p_source.GDB2s);
			AdapterCommon.AddArrayMetadata(scene.Metadata, "SDB", p_source.SDBs);
			AdapterCommon.AddArrayMetadata(scene.Metadata, "BDB", p_source.BDBs);
			AdapterCommon.AddArrayMetadata(scene.Metadata, "MAB", p_source.MABs);
			AdapterCommon.AddArrayMetadata(scene.Metadata, "BVB", p_source.BVBs);

			List<TrackObject> objects = ToObjects(p_source);
			for (int i = 0; i < objects.Count; i++)
			{
				scene.Objects.Add(objects[i]);
			}

			return scene;
		}

		public static void ResolveMeshReferences(TrackScene p_scene)
		{
			if (p_scene == null)
			{
				return;
			}

			ResolveMeshReferences(p_scene.Objects, p_scene.Meshes);
		}

		public static void ResolveMeshReferences(IList<TrackObject> p_objects, IList<TrackMesh> p_meshes)
		{
			if (p_objects == null || p_meshes == null || p_objects.Count == 0 || p_meshes.Count == 0)
			{
				return;
			}

			Dictionary<string, string> meshNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < p_meshes.Count; i++)
			{
				TrackMesh mesh = p_meshes[i];
				if (mesh == null || string.IsNullOrEmpty(mesh.Name))
				{
					continue;
				}

				AddMeshAlias(meshNameMap, mesh.Name, mesh.Name);
				AddMeshAlias(meshNameMap, NormalizeMeshReference(mesh.Name), mesh.Name);
			}

			for (int i = 0; i < p_objects.Count; i++)
			{
				TrackObject obj = p_objects[i];
				if (obj == null || string.IsNullOrEmpty(obj.MeshName))
				{
					continue;
				}

				string normalizedMeshReference = NormalizeMeshReference(obj.MeshName);
				string resolvedMeshName;
				if (meshNameMap.TryGetValue(obj.MeshName, out resolvedMeshName) ||
					meshNameMap.TryGetValue(normalizedMeshReference, out resolvedMeshName))
				{
					obj.MeshName = resolvedMeshName;
				}
			}
		}

		public static List<TrackObject> ToObjects(WDB p_source)
		{
			List<TrackObject> objects = new List<TrackObject>();
			if (p_source == null)
			{
				return objects;
			}

			if (p_source.StaticModels != null)
			{
				foreach (KeyValuePair<string, WDB_StaticModel> pair in p_source.StaticModels)
				{
					objects.Add(CreateStaticModelObject(p_source, pair.Key, pair.Value));
				}
			}

			if (p_source.AnimatedModels != null)
			{
				foreach (KeyValuePair<string, WDB_AnimatedModel> pair in p_source.AnimatedModels)
				{
					objects.Add(CreateAnimatedModelObject(p_source, pair.Key, pair.Value));
				}
			}

			if (p_source.BDBModels != null)
			{
				foreach (KeyValuePair<string, WDB_BDBModel> pair in p_source.BDBModels)
				{
					objects.Add(CreateBdbModelObject(p_source, pair.Key, pair.Value));
				}
			}

			if (p_source.Billboards != null)
			{
				for (int i = 0; i < p_source.Billboards.Length; i++)
				{
					objects.Add(CreateBillboardObject(i, p_source.Billboards[i]));
				}
			}

			if (p_source.BVBModels != null)
			{
				foreach (KeyValuePair<string, WDB_BVBModel> pair in p_source.BVBModels)
				{
					objects.Add(CreateBvbModelObject(p_source, pair.Key, pair.Value));
				}
			}

			if (p_source.Cameras != null)
			{
				foreach (KeyValuePair<string, WDB_Camera> pair in p_source.Cameras)
				{
					objects.Add(CreateCameraObject(pair.Key, pair.Value));
				}
			}

			if (p_source.AmbientLights != null)
			{
				foreach (KeyValuePair<string, WDB_AmbientLight> pair in p_source.AmbientLights)
				{
					objects.Add(CreateAmbientLightObject(pair.Key, pair.Value));
				}
			}

			if (p_source.DirectionalLights != null)
			{
				foreach (KeyValuePair<string, WDB_DirectionalLight> pair in p_source.DirectionalLights)
				{
					objects.Add(CreateDirectionalLightObject(pair.Key, pair.Value));
				}
			}

			return objects;
		}

		private static TrackObject CreateStaticModelObject(WDB p_source, string p_name, WDB_StaticModel p_model)
		{
			TrackObject obj = CreateObject(p_name, "StaticModel");
			if (p_model == null)
			{
				return obj;
			}

			if (p_model.ModelRef != null)
			{
				string gdbName = AdapterCommon.ResolveName(p_source.GDBs, p_model.ModelRef.IndexGDB, "GDB");
				obj.MeshName = NormalizeMeshReference(gdbName);
				obj.Metadata["GDBIndex"] = p_model.ModelRef.IndexGDB.ToString(CultureInfo.InvariantCulture);
				obj.Metadata["GDBName"] = gdbName;
				obj.Metadata["GDBUnknown"] = p_model.ModelRef.Unknown.ToString("R", CultureInfo.InvariantCulture);
			}

			ApplyTransform(obj, p_model.Position, p_model.RotationFwd, p_model.RotationUp);
			AddUnknown3E(obj.Metadata, p_model.Unknown_3E);

			if (p_model.Unknown_3F != null)
			{
				obj.Metadata["Unknown3F"] = string.Format(
					CultureInfo.InvariantCulture,
					"{0:R},{1:R}",
					p_model.Unknown_3F.Unknown1,
					p_model.Unknown_3F.Unknown2
				);
			}

			obj.Metadata["Unknown42"] = p_model.Unknown_42 ? "true" : "false";
			return obj;
		}

		private static TrackObject CreateAnimatedModelObject(WDB p_source, string p_name, WDB_AnimatedModel p_model)
		{
			TrackObject obj = CreateObject(p_name, "AnimatedModel");
			if (p_model == null)
			{
				return obj;
			}

			if (p_model.ModelRef != null)
			{
				if (p_model.ModelRef.IndexGDB.HasValue)
				{
					string gdbName = AdapterCommon.ResolveName(p_source.GDBs, p_model.ModelRef.IndexGDB.Value, "GDB");
					obj.MeshName = NormalizeMeshReference(gdbName);
					obj.Metadata["GDBIndex"] = p_model.ModelRef.IndexGDB.Value.ToString(CultureInfo.InvariantCulture);
					obj.Metadata["GDBName"] = gdbName;
				}

				obj.Metadata["ADBIndex"] = p_model.ModelRef.IndexADB.ToString(CultureInfo.InvariantCulture);
				obj.Metadata["ADBName"] = AdapterCommon.ResolveName(p_source.ADBs, p_model.ModelRef.IndexADB, "ADB");
				obj.Metadata["SDBIndex"] = p_model.ModelRef.IndexSDB.ToString(CultureInfo.InvariantCulture);
				obj.Metadata["SDBName"] = AdapterCommon.ResolveName(p_source.SDBs, p_model.ModelRef.IndexSDB, "SDB");
				obj.Metadata["ModelUnknown"] = p_model.ModelRef.Unknown.ToString("R", CultureInfo.InvariantCulture);
			}

			ApplyTransform(obj, p_model.Position, p_model.RotationFwd, p_model.RotationUp);
			AddUnknown3E(obj.Metadata, p_model.Unknown_3E);

			if (p_model.Unknown_3F != null)
			{
				obj.Metadata["Unknown3F"] = string.Format(
					CultureInfo.InvariantCulture,
					"{0:R},{1:R}",
					p_model.Unknown_3F.Unknown1,
					p_model.Unknown_3F.Unknown2
				);
			}

			obj.Metadata["Unknown35"] = p_model.Unknown_35.ToString(CultureInfo.InvariantCulture);
			obj.Metadata["Unknown42"] = p_model.Unknown_42 ? "true" : "false";
			obj.Metadata["Unknown4C"] = p_model.Unknown_4C ? "true" : "false";
			return obj;
		}

		private static TrackObject CreateBdbModelObject(WDB p_source, string p_name, WDB_BDBModel p_model)
		{
			TrackObject obj = CreateObject(p_name, "BDBModel");
			if (p_model == null)
			{
				return obj;
			}

			if (p_model.ModelRef != null)
			{
				string gdbName = AdapterCommon.ResolveName(p_source.GDBs, p_model.ModelRef.IndexGDB, "GDB");
				obj.MeshName = NormalizeMeshReference(gdbName);
				obj.Metadata["BDBIndex"] = p_model.ModelRef.IndexBDB.ToString(CultureInfo.InvariantCulture);
				obj.Metadata["BDBName"] = AdapterCommon.ResolveName(p_source.BDBs, p_model.ModelRef.IndexBDB, "BDB");
				obj.Metadata["GDBIndex"] = p_model.ModelRef.IndexGDB.ToString(CultureInfo.InvariantCulture);
				obj.Metadata["GDBName"] = gdbName;
				obj.Metadata["ModelUnknown"] = p_model.ModelRef.Unknown.ToString("R", CultureInfo.InvariantCulture);
			}

			ApplyTransform(obj, p_model.Position, p_model.RotationFwd, p_model.RotationUp);
			AddUnknown3E(obj.Metadata, p_model.Unknown_3E);
			obj.Metadata["Unknown42"] = p_model.Unknown_42 ? "true" : "false";
			return obj;
		}

		private static TrackObject CreateBillboardObject(int p_index, WDB_Billboard p_billboard)
		{
			TrackObject obj = CreateObject(
				string.Format(CultureInfo.InvariantCulture, "Billboard[{0}]", p_index),
				"Billboard"
			);

			if (p_billboard == null)
			{
				return obj;
			}

			obj.MaterialName = p_billboard.TextureName ?? string.Empty;
			obj.Transform.Position = AdapterCommon.ToVector3(p_billboard.Position);
			obj.Metadata["Unknown38"] = AdapterCommon.FormatVector3(p_billboard.Unknown38);
			obj.Metadata["Unknown3A"] = p_billboard.Unknown3A.ToString("R", CultureInfo.InvariantCulture);
			obj.Metadata["Unknown3B"] = p_billboard.Unknown3B.ToString("R", CultureInfo.InvariantCulture);
			obj.Metadata["Unknown3C"] = p_billboard.Unknown3C.ToString("R", CultureInfo.InvariantCulture);
			obj.Metadata["Unknown2B"] = string.Format(
				CultureInfo.InvariantCulture,
				"{0},{1}",
				p_billboard.Unknown2B_A,
				p_billboard.Unknown2B_B
			);
			obj.Metadata["Unknown3E"] = string.Format(
				CultureInfo.InvariantCulture,
				"{0},{1}",
				p_billboard.Unknown3E_A,
				p_billboard.Unknown3E_B
			);
			return obj;
		}

		private static TrackObject CreateBvbModelObject(WDB p_source, string p_name, WDB_BVBModel p_model)
		{
			TrackObject obj = CreateObject(p_name, "BVBModel");
			if (p_model == null)
			{
				return obj;
			}

			obj.MeshName = AdapterCommon.ResolveName(p_source.BVBs, p_model.ModelRef, "BVB");
			obj.Metadata["IsCollisionObject"] = "true";
			obj.Metadata["BVBIndex"] = p_model.ModelRef.ToString(CultureInfo.InvariantCulture);
			ApplyTransform(obj, p_model.Position, p_model.RotationFwd, p_model.RotationUp);
			return obj;
		}

		private static TrackObject CreateCameraObject(string p_name, WDB_Camera p_camera)
		{
			TrackObject obj = CreateObject(p_name, "Camera");
			if (p_camera == null)
			{
				return obj;
			}

			ApplyTransform(obj, p_camera.Position, p_camera.RotationFwd, p_camera.RotationUp);

			if (p_camera.Model != null)
			{
				obj.Metadata["AnimatedModelId"] = p_camera.Model.AnimatedModelId.ToString(CultureInfo.InvariantCulture);
				obj.Metadata["BoneId"] = p_camera.Model.BoneId.ToString(CultureInfo.InvariantCulture);
			}

			obj.Metadata["NearPlane"] = p_camera.NearPlane.ToString("R", CultureInfo.InvariantCulture);
			obj.Metadata["FarPlane"] = p_camera.FarPlane.ToString("R", CultureInfo.InvariantCulture);
			obj.Metadata["Fov"] = p_camera.Fov.ToString("R", CultureInfo.InvariantCulture);
			return obj;
		}

		private static TrackObject CreateAmbientLightObject(string p_name, WDB_AmbientLight p_light)
		{
			TrackObject obj = CreateObject(p_name, "AmbientLight");
			if (p_light != null)
			{
				obj.Metadata["Color"] = AdapterCommon.FormatColor(p_light.Color, false);
			}
			return obj;
		}

		private static TrackObject CreateDirectionalLightObject(string p_name, WDB_DirectionalLight p_light)
		{
			TrackObject obj = CreateObject(p_name, "DirectionalLight");
			if (p_light != null)
			{
				obj.Transform.Rotation = AdapterCommon.CreateRotationFromForwardUp(p_light.Direction, null);
				obj.Metadata["Color"] = AdapterCommon.FormatColor(p_light.Color, false);
				obj.Metadata["Direction"] = AdapterCommon.FormatVector3(p_light.Direction);
			}
			return obj;
		}

		private static TrackObject CreateObject(string p_name, string p_nativeType)
		{
			TrackObject obj = new TrackObject();
			obj.Name = p_name ?? string.Empty;
			obj.Metadata["SourceFormat"] = "WDB";
			obj.Metadata["NativeType"] = p_nativeType;
			return obj;
		}

		private static void ApplyTransform(TrackObject p_object, LRVector3 p_position, LRVector3 p_forward, LRVector3 p_up)
		{
			p_object.Transform.Position = AdapterCommon.ToVector3(p_position);
			p_object.Transform.Rotation = AdapterCommon.CreateRotationFromForwardUp(p_forward, p_up);
			p_object.Metadata["RotationForward"] = AdapterCommon.FormatVector3(p_forward);
			p_object.Metadata["RotationUp"] = AdapterCommon.FormatVector3(p_up);
		}

		private static void AddUnknown3E(Dictionary<string, string> p_metadata, WDB_Unknown3E[] p_values)
		{
			WDB_Unknown3E[] values = p_values ?? new WDB_Unknown3E[0];
			p_metadata["Unknown3E.Count"] = values.Length.ToString(CultureInfo.InvariantCulture);
			for (int i = 0; i < values.Length; i++)
			{
				p_metadata[string.Format(CultureInfo.InvariantCulture, "Unknown3E[{0}]", i)] = string.Format(
					CultureInfo.InvariantCulture,
					"{0},{1},{2},{3}",
					values[i].Unknown1,
					values[i].Unknown2,
					values[i].Unknown3,
					values[i].Unknown4
				);
			}
		}

		private static string NormalizeMeshReference(string p_name)
		{
			if (string.IsNullOrEmpty(p_name))
			{
				return string.Empty;
			}

			string fileName = Path.GetFileName(p_name);
			string withoutExtension = Path.GetFileNameWithoutExtension(fileName);
			string normalized = string.IsNullOrEmpty(withoutExtension) ? fileName : withoutExtension;
			return normalized.ToUpperInvariant();
		}

		private static void AddMeshAlias(Dictionary<string, string> p_meshNameMap, string p_alias, string p_meshName)
		{
			if (string.IsNullOrEmpty(p_alias) || string.IsNullOrEmpty(p_meshName))
			{
				return;
			}

			p_meshNameMap[p_alias] = p_meshName;
		}
	}
}


