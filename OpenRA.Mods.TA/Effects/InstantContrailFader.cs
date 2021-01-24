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
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.TA.Graphics;

namespace OpenRA.Mods.TA.Effects
{
	public class InstantContrailFader : IEffect
	{
		WPos pos;
		InstantContrailRenderable trail;
		int ticks;

		public InstantContrailFader(WPos pos, InstantContrailRenderable trail)
		{
			this.pos = pos;
			this.trail = trail;
		}

		public void Tick(World world)
		{
			if (ticks++ == trail.Length)
				world.AddFrameEndTask(w => w.Remove(this));

			trail.Update(pos);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			yield return trail;
		}
	}
}
