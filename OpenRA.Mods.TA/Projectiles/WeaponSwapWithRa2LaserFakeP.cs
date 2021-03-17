#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.TA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.TA.Projectiles
{
	public class WeaponSwapWithRa2LaserFakePInfo : WeaponSwapFakePInfo
	{
		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ZOffset = 0;

		[Desc("The width of the zap.")]
		public readonly WDist Width = new WDist(70);

		[Desc("The center width of the zap.")]
		public readonly WDist InnerWidth = new WDist(0);

		[Desc("The center color of the zap.")]
		public readonly Color InnerColor = Color.FromAhsl(255, 0f, 0.5f, 1f);

		[Desc("The color of the zap.")]
		public readonly Color OuterColor = Color.FromAhsl(255, 0f, 1f, 1f);

		[Desc("Use player color. If this item is set to true, InnerColor and OuterColor will be ignored.")]
		public readonly bool UsePlayerColor = true;

		public override IProjectile Create(ProjectileArgs args)
		{
			return new WeaponSwapWithRa2LaserFakeP(this, args);
		}
	}

	public class WeaponSwapWithRa2LaserFakeP : WeaponSwapFakeP
	{
		private int duration;
		private int zOffset;
		private WDist width;
		private WDist innerWidth;
		private Color innerColor;
		private Color outerColor;
		private WPos source;
		private WPos target;
		private int ticks = 0;
		private WVec swapFirstOffset;
		private WVec swapStepOffset;
		public WeaponSwapWithRa2LaserFakeP(WeaponSwapWithRa2LaserFakePInfo info, ProjectileArgs args)
			: base(info, args)
		{
			if (info.UsePlayerColor)
			{
				int a;
				float h, s, v;
				args.SourceActor.Owner.Color.ToAhsv(out a, out h, out s, out v);
				innerColor = Color.FromAhsv(255, h, s / 1.5f, v);
				outerColor = Color.FromAhsv(255, h, s, v);
			}
			else
			{
				innerColor = info.InnerColor;
				outerColor = info.OuterColor;
			}

			var yawFormTargetToSelf = WRot.FromYaw((args.PassiveTarget - args.Source).Yaw);
			swapFirstOffset = info.FirstBurstTargetOffset.Rotate(yawFormTargetToSelf);
			swapStepOffset = ((info.LastBurstTargetOffset - info.FirstBurstTargetOffset) / info.Bursts).Rotate(yawFormTargetToSelf);

			duration = info.SwapDuration;
			width = info.Width;
			innerWidth = info.InnerWidth;
			zOffset = info.ZOffset;
			source = args.Source;
		}

		public override void Tick(World world)
		{
			base.Tick(world);
			target = Args.PassiveTarget + swapFirstOffset + swapStepOffset * ticks;
			++ticks;
		}

		public override IEnumerable<IRenderable> Render(WorldRenderer r)
		{
			if (duration == 2 || ticks < 2)
			{
				var irc = Color.FromArgb(255, innerColor.R, innerColor.G, innerColor.B / duration);
				var orc = Color.FromArgb(255, outerColor.R, outerColor.G, outerColor.B / duration);
				yield return new Ra2LaserRenderable(source, zOffset, target - source, width, innerWidth, irc, orc);
			}
			else if (ticks < duration)
			{
				var diffTick = duration - ticks;
				var irc = Color.FromArgb(255, diffTick * innerColor.R / duration, diffTick * innerColor.G / duration, diffTick * innerColor.B / duration);
				var orc = Color.FromArgb(255, diffTick * outerColor.R / duration, diffTick * outerColor.G / duration, diffTick * outerColor.B / duration);
				yield return new Ra2LaserRenderable(source, zOffset, target - source, diffTick * width / duration, diffTick * innerWidth / duration, irc, orc);
			}
		}
	}
}
