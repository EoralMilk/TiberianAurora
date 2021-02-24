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
using System.IO;
using System.Text;

namespace OpenRA.Mods.TA.FileFormats
{
    public class Mcs
    {
	    private List<McsFrame> frames;
	    private McsHead head;
	    public List<McsFrame> Frames => frames ?? (frames = new List<McsFrame>());

	    public McsHead Head
        {
            get => head ?? (head = new McsHead());
            set
            {
                head = value;
                frames.Clear();
                frames = new List<McsFrame>(head.FrameCount);
            }
        }

	    public Mcs() { }

	    public bool TryLoad(Stream s)
        {
            try
            {
                var check = Encoding.ASCII.GetString(s.ReadBytes(6));
                if (check != "MCS_V1")
                    throw new IOException("File Error.");
                Head.TryLoad(s);
                Frames.Clear();
                for (int i = 0; i < Head.FrameCount; i++)
                {
                    var frame = new McsFrame();
                    frame.Head = head;
                    frame.TryLoad(s);
                    frames.Add(frame);
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

	    public bool TryDump(Stream s)
        {
            s.WriteArray(Encoding.ASCII.GetBytes("MCS_V1"));
            head.TryDump(s);
            foreach (var i in frames)
            {
                i.TryDump(s);
            }

            return true;
        }

	    public bool IsDumpable()
        {
            if (!head.IsDumpable())
                return false;
            foreach (var i in frames)
                if (i.IsDumpable())
                    return false;
            return true;
        }

        enum FrameInfo : byte
        {
            Shadow = 0x01,
            Remap = 0x02,
            Depth = 0x04,
        }

        public class McsHead
        {
            private byte mcsType;
            public int Width { get; private set; }
            public int Height { get; private set; }
            public int FrameCount { get; private set; }
            public bool IsWithShadow => (mcsType & (byte)FrameInfo.Shadow) != 0;
            public bool IsWithRemap => (mcsType & (byte)FrameInfo.Remap) != 0;

            public McsHead() { }

            public McsHead(int width, int height, int frameCount, bool isWithShadow, bool isWithRemap)
            {
                Width = width;
                Height = height;
                FrameCount = frameCount;
                mcsType = 0;
                if (isWithShadow)
                    mcsType |= (byte)FrameInfo.Shadow;
                if (isWithRemap)
                    mcsType |= (byte)FrameInfo.Remap;
            }

            public bool TryLoad(Stream s)
            {
                Width = s.ReadInt32();
                Height = s.ReadInt32();
                FrameCount = s.ReadInt32();
                mcsType = (byte)s.ReadByte();
                return true;
            }

            public bool TryDump(Stream s)
            {
                s.Write(Width);
                s.Write(Height);
                s.Write(FrameCount);
                s.WriteByte(mcsType);
                return true;
            }

            public bool IsDumpable()
            {
                return Width > 0 && Height > 0 && FrameCount > 0;
            }
        }

        public class McsFrame
        {
            public List<MultChannel> MultChannels { get; private set; }

            public McsHead Head
            {
                set
                {
                    isWithShadow = value.IsWithShadow;
                    isWithRemap = value.IsWithRemap;
                    MultChannels.Clear();
                    MultChannels.Add(new MultChannel());
                    if (isWithShadow)
                        MultChannels.Add(new MultChannel());
                    if (isWithRemap)
                        MultChannels.Add(new MultChannel());
                }
            }

            private bool isWithShadow;
            private bool isWithRemap;

            public McsFrame()
            {
                MultChannels = new List<MultChannel>();
            }

            public bool TryLoad(Stream s)
            {
                MultChannels.Clear();
                var mc = new MultChannel();
                mc.TryLoad(s);
                MultChannels.Add(mc);
                if (isWithShadow)
                {
                    mc = new MultChannel();
                    mc.TryLoad(s);
                    MultChannels.Add(mc);
                }

                if (isWithRemap)
                {
                    mc = new MultChannel();
                    mc.TryLoad(s);
                    MultChannels.Add(mc);
                }

                return true;
            }

            public bool TryDump(Stream s)
            {
                if (MultChannels.Count == 0)
                {
                    throw new IOException("MultChannel Count Error");
                }

                foreach (var i in MultChannels)
                {
                    i.TryDump(s);
                }
                return true;
            }

            public bool IsDumpable()
            {
                foreach (var i in MultChannels)
                    if (i.IsDumpable())
                        return false;
                return true;
            }
        }

        public class MultChannel
        {
            public List<byte[]> Channels { get; private set; }
            private MultChannelHead head;

            public MultChannelHead Head
            {
                get => head ?? (head = new MultChannelHead(0, 0, 0, 0, McType.Body));
                set
                {
                    head = value;
                    Channels.Clear();
                    Channels = new List<byte[]>(head.ChannelCount);
                }
            }

            public MultChannel()
            {
                Channels = new List<byte[]>();
            }

            public void SetMultChannel(int x, int y, int w, int h, List<byte[]> l, McType mcType)
            {
                Head = new MultChannelHead(x, y, w, h, mcType);
            }

            public bool TryLoad(Stream s)
            {
                Head.TryLoad(s);
                int cLength = Head.H * Head.W;
                Channels.Clear();
                for (int i = 0; i < Head.ChannelCount; i++)
                {
                    Channels.Add(s.ReadBytes(cLength));
                }
                return true;
            }

            public bool TryDump(Stream s)
            {
                Head.TryDump(s);
                foreach (var i in Channels)
                {
                    s.WriteArray(i);
                }
                return true;
            }

            public bool IsDumpable()
            {
                if (!Head.IsDumpable())
                    return false;
                if (Channels.Count != Head.ChannelCount)
                    return false;
                return true;
            }
        }

        public enum McType : byte
        {
            Body = 0,
            Shadow = 1,
            Remap = 2,
            Depth = 3,
        }

        public class MultChannelHead
        {
            public int X { get; private set; }
            public int Y { get; private set; }
            public int W { get; private set; }
            public int H { get; private set; }
            public int ChannelCount { get; private set; }
            public McType McType { get; private set; }

            public MultChannelHead(int x, int y, int w, int h, McType mcType)
            {
                X = x;
                Y = y;
                W = w;
                H = h;
                McType = mcType;
                ChannelCount = mcType == McType.Body ? 3 : mcType == McType.Shadow ? 1 : mcType == McType.Remap ? 1 : 0;
            }

            public bool TryLoad(Stream s)
            {
                X = s.ReadInt32();
                Y = s.ReadInt32();
                W = s.ReadInt32();
                H = s.ReadInt32();
                ChannelCount = s.ReadInt32();
                McType = (McType)(byte)s.ReadByte();
                return true;
            }

            public bool TryDump(Stream s)
            {
                s.Write(X);
                s.Write(Y);
                s.Write(W);
                s.Write(H);
                s.Write(ChannelCount);
                s.WriteByte((byte)McType);
                return true;
            }

            public bool IsDumpable()
            {
                return W > 0 && Y > 0 && ChannelCount > 0;
            }
        }
    }
}
