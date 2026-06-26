using LR1Tools.Contracts;
using LibLR1;
using System.Collections.Generic;
using System.Globalization;

namespace LR1Tools.Adapters
{
	public static class MABAdapter
	{
		public static List<TrackAnimationClip> ToAnimationClips(MAB p_source, string p_namePrefix = null)
		{
			List<TrackAnimationClip> output = new List<TrackAnimationClip>();
			string namePrefix = string.IsNullOrEmpty(p_namePrefix) ? "MABAnimation" : p_namePrefix;
			if (p_source == null || p_source.Animations == null)
			{
				return output;
			}

			MAB_MaterialFrame[] materialFrames = p_source.MaterialFrames ?? new MAB_MaterialFrame[0];
			for (int animationIndex = 0; animationIndex < p_source.Animations.Length; animationIndex++)
			{
				MAB_Animation sourceAnimation = p_source.Animations[animationIndex];
				TrackAnimationClip clip = new TrackAnimationClip();
				clip.Name = string.Format(CultureInfo.InvariantCulture, "{0}@Animation{1}", namePrefix, animationIndex);
				clip.LoopMode = "Unknown";
				clip.FrameCount = sourceAnimation != null ? (int?)sourceAnimation.Frames : null;
				clip.Speed = sourceAnimation != null ? sourceAnimation.Speed : 0f;
				AdapterCommon.SetAnimationClipProvenance(
					clip,
					"MAB",
					clip.Name,
					namePrefix,
					string.Format(CultureInfo.InvariantCulture, "{0}:{1}", namePrefix, animationIndex),
					animationIndex);

				if (sourceAnimation == null)
				{
					clip.Metadata["IsNullAnimation"] = "true";
					output.Add(clip);
					continue;
				}

				clip.Metadata["AnimationOffset"] = sourceAnimation.AnimationOffset.ToString(CultureInfo.InvariantCulture);
				clip.Metadata["AnimationLength"] = sourceAnimation.AnimationLength.ToString(CultureInfo.InvariantCulture);
				clip.Metadata["Frames"] = sourceAnimation.Frames.ToString(CultureInfo.InvariantCulture);
				clip.Metadata["Speed"] = sourceAnimation.Speed.ToString(CultureInfo.InvariantCulture);

				TrackAnimationChannel channel = new TrackAnimationChannel();
				channel.Name = clip.Name + ":MaterialFrame";
				channel.Property = "MaterialFrameIndex";
				channel.ValueType = "Int32";
				channel.Interpolation = "Step";
				AdapterCommon.SetAnimationChannelProvenance(
					channel,
					"MAB",
					channel.Name,
					clip.Name,
					string.Format(CultureInfo.InvariantCulture, "{0}:channel:materialFrame", clip.SourceId),
					0);

				channel.Target = new TrackAnimationTarget();
				channel.Target.Name = string.Empty;
				channel.Target.Type = "Material";
				channel.Target.Path = string.Empty;
				channel.Target.Slot = "MaterialFrame";
				AdapterCommon.SetAnimationTargetProvenance(
					channel.Target,
					"MAB",
					channel.Name + ":Target",
					clip.Name,
					string.Format(CultureInfo.InvariantCulture, "{0}:target", channel.SourceId),
					animationIndex);

				string fixedMaterialName = null;
				bool hasMultipleMaterialTargets = false;

				for (int sequenceIndex = 0; sequenceIndex < sourceAnimation.AnimationLength; sequenceIndex++)
				{
					int sourceFrameIndex = sourceAnimation.AnimationOffset + sequenceIndex;
					if (sourceFrameIndex < 0 || sourceFrameIndex >= materialFrames.Length)
					{
						channel.Metadata[string.Format(CultureInfo.InvariantCulture, "MissingMaterialFrame[{0}]", sequenceIndex)] =
							sourceFrameIndex.ToString(CultureInfo.InvariantCulture);
						continue;
					}

					MAB_MaterialFrame sourceFrame = materialFrames[sourceFrameIndex];
					TrackAnimationKeyframe keyframe = new TrackAnimationKeyframe();
					keyframe.FrameIndex = sequenceIndex;
					keyframe.IntValue = sourceFrame != null ? (int?)sourceFrame.Frame : null;
					keyframe.HasStringValue = sourceFrame != null && sourceFrame.Material != null;
					keyframe.StringValue = sourceFrame != null && sourceFrame.Material != null ? sourceFrame.Material : string.Empty;
					keyframe.Metadata["SequenceIndex"] = sequenceIndex.ToString(CultureInfo.InvariantCulture);
					keyframe.Metadata["SourceMaterialFrameIndex"] = sourceFrameIndex.ToString(CultureInfo.InvariantCulture);
					AdapterCommon.SetAnimationKeyframeProvenance(
						keyframe,
						"MAB",
						sourceFrame != null ? sourceFrame.Material : null,
						sourceFrameIndex);
					channel.Keyframes.Add(keyframe);

					string materialName = sourceFrame != null ? sourceFrame.Material : null;
					if (!string.IsNullOrWhiteSpace(materialName))
					{
						if (string.IsNullOrEmpty(fixedMaterialName))
						{
							fixedMaterialName = materialName;
						}
						else if (!string.Equals(fixedMaterialName, materialName, System.StringComparison.OrdinalIgnoreCase))
						{
							hasMultipleMaterialTargets = true;
						}
					}
				}

				if (hasMultipleMaterialTargets)
				{
					channel.Metadata["HasVariableMaterialTargets"] = "true";
				}
				else if (!string.IsNullOrWhiteSpace(fixedMaterialName))
				{
					channel.Target.Name = fixedMaterialName;
					channel.Target.Path = fixedMaterialName;
				}

				clip.Channels.Add(channel);
				output.Add(clip);
			}

			return output;
		}

