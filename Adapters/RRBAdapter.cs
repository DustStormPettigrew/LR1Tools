using LR1Tools.Contracts;
using LibLR1;
using LibLR1.Utils;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace LR1Tools.Adapters
{
	public static class RRBAdapter
	{
		public static TrackScene ToScene(RRB p_source, string p_sceneName = null)
		{
			TrackScene scene = new TrackScene();
			scene.Name = string.IsNullOrEmpty(p_sceneName) ? "RRBScene" : p_sceneName;
			scene.SourceName = "RRB";

			TrackPath path = ToPath(p_source, scene.Name + ".Path");
			TrackMesh mesh = ToMesh(p_source, scene.Name + ".PathMesh");
			TrackObject pathObject = new TrackObject();

			pathObject.Name = scene.Name + ".PathObject";
			pathObject.MeshName = mesh.Name;
			pathObject.PathName = path.Name;
			pathObject.Metadata["SourceFormat"] = "RRB";

			scene.Paths.Add(path);
			scene.Meshes.Add(mesh);
			scene.Objects.Add(pathObject);

			AddSceneMetadata(scene.Metadata, p_source);
			return scene;
		}

		public static TrackPath ToPath(RRB p_source, string p_pathName = null)
		{
			TrackPath path = new TrackPath();
			path.Name = string.IsNullOrEmpty(p_pathName) ? "RRBPath" : p_pathName;
			path.Closed = false;

			List<TrackPathNode> nodes = BuildPathNodes(p_source);
			for (int i = 0; i < nodes.Count; i++)
			{
				path.Nodes.Add(nodes[i]);
			}

			AddSharedMetadata(path.Metadata, p_source);
			AdapterCommon.AddMetadata(path.Metadata, "SourceFormat", "RRB");
			return path;
		}

		public static TrackMesh ToMesh(RRB p_source, string p_meshName = null)
		{
			TrackMesh mesh = new TrackMesh();
			mesh.Name = string.IsNullOrEmpty(p_meshName) ? "RRBPathMesh" : p_meshName;
			mesh.IsCollisionMesh = false;

			List<TrackPathNode> nodes = BuildPathNodes(p_source);
			for (int i = 0; i < nodes.Count; i++)
			{
				TrackVertex vertex = new TrackVertex();
				vertex.Position = new Vector3(
					nodes[i].Position.X,
					nodes[i].Position.Y,
					nodes[i].Position.Z
				);
				vertex.Normal = new Vector3(
					nodes[i].Up.X,
					nodes[i].Up.Y,
					nodes[i].Up.Z
				);
				vertex.Metadata["PathNodeIndex"] = i.ToString();

				foreach (KeyValuePair<string, string> pair in nodes[i].Metadata)
				{
					vertex.Metadata["Node." + pair.Key] = pair.Value;
				}

				mesh.Vertices.Add(vertex);
			}

			AddSharedMetadata(mesh.Metadata, p_source);
			AdapterCommon.AddMetadata(mesh.Metadata, "SourceFormat", "RRB");
			AdapterCommon.AddMetadata(mesh.Metadata, "PrimitiveTopology", "LineStrip");
			return mesh;
		}

		private static void AddSceneMetadata(Dictionary<string, string> p_metadata, RRB p_source)
		{
			AddSharedMetadata(p_metadata, p_source);
			AdapterCommon.AddMetadata(p_metadata, "SourceFormat", "RRB");
			AdapterCommon.AddMetadata(p_metadata, "NodeCount", p_source != null && p_source.Nodes != null ? p_source.Nodes.Length : 0);
		}

		private static void AddSharedMetadata(Dictionary<string, string> p_metadata, RRB p_source)
		{
			if (p_source == null)
			{
				return;
			}

			AdapterCommon.AddMetadata(p_metadata, "Unknown28", AdapterCommon.FormatQuaternion(p_source.Unknown28));
			AdapterCommon.AddMetadata(p_metadata, "Unknown29", AdapterCommon.FormatVector3(p_source.Unknown29));
			AdapterCommon.AddMetadata(p_metadata, "Unknown2A", AdapterCommon.FormatVector3(p_source.Unknown2A));
			AdapterCommon.AddMetadata(p_metadata, "Unknown2B", AdapterCommon.FormatQuaternion(p_source.Unknown2B));
			AdapterCommon.AddMetadata(p_metadata, "Unknown2C", p_source.Unknown2C);
			AdapterCommon.AddMetadata(p_metadata, "Unknown2D", p_source.Unknown2D);
		}

		private static List<TrackPathNode> BuildPathNodes(RRB p_source)
		{
			List<TrackPathNode> nodes = new List<TrackPathNode>();
			Vector3 position = AdapterCommon.ToVector3(p_source != null ? p_source.Unknown29 : null);
			RRB_Node[] sourceNodes = p_source != null && p_source.Nodes != null ? p_source.Nodes : new RRB_Node[0];

			for (int i = 0; i < sourceNodes.Length; i++)
			{
				RRB_Node nativeNode = sourceNodes[i];
				LRQuaternion rotation = new LRQuaternion(
					GetAsFloat(nativeNode.RotX),
					GetAsFloat(nativeNode.RotY),
					GetAsFloat(nativeNode.RotZ),
					GetAsFloat(nativeNode.RotW)
				);

				position = new Vector3(
					position.X + GetAsFloat(nativeNode.DeltaX),
					position.Y + GetAsFloat(nativeNode.DeltaY),
					position.Z + GetAsFloat(nativeNode.DeltaZ)
				);

				TrackPathNode pathNode = new TrackPathNode();
				pathNode.Position = position;
				pathNode.Forward = AdapterCommon.RotateForward(rotation);
				pathNode.Up = AdapterCommon.RotateUp(rotation);
				pathNode.Metadata["NodeIndex"] = i.ToString();
				pathNode.Metadata["DeltaX"] = GetAsFloat(nativeNode.DeltaX).ToString("R", CultureInfo.InvariantCulture);
				pathNode.Metadata["DeltaY"] = GetAsFloat(nativeNode.DeltaY).ToString("R", CultureInfo.InvariantCulture);
				pathNode.Metadata["DeltaZ"] = GetAsFloat(nativeNode.DeltaZ).ToString("R", CultureInfo.InvariantCulture);
				pathNode.Metadata["RotationQuaternion"] = AdapterCommon.FormatQuaternion(rotation);
				pathNode.Metadata["Fract1"] = GetAsFloat(nativeNode.Fract1).ToString("R", CultureInfo.InvariantCulture);
				pathNode.Metadata["Fract2"] = GetAsFloat(nativeNode.Fract2).ToString("R", CultureInfo.InvariantCulture);
				pathNode.Metadata["UnknownTiming"] = nativeNode.UnknownTiming.ToString(CultureInfo.InvariantCulture);
				nodes.Add(pathNode);
			}

			return nodes;
		}

		private static float GetAsFloat(Fract8Bit p_value)
		{
			return p_value != null ? p_value.AsFloat : 0f;
		}

		private static float GetAsFloat(Fract16Bit p_value)
		{
			return p_value != null ? p_value.AsFloat : 0f;
		}
	}
}


