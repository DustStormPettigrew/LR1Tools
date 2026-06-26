using LR1Tools.Contracts;
using LibLR1;
using LibLR1.Utils;
using System.Collections.Generic;
using System.Globalization;

namespace LR1Tools.Adapters
{
	public static class ADBAdapter
	{
		public static List<TrackAnimationClip> ToAnimationClips(ADB p_source, string p_namePrefix = null)
		{
			List<TrackAnimationClip> output = new List<TrackAnimationClip>();
			string namePrefix = string.IsNullOrEmpty(p_namePrefix) ? "ADBAnimation" : p_namePrefix;
			if (p_source == null || p_source.Animations == null || p_source.Animations.Count == 0)
			{
				return output;
			}

			List<KeyValuePair<string, ADB_Meta>> animationEntries = new List<KeyValuePair<string, ADB_Meta>>(p_source.Animations);
			ADB_Pointer[] pointers = p_source.Pointers ?? new ADB_Pointer[0];
			ADB_Data data = p_source.Data ?? new ADB_Data();
			LRVector3[] positionOffsets = data.PositionOffsets ?? new LRVector3[0];
			LRQuaternion[] transforms = data.Transforms ?? new LRQuaternion[0];
			int[] timeOffsets = data.TimeOffsets ?? new int[0];

			for (int animationIndex = 0; animationIndex < animationEntries.Count; animationIndex++)
			{
				KeyValuePair<string, ADB_Meta> entry = animationEntries[animationIndex];
				ADB_Meta sourceAnimation = entry.Value;
				string clipName = !string.IsNullOrWhiteSpace(entry.Key)
					? entry.Key
					: string.Format(CultureInfo.InvariantCulture, "{0}@Animation{1}", namePrefix, animationIndex);

				TrackAnimationClip clip = new TrackAnimationClip();
				clip.Name = clipName;
				clip.LoopMode = "Unknown";
				clip.FrameCount = null;
				clip.Speed = sourceAnimation != null ? sourceAnimation.Speed : 0f;
				AdapterCommon.SetAnimationClipProvenance(
					clip,
					"ADB",
					clipName,
					entry.Key,
					string.Format(CultureInfo.InvariantCulture, "{0}:{1}", namePrefix, clipName),
					animationIndex);

				if (sourceAnimation == null)
				{
					clip.Metadata["IsNullAnimation"] = "true";
					output.Add(clip);
					continue;
				}

				clip.Metadata["PointerTableOffset"] = sourceAnimation.PointerTableOffset.ToString(CultureInfo.InvariantCulture);
				clip.Metadata["Length"] = sourceAnimation.Length.ToString(CultureInfo.InvariantCulture);
				clip.Metadata["Length1"] = sourceAnimation.Length1.ToString(CultureInfo.InvariantCulture);
				clip.Metadata["Speed"] = sourceAnimation.Speed.ToString(CultureInfo.InvariantCulture);
				clip.Metadata["InitialPosition"] = AdapterCommon.FormatVector3(sourceAnimation.InitialPosition);
				clip.Metadata["InitialQuaternion"] = AdapterCommon.FormatQuaternion(sourceAnimation.InitialQuaternion);
				clip.Metadata["PointerCount"] = pointers.Length.ToString(CultureInfo.InvariantCulture);
				clip.Metadata["PositionOffsetCount"] = positionOffsets.Length.ToString(CultureInfo.InvariantCulture);
				clip.Metadata["TransformCount"] = transforms.Length.ToString(CultureInfo.InvariantCulture);
				clip.Metadata["TimeOffsetCount"] = timeOffsets.Length.ToString(CultureInfo.InvariantCulture);

				int pointerStart = sourceAnimation.PointerTableOffset;
				int pointerEnd = ResolvePointerEnd(animationEntries, animationIndex, pointers.Length);
				clip.Metadata["PointerTableEnd"] = pointerEnd.ToString(CultureInfo.InvariantCulture);

				if (pointerStart < 0 || pointerStart > pointers.Length)
				{
					clip.Metadata["PointerTableError"] = "PointerTableOffset is outside the available pointer table.";
					output.Add(clip);
					continue;
				}

				if (pointerEnd < pointerStart)
				{
					clip.Metadata["PointerTableError"] = "Resolved pointer table end precedes the start offset.";
					output.Add(clip);
					continue;
				}

				for (int pointerIndex = pointerStart; pointerIndex < pointerEnd; pointerIndex++)
				{
					ADB_Pointer pointer = pointers[pointerIndex];
					int localPointerIndex = pointerIndex - pointerStart;
					string targetName = string.Format(CultureInfo.InvariantCulture, "Pointer[{0}]", localPointerIndex);

					if (pointer == null)
					{
						clip.Metadata[string.Format(CultureInfo.InvariantCulture, "Pointer[{0}].IsNull", localPointerIndex)] = "true";
						continue;
					}

					if (pointer.PositionLength > 0 || pointer.PositionOffset != 0 || pointer.PositionTimeOffset != 0)
					{
						TrackAnimationChannel channel = CreateChannel(
							clip,
							localPointerIndex,
							targetName,
							"PositionOffset",
							"Vector3",
							"Unknown");

						PopulateCommonPointerMetadata(channel.Metadata, pointer);
						channel.Metadata["PointerType"] = "Position";
						AddVector3Keyframes(
							channel,
							positionOffsets,
							timeOffsets,
							pointer.PositionOffset,
							pointer.PositionTimeOffset,
							pointer.PositionLength);
						clip.Channels.Add(channel);
					}

					if (pointer.TransformLength > 0 || pointer.TransformOffset != 0 || pointer.TransformTimeOffset != 0)
					{
						TrackAnimationChannel channel = CreateChannel(
							clip,
							localPointerIndex,
							targetName,
							"TransformQuaternion",
							"Quaternion",
							"Unknown");

						PopulateCommonPointerMetadata(channel.Metadata, pointer);
						channel.Metadata["PointerType"] = "Transform";
						AddQuaternionKeyframes(
							channel,
							transforms,
							timeOffsets,
							pointer.TransformOffset,
							pointer.TransformTimeOffset,
							pointer.TransformLength);
						clip.Channels.Add(channel);
					}
				}

				output.Add(clip);
			}

			return output;
		}

