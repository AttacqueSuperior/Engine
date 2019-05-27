﻿#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.AS.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Projectiles
{
	[Desc("Not a sprite, but an engine effect.")]
	public class ElectricBoltInfo : IProjectileInfo
	{
		[Desc("The width of the zap.")]
		public readonly WDist Width = new WDist(12);

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ZOffset = 0;

		[Desc("The maximum duration (in ticks) of the beam's existence.")]
		public readonly int Duration = 5;

		[Desc("Colors of the zaps. The amount of zaps are the amount of colors listed here and PlayerColorZaps.")]
		public readonly Color[] Colors = {
			Color.FromArgb(80, 80, 255),
			Color.FromArgb(80, 80, 255),
			Color.FromArgb(255, 255, 255)
		};

		[Desc("Additional zaps colored with the player's color.")]
		public readonly int PlayerColorZaps = 0;

		[Desc("Distortion offset.")]
		public readonly int Distortion = 128;

		[Desc("Maximum length per segment.")]
		public readonly WDist SegmentLength = new WDist(320);

		[Desc("Image containing launch effect sequence.")]
		public readonly string LaunchEffectImage = null;

		[Desc("Launch effect sequence to play.")]
		[SequenceReference("LaunchEffectImage")]
		public readonly string LaunchEffectSequence = null;

		[Desc("Palette to use for launch effect.")]
		[PaletteReference]
		public readonly string LaunchEffectPalette = "effect";

		public IProjectile Create(ProjectileArgs args)
		{
			return new ElectricBolt(this, args);
		}
	}

	public class ElectricBolt : IProjectile, ISync
	{
		readonly ProjectileArgs args;
		readonly ElectricBoltInfo info;
		readonly WVec leftVector;
		readonly WVec upVector;
		readonly MersenneTwister random;
		readonly bool hasLaunchEffect;

		[Sync]
		readonly WPos target, source;

		int ticks = 0;

		HashSet<Pair<Color, WPos[]>> zaps;

		public ElectricBolt(ElectricBoltInfo info, ProjectileArgs args)
		{
			this.args = args;
			this.info = info;
			var playerColors = args.SourceActor.Owner.Color;
			var colors = info.Colors;
			for (int i = 0; i < info.PlayerColorZaps; i++)
				colors.Append(playerColors);

			target = args.PassiveTarget;
			source = args.Source;

			hasLaunchEffect = !string.IsNullOrEmpty(info.LaunchEffectImage) && !string.IsNullOrEmpty(info.LaunchEffectSequence);

			var direction = args.PassiveTarget - args.Source;

			if (info.Distortion != 0)
			{
				leftVector = new WVec(direction.Y, -direction.X, 0);
				if (leftVector.Length != 0)
					leftVector = 1024 * leftVector / leftVector.Length;

				upVector = new WVec(
					-direction.X * direction.Z,
					-direction.Z * direction.Y,
					direction.X * direction.X + direction.Y * direction.Y);
				if (upVector.Length != 0)
					upVector = 1024 * upVector / upVector.Length;

				random = args.SourceActor.World.SharedRandom;
			}

			zaps = new HashSet<Pair<Color, WPos[]>>();
			foreach (var c in colors)
			{
				var numSegments = (direction.Length - 1) / info.SegmentLength.Length + 1;
				var offsets = new WPos[numSegments + 1];
				offsets[0] = args.Source;
				offsets[offsets.Length - 1] = args.PassiveTarget;

				for (var i = 1; i < numSegments; i++)
				{
					var segmentStart = direction / numSegments * i;
					offsets[i] = args.Source + segmentStart;
				}

				zaps.Add(Pair.New(c, offsets));
			}
		}

		public void Tick(World world)
		{
			if (hasLaunchEffect && ticks == 0)
				world.AddFrameEndTask(w => w.Add(new SpriteEffect(args.CurrentSource, args.CurrentMuzzleFacing, world,
					info.LaunchEffectImage, info.LaunchEffectSequence, info.LaunchEffectPalette)));

			if (ticks == 0)
				args.Weapon.Impact(Target.FromPos(target), args.SourceActor, args.DamageModifiers);

			if (++ticks >= info.Duration)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (wr.World.FogObscures(target) &&
				wr.World.FogObscures(source))
				yield break;

			if (ticks < info.Duration)
			{
				foreach (var zap in zaps)
				{
					var offsets = zap.Second;
					for (var i = 1; i < offsets.Length - 1; i++)
					{
						var angle = WAngle.FromDegrees(random.Next(360));
						var distortion = random.Next(info.Distortion);

						var offset = distortion * angle.Cos() * leftVector / (1024 * 1024)
							+ distortion * angle.Sin() * upVector / (1024 * 1024);

						offsets[i] += offset;
					}

					yield return new ElectricBoltRenderable(offsets, info.ZOffset, info.Width, zap.First);
				}
			}
		}
	}
}
