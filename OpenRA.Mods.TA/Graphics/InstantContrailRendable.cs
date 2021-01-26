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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.TA.Graphics
{
	public struct InstantContrailRenderable : IRenderable, IFinalizedRenderable
	{
		public int Length { get { return trail.Length; } }

		readonly World world;
		readonly Color color;
		readonly int zOffset;

		// Store trail positions in a circular buffer
		readonly WPos[] trail;
		readonly WDist width;
		int next;
		int length;
		int skip;

		public InstantContrailRenderable(World world, Color color, WDist width, int length, int skip, int zOffset)
			: this(world, new WPos[length], width, 0, 0, skip, color, zOffset) { }

		InstantContrailRenderable(World world, WPos[] trail, WDist width, int next, int length, int skip, Color color, int zOffset)
		{
			this.world = world;
			this.trail = trail;
			this.width = width;
			this.next = next;
			this.length = length;
			this.skip = skip;
			this.color = color;
			this.zOffset = zOffset;
		}

		public WPos Pos { get { return trail[Index(next - 1)]; } }
		public PaletteReference Palette { get { return null; } }
		public int ZOffset { get { return zOffset; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithPalette(PaletteReference newPalette) { return new InstantContrailRenderable(world, (WPos[])trail.Clone(), width, next, length, skip, color, zOffset); }
		public IRenderable WithZOffset(int newOffset) { return new InstantContrailRenderable(world, (WPos[])trail.Clone(), width, next, length, skip, color, newOffset); }
		public IRenderable OffsetBy(WVec vec) { return new InstantContrailRenderable(world, trail.Select(pos => pos + vec).ToArray(), width, next, length, skip, color, zOffset); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			var renderLength = length - skip;
			if (renderLength <= 0)
				return;

			var screenWidth = wr.ScreenVector(new WVec(width, WDist.Zero, WDist.Zero))[0];
			var wcr = Game.Renderer.WorldRgbaColorRenderer;

			// Start of the first line segment is the tail of the list - don't smooth it.
			var curPos = trail[Index(next - skip - 1)];
			var curColor = color;

			for (var i = 1; i < renderLength; i++)
			{
				var j = next - skip - 1 - i;
				WPos nextPos;

				// Smooth the contrail to tail direction, use 4 position for max smooth effect.
				if (i < renderLength - 3)
					nextPos = Average(trail[Index(j)], trail[Index(j - 1)], trail[Index(j - 2)], trail[Index(j - 3)]);
				else
				{
					var smoothparams = new List<WPos>();
					for (int k = 0; k < renderLength - i; k++)
						smoothparams.Add(trail[Index(j - k)]);
					nextPos = Average(smoothparams.ToArray());
				}

				var nextColor = Color.FromArgb((renderLength - i) * 1000 / renderLength * color.A / 1000, color); // Color.Transparent

				if (!world.FogObscures(curPos) && !world.FogObscures(nextPos))
					wcr.DrawLine(wr.Screen3DPosition(curPos), wr.Screen3DPosition(nextPos), screenWidth, curColor, nextColor);

				curPos = nextPos;
				curColor = nextColor;
			}
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }

		// Array index modulo length
		int Index(int i)
		{
			var j = i % trail.Length;
			return j < 0 ? j + trail.Length : j;
		}

		static WPos Average(params WPos[] list)
		{
			return list.Average();
		}

		public void Update(WPos pos)
		{
			trail[next] = pos;
			next = Index(next + 1);

			if (length < trail.Length)
				length++;
		}

		public static Color ChooseColor(Actor self)
		{
			var ownerColor = Color.FromArgb(255, self.Owner.Color);
			return Exts.ColorLerp(0.5f, ownerColor, Color.White);
		}
	}
}