		private static int ResolvePointerEnd(IList<KeyValuePair<string, ADB_Meta>> p_entries, int p_animationIndex, int p_pointerCount)
		{
			ADB_Meta current = p_entries[p_animationIndex].Value;
			int currentStart = current != null ? current.PointerTableOffset : -1;
			int bestEnd = p_pointerCount;

			for (int i = 0; i < p_entries.Count; i++)
			{
				if (i == p_animationIndex)
				{
					continue;
				}

				ADB_Meta next = p_entries[i].Value;
				if (next == null || next.PointerTableOffset < 0 || next.PointerTableOffset <= currentStart)
				{
					continue;
				}

				if (next.PointerTableOffset < bestEnd)
				{
					bestEnd = next.PointerTableOffset;
				}
			}

			return bestEnd <= p_pointerCount ? bestEnd : p_pointerCount;
		}

		private static TrackAnimationChannel CreateChannel(
			TrackAnimationClip p_clip,
			int p_localPointerIndex,
			string p_targetName,
			string p_property,
			string p_valueType,
			string p_interpolation)
		{
			TrackAnimationChannel channel = new TrackAnimationChannel();
			channel.Name = string.Format(CultureInfo.InvariantCulture, "{0}:{1}:{2}", p_clip.Name, p_targetName, p_property);
			channel.Property = p_property;
			channel.ValueType = p_valueType;
			channel.Interpolation = p_interpolation;
			AdapterCommon.SetAnimationChannelProvenance(
				channel,
				"ADB",
				channel.Name,
				p_clip.Name,
				string.Format(CultureInfo.InvariantCulture, "{0}:channel:{1}:{2}", p_clip.SourceId, p_localPointerIndex, p_property),
				p_localPointerIndex);

			channel.Target = new TrackAnimationTarget();
			channel.Target.Name = p_targetName;
			channel.Target.Type = "Pointer";
			channel.Target.Path = p_targetName;
			channel.Target.Slot = p_property;
			AdapterCommon.SetAnimationTargetProvenance(
				channel.Target,
				"ADB",
				channel.Name + ":Target",
				p_targetName,
				string.Format(CultureInfo.InvariantCulture, "{0}:target", channel.SourceId),
				p_localPointerIndex);
			return channel;
		}

		private static void PopulateCommonPointerMetadata(Dictionary<string, string> p_metadata, ADB_Pointer p_pointer)
		{
			p_metadata["TransformOffset"] = p_pointer.TransformOffset.ToString(CultureInfo.InvariantCulture);
			p_metadata["TransformTimeOffset"] = p_pointer.TransformTimeOffset.ToString(CultureInfo.InvariantCulture);
			p_metadata["TransformLength"] = p_pointer.TransformLength.ToString(CultureInfo.InvariantCulture);
			p_metadata["PositionOffset"] = p_pointer.PositionOffset.ToString(CultureInfo.InvariantCulture);
			p_metadata["PositionTimeOffset"] = p_pointer.PositionTimeOffset.ToString(CultureInfo.InvariantCulture);
			p_metadata["PositionLength"] = p_pointer.PositionLength.ToString(CultureInfo.InvariantCulture);
		}

