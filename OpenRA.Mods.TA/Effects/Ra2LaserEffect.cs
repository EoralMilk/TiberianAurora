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

using System;
using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.TA.Effects
{
	class Ra2LaserEffect : IEffect
	{
		private readonly int duration;
		private readonly int zOffset;
		private readonly WDist width;
		private readonly WDist innerWidth;
		private readonly Color innerColor;
		private readonly Color outerColor;
		private readonly Func<WPos> source;
		private readonly Func<WPos> target;

		private int ticks = 0;

		public Ra2LaserEffect(int duration, WDist width, WDist innerWidth, Color innerColor, Color outerColor, Func<WPos> source, Func<WPos> target, int zOffset = 0)
		{
			this.duration = duration > 1 ? duration : 2;
			this.width = width;
			this.innerWidth = innerWidth;
			this.innerColor = innerColor;
			this.outerColor = outerColor;
			this.source = source;
			this.target = target;
			this.zOffset = zOffset;
		}

		public void Tick(World world)
		{
			if (++ticks >= duration)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer r)
		{
			if (duration == 2 || ticks < 2)
			{
				var irc = Color.FromArgb(255, innerColor.R, innerColor.G, innerColor.B / duration);
				var orc = Color.FromArgb(255, outerColor.R, outerColor.G, outerColor.B / duration);
				yield return new Ra2LaserRenderable(source(), zOffset, target() - source(), width, innerWidth, irc, orc);
			}
			else if (ticks < duration)
			{
				var diffTick = duration - ticks;
				var irc = Color.FromArgb(255, diffTick * innerColor.R / duration, diffTick * innerColor.G / duration, diffTick * innerColor.B / duration);
				var orc = Color.FromArgb(255, diffTick * outerColor.R / duration, diffTick * outerColor.G / duration, diffTick * outerColor.B / duration);
				yield return new Ra2LaserRenderable(source(), zOffset, target() - source(), diffTick * width / duration, diffTick * innerWidth / duration, irc, orc);
			}
		}
	}
}
