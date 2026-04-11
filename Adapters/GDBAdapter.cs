using LR1Tools.Contracts;
using LibLR1;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace LR1Tools.Adapters
{
	public static class GDBAdapter
	{
		public static TrackScene ToScene(GDB p_source, string p_sceneName = null)
		{
			TrackScene scene = new TrackScene();
			scene.Name = string.IsNullOrEmpty(p_sceneName) ? "GDBScene" : p_sceneName;
			scene.SourceName = "GDB";

			if (p_source == null)
			{
				return scene;
			}

			scene.Metadata["SourceFormat"] = "GDB";
			scene.Metadata["Scale"] = p_source.Scale.ToString("R", CultureInfo.InvariantCulture);
			scene.Metadata["PolygonCount"] = (p_source.Polygons != null ? p_source.Polygons.Length : 0).ToString(CultureInfo.InvariantCulture);

			string[] materials = p_source.Materials ?? new string[0];
			for (int i = 0; i < materials.Length; i++)
			{
				TrackMaterial material = new TrackMaterial();
				material.Name = materials[i] ?? string.Empty;
				material.Metadata["SourceFormat"] = "GDB";
				material.Metadata["MaterialIndex"] = i.ToString(CultureInfo.InvariantCulture);
				scene.Materials.Add(material);
			}

			List<TrackMesh> meshes = ToMeshes(p_source, scene.Name);
			for (int i = 0; i < meshes.Count; i++)
			{
				scene.Meshes.Add(meshes[i]);
			}

			List<TrackObject> objects = ToObjects(p_source, meshes, scene.Name);
			for (int i = 0; i < objects.Count; i++)
			{
				scene.Objects.Add(objects[i]);
			}

			AddMetaStream(scene.Metadata, p_source.Meta);
			return scene;
		}

		public static List<TrackMesh> ToMeshes(GDB p_source, string p_namePrefix = null)
		{
			List<TrackMesh> meshes = new List<TrackMesh>();
			if (p_source == null)
			{
				return meshes;
			}

			string namePrefix = string.IsNullOrEmpty(p_namePrefix) ? "GDB" : p_namePrefix;
			string meshName = NormalizeMeshName(namePrefix);
			List<TrackVertex> sourceVertices = BuildVertices(p_source);
			List<GDBMeshSegment> segments = ExtractSegments(p_source);
			TrackMesh mesh = new TrackMesh();
			mesh.Name = meshName;
			mesh.MaterialName = ResolveMeshMaterialName(p_source.Materials, segments);
			mesh.IsCollisionMesh = IsCollisionMeshName(meshName);
			mesh.Metadata["SourceFormat"] = "GDB";
			mesh.Metadata["VertexEncoding"] = GetVertexEncoding(p_source);
			mesh.Metadata["Scale"] = p_source.Scale.ToString("R", CultureInfo.InvariantCulture);
			mesh.Metadata["SegmentCount"] = segments.Count.ToString(CultureInfo.InvariantCulture);
			mesh.Metadata["PolygonCount"] = (p_source.Polygons != null ? p_source.Polygons.Length : 0).ToString(CultureInfo.InvariantCulture);

			Dictionary<int, int> sourceToMeshVertexIndex = new Dictionary<int, int>();
			int skippedTriangleCount = 0;

			if (segments.Count == 0)
			{
				AddAllGeometry(mesh, sourceVertices, p_source.Polygons, sourceToMeshVertexIndex, ref skippedTriangleCount);
			}
			else
			{
				for (int i = 0; i < segments.Count; i++)
				{
					GDBMeshSegment segment = segments[i];
					AddSegmentMetadata(mesh.Metadata, segment, i);
					AddSegmentGeometry(
						mesh,
						sourceVertices,
						p_source.Polygons,
						segment,
						i > 0 ? segments[i - 1] : null,
						sourceToMeshVertexIndex,
						ref skippedTriangleCount);
				}
			}

			mesh.Metadata["VertexCount"] = mesh.Vertices.Count.ToString(CultureInfo.InvariantCulture);
			mesh.Metadata["IndexCount"] = mesh.Indices.Count.ToString(CultureInfo.InvariantCulture);
			mesh.Metadata["SkippedTriangleCount"] = skippedTriangleCount.ToString(CultureInfo.InvariantCulture);
			meshes.Add(mesh);

			return meshes;
		}

		public static List<TrackObject> ToObjects(GDB p_source, IList<TrackMesh> p_meshes, string p_namePrefix = null)
		{
			List<TrackObject> output = new List<TrackObject>();
			if (p_source == null || p_meshes == null)
			{
				return output;
			}

			string namePrefix = string.IsNullOrEmpty(p_namePrefix) ? "GDB" : p_namePrefix;
			for (int i = 0; i < p_meshes.Count; i++)
			{
				TrackObject obj = new TrackObject();
				obj.Name = string.Format(CultureInfo.InvariantCulture, "{0}.Object{1}", namePrefix, i);
				obj.MeshName = p_meshes[i].Name;
				obj.MaterialName = p_meshes[i].MaterialName;
				obj.Transform.Scale = new Vector3(p_source.Scale, p_source.Scale, p_source.Scale);
				obj.Metadata["SourceFormat"] = "GDB";
				obj.Metadata["MeshIndex"] = i.ToString(CultureInfo.InvariantCulture);
				obj.Metadata["VertexEncoding"] = GetVertexEncoding(p_source);
				output.Add(obj);
			}

			return output;
		}

		private static List<TrackVertex> BuildVertices(GDB p_source)
		{
			List<TrackVertex> vertices = new List<TrackVertex>();
			string encoding = GetVertexEncoding(p_source);

			if (encoding == "VertexNormal")
			{
				GDB_Vertex_Normal[] source = p_source.VertexNormals ?? new GDB_Vertex_Normal[0];
				for (int i = 0; i < source.Length; i++)
				{
					TrackVertex vertex = new TrackVertex();
					vertex.Position = AdapterCommon.ToVector3(source[i].Position);
					vertex.PrimaryTexCoord = AdapterCommon.ToVector2(source[i].TexCoords);
					vertex.Normal = AdapterCommon.ToVector3(source[i].Normal);
					vertex.Metadata["VertexIndex"] = i.ToString(CultureInfo.InvariantCulture);
					vertices.Add(vertex);
				}
				return vertices;
			}

			if (encoding == "VertexColor")
			{
				GDB_Vertex_Color[] source = p_source.VertexColors ?? new GDB_Vertex_Color[0];
				for (int i = 0; i < source.Length; i++)
				{
					TrackVertex vertex = new TrackVertex();
					vertex.Position = AdapterCommon.ToVector3(source[i].Position);
					vertex.PrimaryTexCoord = AdapterCommon.ToVector2(source[i].TexCoords);
					vertex.Color = AdapterCommon.ToTrackColor(source[i].Color);
					vertex.Metadata["VertexIndex"] = i.ToString(CultureInfo.InvariantCulture);
					vertices.Add(vertex);
				}
				return vertices;
			}

			if (encoding == "VertexUV")
			{
				GDB_Vertex_UV[] source = p_source.VertexUVs ?? new GDB_Vertex_UV[0];
				for (int i = 0; i < source.Length; i++)
				{
					TrackVertex vertex = new TrackVertex();
					vertex.Position = AdapterCommon.ToVector3(source[i].Position);
					vertex.PrimaryTexCoord = AdapterCommon.ToVector2(source[i].TexCoords);
					vertex.Metadata["VertexIndex"] = i.ToString(CultureInfo.InvariantCulture);
					vertices.Add(vertex);
				}
				return vertices;
			}

			GDB_Vertex_Position[] positions = p_source.VertexPositions ?? new GDB_Vertex_Position[0];
			for (int i = 0; i < positions.Length; i++)
			{
				TrackVertex vertex = new TrackVertex();
				vertex.Position = AdapterCommon.ToVector3(positions[i].Position);
				vertex.Metadata["VertexIndex"] = i.ToString(CultureInfo.InvariantCulture);
				vertices.Add(vertex);
			}

			return vertices;
		}

		private static void AddAllGeometry(
			TrackMesh p_mesh,
			List<TrackVertex> p_sourceVertices,
			GDB_Polygon[] p_polygons,
			Dictionary<int, int> p_sourceToMeshVertexIndex,
			ref int p_skippedTriangleCount)
		{
			GDB_Polygon[] polygons = p_polygons ?? new GDB_Polygon[0];
			for (int polygonIndex = 0; polygonIndex < polygons.Length; polygonIndex++)
			{
				int v0 = EnsureMeshVertex(p_mesh, p_sourceVertices, polygons[polygonIndex].V0, p_sourceToMeshVertexIndex);
				int v1 = EnsureMeshVertex(p_mesh, p_sourceVertices, polygons[polygonIndex].V1, p_sourceToMeshVertexIndex);
				int v2 = EnsureMeshVertex(p_mesh, p_sourceVertices, polygons[polygonIndex].V2, p_sourceToMeshVertexIndex);

				if (v0 < 0 || v1 < 0 || v2 < 0)
				{
					p_skippedTriangleCount++;
					continue;
				}

				p_mesh.Indices.Add(v0);
				p_mesh.Indices.Add(v1);
				p_mesh.Indices.Add(v2);
			}
		}

		private static void AddSegmentGeometry(
			TrackMesh p_mesh,
			List<TrackVertex> p_sourceVertices,
			GDB_Polygon[] p_polygons,
			GDBMeshSegment p_segment,
			GDBMeshSegment p_previousSegment,
			Dictionary<int, int> p_sourceToMeshVertexIndex,
			ref int p_skippedTriangleCount)
		{
			GDB_Polygon[] polygons = p_polygons ?? new GDB_Polygon[0];
			if (polygons.Length == 0)
			{
				return;
			}

			int offset = p_segment.Offset < 0 ? 0 : p_segment.Offset;
			int length = p_segment.Length <= 0 ? polygons.Length : p_segment.Length;
			int limit = offset + length;
			if (limit > polygons.Length)
			{
				limit = polygons.Length;
			}

			for (int polygonIndex = offset; polygonIndex < limit; polygonIndex++)
			{
				GDB_Polygon polygon = polygons[polygonIndex];
				int sourceV0 = ResolveSourceVertexIndex(p_segment, p_previousSegment, polygon.V0, p_sourceVertices.Count);
				int sourceV1 = ResolveSourceVertexIndex(p_segment, p_previousSegment, polygon.V1, p_sourceVertices.Count);
				int sourceV2 = ResolveSourceVertexIndex(p_segment, p_previousSegment, polygon.V2, p_sourceVertices.Count);

				if (sourceV0 < 0 || sourceV1 < 0 || sourceV2 < 0)
				{
					p_skippedTriangleCount++;
					continue;
				}

				int v0 = EnsureMeshVertex(p_mesh, p_sourceVertices, sourceV0, p_sourceToMeshVertexIndex);
				int v1 = EnsureMeshVertex(p_mesh, p_sourceVertices, sourceV1, p_sourceToMeshVertexIndex);
				int v2 = EnsureMeshVertex(p_mesh, p_sourceVertices, sourceV2, p_sourceToMeshVertexIndex);

				if (v0 < 0 || v1 < 0 || v2 < 0)
				{
					p_skippedTriangleCount++;
					continue;
				}

				p_mesh.Indices.Add(v0);
				p_mesh.Indices.Add(v1);
				p_mesh.Indices.Add(v2);
			}
		}

		private static List<GDBMeshSegment> ExtractSegments(GDB p_source)
		{
			List<GDBMeshSegment> segments = new List<GDBMeshSegment>();
			GDB_Meta[] metaStream = p_source.Meta ?? new GDB_Meta[0];

			int materialId = -1;
			GDB_Meta_Vertices vertexMeta = null;
			GDB_Meta_Bone boneMeta = null;
			GDB_Meta_2F meta2F = null;
			bool hasMeta30 = false;

			for (int i = 0; i < metaStream.Length; i++)
			{
				GDB_Meta meta = metaStream[i];

				GDB_Meta_Material material = meta as GDB_Meta_Material;
				if (material != null)
				{
					materialId = material.MaterialId;
					continue;
				}

				GDB_Meta_Vertices vertices = meta as GDB_Meta_Vertices;
				if (vertices != null)
				{
					vertexMeta = vertices;
					continue;
				}

				GDB_Meta_Bone bone = meta as GDB_Meta_Bone;
				if (bone != null)
				{
					boneMeta = bone;
					continue;
				}

				GDB_Meta_2F extra = meta as GDB_Meta_2F;
				if (extra != null)
				{
					meta2F = extra;
					continue;
				}

				if (meta is GDB_Meta_30)
				{
					hasMeta30 = true;
					continue;
				}

				GDB_Meta_Indices indices = meta as GDB_Meta_Indices;
				if (indices != null)
				{
					GDBMeshSegment segment = new GDBMeshSegment();
					segment.MaterialId = materialId;
					segment.Offset = indices.Offset;
					segment.Length = indices.Length;
					segment.VertexMeta = vertexMeta;
					segment.VertexStart = vertexMeta != null ? vertexMeta.Offset : 0;
					segment.VertexLength = vertexMeta != null ? vertexMeta.Length : 0;
					segment.VertexOffset = vertexMeta != null ? vertexMeta.UnknownByte : 0;
					segment.BoneMeta = boneMeta;
					segment.Meta2F = meta2F;
					segment.HasMeta30 = hasMeta30;
					segments.Add(segment);
				}
			}

			if (segments.Count == 0)
			{
				GDBMeshSegment fallback = new GDBMeshSegment();
				fallback.MaterialId = p_source.Materials != null && p_source.Materials.Length == 1 ? 0 : -1;
				fallback.Offset = 0;
				fallback.Length = p_source.Polygons != null ? p_source.Polygons.Length : 0;
				fallback.VertexStart = 0;
				fallback.VertexLength = GetVertexCount(p_source);
				fallback.VertexOffset = 0;
				segments.Add(fallback);
			}

			return segments;
		}

		private static int EnsureMeshVertex(
			TrackMesh p_mesh,
			List<TrackVertex> p_sourceVertices,
			int p_sourceVertexIndex,
			Dictionary<int, int> p_sourceToMeshVertexIndex)
		{
			if (p_sourceVertexIndex < 0 || p_sourceVertexIndex >= p_sourceVertices.Count)
			{
				return -1;
			}

			int meshVertexIndex;
			if (p_sourceToMeshVertexIndex.TryGetValue(p_sourceVertexIndex, out meshVertexIndex))
			{
				return meshVertexIndex;
			}

			meshVertexIndex = p_mesh.Vertices.Count;
			TrackVertex vertex = AdapterCommon.CloneVertex(p_sourceVertices[p_sourceVertexIndex]);
			vertex.Metadata["SourceVertexIndex"] = p_sourceVertexIndex.ToString(CultureInfo.InvariantCulture);
			p_mesh.Vertices.Add(vertex);
			p_sourceToMeshVertexIndex[p_sourceVertexIndex] = meshVertexIndex;
			return meshVertexIndex;
		}

		private static int ResolveSourceVertexIndex(
			GDBMeshSegment p_segment,
			GDBMeshSegment p_previousSegment,
			int p_encodedVertexIndex,
			int p_totalVertexCount)
		{
			if (p_segment == null)
			{
				return p_encodedVertexIndex >= 0 && p_encodedVertexIndex < p_totalVertexCount ? p_encodedVertexIndex : -1;
			}

			if (p_segment.VertexLength <= 0)
			{
				return p_encodedVertexIndex >= 0 && p_encodedVertexIndex < p_totalVertexCount ? p_encodedVertexIndex : -1;
			}

			int adjustedIndex = p_encodedVertexIndex - p_segment.VertexOffset;
			if (adjustedIndex >= 0 && adjustedIndex < p_segment.VertexLength)
			{
				return p_segment.VertexStart + adjustedIndex;
			}

			if (p_previousSegment != null)
			{
				if (adjustedIndex >= p_segment.VertexLength)
				{
					return p_previousSegment.VertexStart + (p_encodedVertexIndex - p_previousSegment.VertexOffset);
				}

				if (adjustedIndex < 0)
				{
					return p_previousSegment.VertexStart + p_encodedVertexIndex;
				}
			}

			return p_encodedVertexIndex >= 0 && p_encodedVertexIndex < p_totalVertexCount ? p_encodedVertexIndex : -1;
		}

		private static string ResolveMaterialName(string[] p_materials, int p_materialId)
		{
			if (p_materials != null && p_materialId >= 0 && p_materialId < p_materials.Length)
			{
				return p_materials[p_materialId];
			}

			return string.Empty;
		}

		private static string ResolveMeshMaterialName(string[] p_materials, IList<GDBMeshSegment> p_segments)
		{
			HashSet<int> materialIds = new HashSet<int>();
			for (int i = 0; i < p_segments.Count; i++)
			{
				if (p_segments[i].MaterialId >= 0)
				{
					materialIds.Add(p_segments[i].MaterialId);
				}
			}

			if (materialIds.Count == 1)
			{
				foreach (int materialId in materialIds)
				{
					return ResolveMaterialName(p_materials, materialId);
				}
			}

			return string.Empty;
		}

		private static string GetVertexEncoding(GDB p_source)
		{
			if (p_source.VertexNormals != null && p_source.VertexNormals.Length > 0)
			{
				return "VertexNormal";
			}

			if (p_source.VertexColors != null && p_source.VertexColors.Length > 0)
			{
				return "VertexColor";
			}

			if (p_source.VertexUVs != null && p_source.VertexUVs.Length > 0)
			{
				return "VertexUV";
			}

			if (p_source.VertexPositions != null && p_source.VertexPositions.Length > 0)
			{
				return "VertexPosition";
			}

			return "None";
		}

		private static int GetVertexCount(GDB p_source)
		{
			if (p_source.VertexNormals != null && p_source.VertexNormals.Length > 0)
			{
				return p_source.VertexNormals.Length;
			}

			if (p_source.VertexColors != null && p_source.VertexColors.Length > 0)
			{
				return p_source.VertexColors.Length;
			}

			if (p_source.VertexUVs != null && p_source.VertexUVs.Length > 0)
			{
				return p_source.VertexUVs.Length;
			}

			if (p_source.VertexPositions != null && p_source.VertexPositions.Length > 0)
			{
				return p_source.VertexPositions.Length;
			}

			return 0;
		}

		private static void AddSegmentMetadata(Dictionary<string, string> p_metadata, GDBMeshSegment p_segment, int p_segmentIndex)
		{
			string prefix = string.Format(CultureInfo.InvariantCulture, "Segment[{0}]", p_segmentIndex);
			p_metadata[prefix + ".Offset"] = p_segment.Offset.ToString(CultureInfo.InvariantCulture);
			p_metadata[prefix + ".Length"] = p_segment.Length.ToString(CultureInfo.InvariantCulture);
			p_metadata[prefix + ".VertexStart"] = p_segment.VertexStart.ToString(CultureInfo.InvariantCulture);
			p_metadata[prefix + ".VertexLength"] = p_segment.VertexLength.ToString(CultureInfo.InvariantCulture);
			p_metadata[prefix + ".VertexOffset"] = p_segment.VertexOffset.ToString(CultureInfo.InvariantCulture);

			if (p_segment.MaterialId >= 0)
			{
				p_metadata[prefix + ".MaterialIndex"] = p_segment.MaterialId.ToString(CultureInfo.InvariantCulture);
			}

			if (p_segment.BoneMeta != null)
			{
				p_metadata[prefix + ".BoneId"] = p_segment.BoneMeta.BoneId.ToString(CultureInfo.InvariantCulture);
			}

			if (p_segment.Meta2F != null)
			{
				p_metadata[prefix + ".Meta2F"] = p_segment.Meta2F.Value.ToString(CultureInfo.InvariantCulture);
			}

			p_metadata[prefix + ".HasMeta30"] = p_segment.HasMeta30 ? "true" : "false";
		}

		private static bool IsCollisionMeshName(string p_name)
		{
			if (string.IsNullOrEmpty(p_name))
			{
				return false;
			}

			return p_name.IndexOf("collide", System.StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static string NormalizeMeshName(string p_name)
		{
			return string.IsNullOrEmpty(p_name) ? "GDB" : p_name.ToUpperInvariant();
		}

		private static void AddMetaStream(Dictionary<string, string> p_metadata, GDB_Meta[] p_meta)
		{
			GDB_Meta[] meta = p_meta ?? new GDB_Meta[0];
			p_metadata["Meta.Count"] = meta.Length.ToString(CultureInfo.InvariantCulture);
			for (int i = 0; i < meta.Length; i++)
			{
				string prefix = string.Format(CultureInfo.InvariantCulture, "Meta[{0}]", i);
				p_metadata[prefix + ".Type"] = meta[i].Type.ToString(CultureInfo.InvariantCulture);

				GDB_Meta_Material material = meta[i] as GDB_Meta_Material;
				if (material != null)
				{
					p_metadata[prefix + ".MaterialId"] = material.MaterialId.ToString(CultureInfo.InvariantCulture);
					continue;
				}

				GDB_Meta_Indices indices = meta[i] as GDB_Meta_Indices;
				if (indices != null)
				{
					p_metadata[prefix + ".Offset"] = indices.Offset.ToString(CultureInfo.InvariantCulture);
					p_metadata[prefix + ".Length"] = indices.Length.ToString(CultureInfo.InvariantCulture);
					continue;
				}

				GDB_Meta_Vertices vertices = meta[i] as GDB_Meta_Vertices;
				if (vertices != null)
				{
					p_metadata[prefix + ".UnknownByte"] = vertices.UnknownByte.ToString(CultureInfo.InvariantCulture);
					p_metadata[prefix + ".Offset"] = vertices.Offset.ToString(CultureInfo.InvariantCulture);
					p_metadata[prefix + ".Length"] = vertices.Length.ToString(CultureInfo.InvariantCulture);
					continue;
				}

				GDB_Meta_Bone bone = meta[i] as GDB_Meta_Bone;
				if (bone != null)
				{
					p_metadata[prefix + ".BoneId"] = bone.BoneId.ToString(CultureInfo.InvariantCulture);
					continue;
				}

				GDB_Meta_2F extra = meta[i] as GDB_Meta_2F;
				if (extra != null)
				{
					p_metadata[prefix + ".Value"] = extra.Value.ToString(CultureInfo.InvariantCulture);
					continue;
				}

				if (meta[i] is GDB_Meta_30)
				{
					p_metadata[prefix + ".Flag"] = "true";
				}
			}
		}

		private sealed class GDBMeshSegment
		{
			public int MaterialId = -1;
			public int Offset;
			public int Length;
			public int VertexStart;
			public int VertexLength;
			public int VertexOffset;
			public GDB_Meta_Vertices VertexMeta;
			public GDB_Meta_Bone BoneMeta;
			public GDB_Meta_2F Meta2F;
			public bool HasMeta30;
		}
	}
}