		private static void AddVector3Keyframes(
			TrackAnimationChannel p_channel,
			LRVector3[] p_values,
			int[] p_timeOffsets,
			int p_valueOffset,
			int p_timeOffset,
			int p_length)
		{
			AddSliceMetadata(p_channel.Metadata, p_values.Length, p_timeOffsets.Length, p_valueOffset, p_timeOffset, p_length);
			if (p_length <= 0)
			{
				return;
			}

			for (int i = 0; i < p_length; i++)
			{
				int valueIndex = p_valueOffset + i;
				if (valueIndex < 0 || valueIndex >= p_values.Length)
				{
					p_channel.Metadata[string.Format(CultureInfo.InvariantCulture, "MissingValue[{0}]", i)] = valueIndex.ToString(CultureInfo.InvariantCulture);
					continue;
				}

				TrackAnimationKeyframe keyframe = new TrackAnimationKeyframe();
				keyframe.HasVector3Value = true;
				keyframe.Vector3Value = AdapterCommon.ToVector3(p_values[valueIndex]);
				keyframe.Metadata["ValueIndex"] = valueIndex.ToString(CultureInfo.InvariantCulture);
				TryApplyTimeOffset(keyframe, p_channel.Metadata, p_timeOffsets, p_timeOffset + i, i);
				AdapterCommon.SetAnimationKeyframeProvenance(keyframe, "ADB", p_channel.Target != null ? p_channel.Target.Name : null, valueIndex);
				p_channel.Keyframes.Add(keyframe);
			}
		}

		private static void AddQuaternionKeyframes(
			TrackAnimationChannel p_channel,
			LRQuaternion[] p_values,
			int[] p_timeOffsets,
			int p_valueOffset,
			int p_timeOffset,
			int p_length)
		{
			AddSliceMetadata(p_channel.Metadata, p_values.Length, p_timeOffsets.Length, p_valueOffset, p_timeOffset, p_length);
			if (p_length <= 0)
			{
				return;
			}

			for (int i = 0; i < p_length; i++)
			{
				int valueIndex = p_valueOffset + i;
				if (valueIndex < 0 || valueIndex >= p_values.Length)
				{
					p_channel.Metadata[string.Format(CultureInfo.InvariantCulture, "MissingValue[{0}]", i)] = valueIndex.ToString(CultureInfo.InvariantCulture);
					continue;
				}

				TrackAnimationKeyframe keyframe = new TrackAnimationKeyframe();
				keyframe.HasQuaternionValue = true;
				keyframe.QuaternionValue = AdapterCommon.ToQuaternion(p_values[valueIndex]);
				keyframe.Metadata["ValueIndex"] = valueIndex.ToString(CultureInfo.InvariantCulture);
				TryApplyTimeOffset(keyframe, p_channel.Metadata, p_timeOffsets, p_timeOffset + i, i);
				AdapterCommon.SetAnimationKeyframeProvenance(keyframe, "ADB", p_channel.Target != null ? p_channel.Target.Name : null, valueIndex);
				p_channel.Keyframes.Add(keyframe);
			}
		}

		private static void AddSliceMetadata(Dictionary<string, string> p_metadata, int p_valueCount, int p_timeCount, int p_valueOffset, int p_timeOffset, int p_length)
		{
			p_metadata["SliceValueOffset"] = p_valueOffset.ToString(CultureInfo.InvariantCulture);
			p_metadata["SliceTimeOffset"] = p_timeOffset.ToString(CultureInfo.InvariantCulture);
			p_metadata["SliceLength"] = p_length.ToString(CultureInfo.InvariantCulture);
			p_metadata["ValueCount"] = p_valueCount.ToString(CultureInfo.InvariantCulture);
			p_metadata["TimeOffsetCount"] = p_timeCount.ToString(CultureInfo.InvariantCulture);
		}

		private static void TryApplyTimeOffset(
			TrackAnimationKeyframe p_keyframe,
			Dictionary<string, string> p_channelMetadata,
			int[] p_timeOffsets,
			int p_timeIndex,
			int p_sequenceIndex)
		{
			p_keyframe.Metadata["SequenceIndex"] = p_sequenceIndex.ToString(CultureInfo.InvariantCulture);
			if (p_timeIndex < 0 || p_timeIndex >= p_timeOffsets.Length)
			{
				p_keyframe.Metadata["MissingTimeOffsetIndex"] = p_timeIndex.ToString(CultureInfo.InvariantCulture);
				p_channelMetadata[string.Format(CultureInfo.InvariantCulture, "MissingTimeOffset[{0}]", p_sequenceIndex)] = p_timeIndex.ToString(CultureInfo.InvariantCulture);
				return;
			}

			p_keyframe.Time = p_timeOffsets[p_timeIndex];
			p_keyframe.Metadata["TimeOffsetIndex"] = p_timeIndex.ToString(CultureInfo.InvariantCulture);
			p_keyframe.Metadata["TimeOffset"] = p_timeOffsets[p_timeIndex].ToString(CultureInfo.InvariantCulture);
		}
	}
}
