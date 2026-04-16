using LR1Tools.Contracts;
using LibLR1;
using System.Collections.Generic;
using System.Globalization;

namespace LR1Tools.Adapters
{
	public static class MABAdapter
	{
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
