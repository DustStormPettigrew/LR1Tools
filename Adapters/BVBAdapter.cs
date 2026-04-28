using LR1Tools.Contracts;
using LibLR1;
using LibLR1.Utils;
using System.Globalization;

namespace LR1Tools.Adapters
{
	public static class BVBAdapter
	{
		public static TrackScene ToScene(BVB p_source, string p_sceneName = null)
		{
			TrackScene scene = new TrackScene();
			scene.ExportType = TrackSceneExportTypes.Mesh;
			scene.Name = string.IsNullOrEmpty(p_sceneName) ? "BVBScene" : p_sceneName;
			AdapterCommon.SetSceneProvenance(scene, "BVB", scene.Name);

			if (p_source == null)
			{
				return scene;
			}

			scene.Metadata["SourceFormat"] = "BVB";
			scene.Metadata["MaterialCount"] = (p_source.Materials != null ? p_source.Materials.Length : 0).ToString(CultureInfo.InvariantCulture);
			scene.Metadata["VertexCount"] = (p_source.Vertices != null ? p_source.Vertices.Length : 0).ToString(CultureInfo.InvariantCulture);
			scene.Metadata["PolygonCount"] = (p_source.Polygons != null ? p_source.Polygons.Length : 0).ToString(CultureInfo.InvariantCulture);
			scene.Metadata["PolygonRangeCount"] = (p_source.PolygonRanges != null ? p_source.PolygonRanges.Length : 0).ToString(CultureInfo.InvariantCulture);
			AdapterCommon.AddArrayMetadata(scene.Metadata, "Material", p_source.Materials);

			scene.Meshes.Add(ToMesh(p_source, scene.Name));
			return scene;
		}

		public static TrackMesh ToMesh(BVB p_source, string p_meshName = null)
		{
			TrackMesh mesh = new TrackMesh();
			mesh.Name = NormalizeMeshName(p_meshName);
			mesh.IsCollisionMesh = true;
			AdapterCommon.SetMeshProvenance(mesh, "BVB", mesh.Name, p_meshName, "0", 0);

			if (p_source == null)
			{
				return mesh;
			}

			LRVector3[] sourceVertices = p_source.Vertices ?? new LRVector3[0];
			for (int i = 0; i < sourceVertices.Length; i++)
			{
				TrackVertex vertex = new TrackVertex();
				vertex.Position = AdapterCommon.ToVector3(sourceVertices[i]);
				vertex.Metadata["VertexIndex"] = i.ToString(CultureInfo.InvariantCulture);
				mesh.Vertices.Add(vertex);
			}

			BVB_Polygon[] polygons = p_source.Polygons ?? new BVB_Polygon[0];
			int skippedTriangleCount = 0;
			for (int i = 0; i < polygons.Length; i++)
			{
				BVB_Polygon polygon = polygons[i];
				if (!IsValidVertexIndex(polygon.V0, mesh.Vertices.Count) ||
					!IsValidVertexIndex(polygon.V1, mesh.Vertices.Count) ||
					!IsValidVertexIndex(polygon.V2, mesh.Vertices.Count))
				{
					skippedTriangleCount++;
					continue;
				}

				mesh.Indices.Add(polygon.V0);
				mesh.Indices.Add(polygon.V1);
				mesh.Indices.Add(polygon.V2);
			}

			mesh.Metadata["SourceFormat"] = "BVB";
			mesh.Metadata["MaterialCount"] = (p_source.Materials != null ? p_source.Materials.Length : 0).ToString(CultureInfo.InvariantCulture);
			mesh.Metadata["VertexCount"] = mesh.Vertices.Count.ToString(CultureInfo.InvariantCulture);
			mesh.Metadata["IndexCount"] = mesh.Indices.Count.ToString(CultureInfo.InvariantCulture);
			mesh.Metadata["PolygonCount"] = polygons.Length.ToString(CultureInfo.InvariantCulture);
			mesh.Metadata["PolygonRangeCount"] = (p_source.PolygonRanges != null ? p_source.PolygonRanges.Length : 0).ToString(CultureInfo.InvariantCulture);
			mesh.Metadata["SkippedTriangleCount"] = skippedTriangleCount.ToString(CultureInfo.InvariantCulture);

			string materialName = ResolveMeshMaterialName(p_source.Materials);
			if (!string.IsNullOrEmpty(materialName))
			{
				mesh.MaterialName = materialName;
			}

			return mesh;
		}

		private static bool IsValidVertexIndex(int p_index, int p_vertexCount)
		{
			return p_index >= 0 && p_index < p_vertexCount;
		}

		private static string ResolveMeshMaterialName(string[] p_materials)
		{
			if (p_materials == null)
			{
				return string.Empty;
			}

			for (int i = 0; i < p_materials.Length; i++)
			{
				if (!string.IsNullOrWhiteSpace(p_materials[i]))
				{
					return p_materials[i].Trim();
				}
			}

			return string.Empty;
		}

		private static string NormalizeMeshName(string p_name)
		{
			return string.IsNullOrEmpty(p_name) ? "BVB" : p_name.ToUpperInvariant();
		}
	}
}