		public static List<TrackMaterialAnimation> ToMaterialAnimations(MAB p_source, string p_namePrefix = null)
		{
			List<TrackMaterialAnimation> output = new List<TrackMaterialAnimation>();
			string namePrefix = string.IsNullOrEmpty(p_namePrefix) ? "MABAnimation" : p_namePrefix;
			if (p_source == null || p_source.Animations == null)
			{
				return output;
			}

			MAB_MaterialFrame[] materialFrames = p_source.MaterialFrames ?? new MAB_MaterialFrame[0];
			for (int animationIndex = 0; animationIndex < p_source.Animations.Length; animationIndex++)
			{
				MAB_Animation sourceAnimation = p_source.Animations[animationIndex];
				TrackMaterialAnimation animation = new TrackMaterialAnimation();
				animation.Name = string.Format(CultureInfo.InvariantCulture, "{0}@Animation{1}", namePrefix, animationIndex);
				animation.Behavior = TrackMaterialAnimationBehaviors.FrameSwap;
				animation.LoopMode = "Unknown";
				animation.FrameCount = sourceAnimation != null ? (int?)sourceAnimation.Frames : null;
				animation.Speed = sourceAnimation != null ? sourceAnimation.Speed : 0f;
				AdapterCommon.SetMaterialAnimationProvenance(
					animation,
					"MAB",
					animation.Name,
					namePrefix,
					string.Format(CultureInfo.InvariantCulture, "{0}:{1}", namePrefix, animationIndex),
					animationIndex);

				if (sourceAnimation == null)
				{
					animation.Metadata["IsNullAnimation"] = "true";
					output.Add(animation);
					continue;
				}

				animation.Metadata["AnimationOffset"] = sourceAnimation.AnimationOffset.ToString(CultureInfo.InvariantCulture);
				animation.Metadata["AnimationLength"] = sourceAnimation.AnimationLength.ToString(CultureInfo.InvariantCulture);
				animation.Metadata["Frames"] = sourceAnimation.Frames.ToString(CultureInfo.InvariantCulture);
				animation.Metadata["Speed"] = sourceAnimation.Speed.ToString(CultureInfo.InvariantCulture);

				for (int sequenceIndex = 0; sequenceIndex < sourceAnimation.AnimationLength; sequenceIndex++)
				{
					int sourceFrameIndex = sourceAnimation.AnimationOffset + sequenceIndex;
					if (sourceFrameIndex < 0 || sourceFrameIndex >= materialFrames.Length)
					{
						animation.Metadata[string.Format(CultureInfo.InvariantCulture, "MissingMaterialFrame[{0}]", sequenceIndex)] =
							sourceFrameIndex.ToString(CultureInfo.InvariantCulture);
						continue;
					}

					MAB_MaterialFrame sourceFrame = materialFrames[sourceFrameIndex];
					TrackMaterialAnimationFrame frame = new TrackMaterialAnimationFrame();
					frame.MaterialName = sourceFrame != null && sourceFrame.Material != null ? sourceFrame.Material : string.Empty;
					frame.FrameIndex = sourceFrame != null ? sourceFrame.Frame : 0;
					frame.Metadata["SequenceIndex"] = sequenceIndex.ToString(CultureInfo.InvariantCulture);
					frame.Metadata["SourceMaterialFrameIndex"] = sourceFrameIndex.ToString(CultureInfo.InvariantCulture);
					animation.Frames.Add(frame);

					if (string.IsNullOrEmpty(animation.MaterialName) && !string.IsNullOrEmpty(frame.MaterialName))
					{
						animation.MaterialName = frame.MaterialName;
					}
				}

				output.Add(animation);
			}

			return output;
		}
	}
}
