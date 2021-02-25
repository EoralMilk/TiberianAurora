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
using System.IO;
using OpenRA.Graphics;
using OpenRA.Mods.TA.FileFormats;
using OpenRA.Primitives;

namespace OpenRA.Mods.TA.SpriteLoaders
{
	public class McsLoader : ISpriteLoader
	{
		class McsFrame : ISpriteFrame
		{
			public SpriteFrameType Type => SpriteFrameType.BGRA;
			public Size Size { get; private set; }
			public Size FrameSize { get; private set; }
			public float2 Offset { get; private set; }
			public byte[] Data { get; private set; }
			public bool DisableExportPadding => false;

			public McsFrame(Size size, Size frameSize, float2 offset, byte[] data)
			{
				Size = size;
				FrameSize = frameSize;
				Offset = offset;
				Data = data;
			}
		}

		public bool TryParseSprite(Stream s, out ISpriteFrame[] frames, out TypeDictionary metadata)
		{
			frames = null;
			metadata = null;

			var mcs = new Mcs();
			if (!mcs.TryLoad(s))
				return false;

			frames = new ISpriteFrame[mcs.Head.FrameCount * (mcs.Head.IsWithShadow ? 2 : 1)];

			for (int i = 0; i < mcs.Head.FrameCount; i++)
			{
				Mcs.MultChannel body = null, shadow = null, remap = null;
				foreach (var mc in mcs.Frames[i].MultChannels)
				{
					if (mc.Head.McType == Mcs.McType.Body)
						body = mc;
					if (mc.Head.McType == Mcs.McType.Shadow)
						shadow = mc;
					if (mc.Head.McType == Mcs.McType.Remap)
						remap = mc;
				}

				if (body != null)
				{
					int cLength = body.Head.H * body.Head.W;
					var bodyData = new byte[cLength * 4];
					for (int j = 0; j < cLength; j++)
					{
						bodyData[j * 4] = body.Channels[0][j];
						bodyData[j * 4 + 1] = body.Channels[1][j];
						bodyData[j * 4 + 2] = body.Channels[2][j];
						bodyData[j * 4 + 3] = body.Channels[3][j];
					}

					frames[i] = new McsFrame(new Size(body.Head.W, body.Head.H),
						new Size(mcs.Head.Width, mcs.Head.Height), 
						new float2(((float)(body.Head.W + (body.Head.X * 2) - mcs.Head.Width)) / 2f, ((float)(body.Head.H + (body.Head.Y * 2) - mcs.Head.Height)) / 2f),
						bodyData);
				}
				else
				{
					return false;
				}

				if (mcs.Head.IsWithShadow)
				{
					if (shadow != null)
					{
						int cLength = shadow.Head.H * shadow.Head.W;
						var shadowData = new byte[cLength * 4];
						for (int j = 0; j < cLength; j++)
						{
							shadowData[j * 4] = 0;
							shadowData[j * 4 + 1] = 0;
							shadowData[j * 4 + 2] = 0;
							shadowData[j * 4 + 3] = shadow.Channels[0][j];
						}

						frames[i + mcs.Head.FrameCount] = new McsFrame(new Size(shadow.Head.W, shadow.Head.H),
							new Size(mcs.Head.Width, mcs.Head.Height), 
							new float2(((float)(shadow.Head.W + (shadow.Head.X * 2) - mcs.Head.Width)) / 2f, ((float)(shadow.Head.H + (shadow.Head.Y * 2) - mcs.Head.Height)) / 2f),
							shadowData);
					}
					else
					{
						return false;
					}
				}
			}

			return true;
		}
	}
}
