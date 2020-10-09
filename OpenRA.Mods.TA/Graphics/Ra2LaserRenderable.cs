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

using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Graphics
{
	public struct Ra2LaserRenderable : IRenderable, IFinalizedRenderable
	{
		readonly WPos pos;
		readonly int zOffset;
		readonly WVec length;
		readonly WDist width;
		readonly WDist innerWidth;
		readonly Color innerColor;
		readonly Color outerColor;

		public Ra2LaserRenderable(WPos pos, int zOffset, WVec length, WDist width, WDist innerWidth, Color innerColor, Color outerColor)
		{
			this.pos = pos;
			this.zOffset = zOffset;
			this.length = length;
			this.width = width;
			this.innerWidth = innerWidth;
			this.innerColor = innerColor;
			this.outerColor = outerColor;
		}

		public WPos Pos { get { return pos; } }
		public PaletteReference Palette { get { return null; } }
		public int ZOffset { get { return zOffset; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithPalette(PaletteReference newPalette) { return new Ra2LaserRenderable(pos, zOffset, length, width, innerWidth, innerColor, outerColor); }
		public IRenderable WithZOffset(int newOffset) { return new Ra2LaserRenderable(pos, zOffset, length, width, innerWidth, innerColor, outerColor); }
		public IRenderable OffsetBy(WVec vec) { return new Ra2LaserRenderable(pos + vec, zOffset, length, width, innerWidth, innerColor, outerColor); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			var vecLength = length.Length;
			if (vecLength == 0)
				return;

			var start = wr.Screen3DPosition(pos);
			var end = wr.Screen3DPosition(pos + length);
			var screenInnerWidth = wr.ScreenVector(new WVec(innerWidth, WDist.Zero, WDist.Zero))[0];
			var screenWidth = wr.ScreenVector(new WVec(width, WDist.Zero, WDist.Zero))[0];

			var iColor = innerColor;
			var oColor = outerColor;
			var ooColor = Color.FromArgb(0, outerColor);
			var delta = (end - start) / (end - start).XY.Length;
			var innerCorner = screenInnerWidth / 2 * new float3(-delta.Y, delta.X, delta.Z);
			var corner = screenWidth / 2 * new float3(-delta.Y, delta.X, delta.Z);
			Game.Renderer.WorldRgbaColorRenderer.FillRect(start + corner, start + innerCorner, end + innerCorner, end + corner, ooColor, oColor, oColor, ooColor, BlendMode.Additive);
			Game.Renderer.WorldRgbaColorRenderer.FillRect(start, start + innerCorner, end + innerCorner, end, iColor, oColor, oColor, iColor, BlendMode.Additive);
			Game.Renderer.WorldRgbaColorRenderer.FillRect(start - innerCorner, start, end, end - innerCorner, oColor, iColor, iColor, oColor, BlendMode.Additive);
			Game.Renderer.WorldRgbaColorRenderer.FillRect(start - innerCorner, start - corner, end - corner, end - innerCorner, oColor, ooColor, ooColor, oColor, BlendMode.Additive);
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
