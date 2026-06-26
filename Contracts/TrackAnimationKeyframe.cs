using System.Collections.Generic;
using System.Numerics;

namespace LR1Tools.Contracts
{
	public class TrackAnimationKeyframe
	{
		public string SourceName { get; set; }
		public string SourceFormat { get; set; }
		public string SourcePath { get; set; }
		public int? SourceIndex { get; set; }
		public int? FrameIndex { get; set; }
		public float? Time { get; set; }
		public int? IntValue { get; set; }
		public float? FloatValue { get; set; }
		public bool HasStringValue { get; set; }
		public string StringValue { get; set; }
		public bool HasVector2Value { get; set; }
		public Vector2 Vector2Value { get; set; }
		public bool HasVector3Value { get; set; }
		public Vector3 Vector3Value { get; set; }
		public bool HasQuaternionValue { get; set; }
		public Quaternion QuaternionValue { get; set; }
		public Dictionary<string, string> Metadata { get; private set; }

		public TrackAnimationKeyframe()
		{
			SourceName = string.Empty;
			SourceFormat = string.Empty;
			SourcePath = string.Empty;
			SourceIndex = null;
			FrameIndex = null;
			Time = null;
			IntValue = null;
			FloatValue = null;
			HasStringValue = false;
			StringValue = string.Empty;
			HasVector2Value = false;
			Vector2Value = Vector2.Zero;
			HasVector3Value = false;
			Vector3Value = Vector3.Zero;
			HasQuaternionValue = false;
			QuaternionValue = Quaternion.Identity;
			Metadata = new Dictionary<string, string>();
		}
	}
}
