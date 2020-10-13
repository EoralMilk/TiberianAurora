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
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.TA.Projectiles
{
	[Desc("Not a sprite, but an engine effect.")]
	public class Ra2LaserZapInfo : IProjectileInfo
	{
        [Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
        public readonly int ZOffset = 0;

        [Desc("The maximum duration (in ticks) of the beam's existence.")]
        public readonly int Duration = 10;

        [Desc("Total time-frame in ticks that the beam deals damage every DamageInterval.")]
        public readonly int DamageDuration = 1;

        [Desc("The number of ticks between the beam causing warhead impacts in its area of effect.")]
        public readonly int DamageInterval = 1;

        [Desc("Beam follows the target.")]
        public readonly bool TrackTarget = true;

        [Desc("Beam follows the actor self.")]
        public readonly bool TrackSelf = true;

        [Desc("Maximum offset at the maximum range.")]
        public readonly WDist Inaccuracy = WDist.Zero;

        [Desc("Beam can be blocked.")]
        public readonly bool Blockable = false;

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

        [Desc("Impact animation.")]
        public readonly string HitAnim = null;

        [SequenceReference("HitAnim")]
        [Desc("Sequence of impact animation to use.")]
        public readonly string HitAnimSequence = "idle";

        [PaletteReference]
        public readonly string HitAnimPalette = "effect";

        [Desc("Image containing launch effect sequence.")]
        public readonly string LaunchEffectImage = null;

        [SequenceReference("LaunchEffectImage")]
        [Desc("Launch effect sequence to play.")]
        public readonly string LaunchEffectSequence = null;

        [PaletteReference]
        [Desc("Palette to use for launch effect.")]
        public readonly string LaunchEffectPalette = "effect";

        public IProjectile Create(ProjectileArgs args)
        {
        	return new Ra2LaserZap(this, args);
        }
	}

	public class Ra2LaserZap : IProjectile, ISync
	{
        readonly ProjectileArgs args;
        readonly Ra2LaserZapInfo info;
        readonly Animation hitanim;
        readonly Color innerColor;
        readonly Color outerColor;
        readonly bool hasLaunchEffect;
        bool showHitAnim;
        int ticks;
        int interval;

        [Sync]
        WPos target, source;

        public Ra2LaserZap(Ra2LaserZapInfo info, ProjectileArgs args)
        {
        	this.args = args;
        	this.info = info;
        	target = args.PassiveTarget;
        	source = args.Source;
        	ticks = 0;

        	if (info.Inaccuracy.Length > 0)
        	{
                var inaccuracy = OpenRA.Mods.Common.Util.ApplyPercentageModifiers(info.Inaccuracy.Length, args.InaccuracyModifiers);
                var maxOffset = inaccuracy * (target - source).Length / args.Weapon.Range.Length;
                target += WVec.FromPDF(args.SourceActor.World.SharedRandom, 2) * maxOffset / 1024;
        	}

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

        	if (!string.IsNullOrEmpty(info.HitAnim))
        	{
                hitanim = new Animation(args.SourceActor.World, info.HitAnim);
                showHitAnim = true;
        	}

        	hasLaunchEffect = !string.IsNullOrEmpty(info.LaunchEffectImage) && !string.IsNullOrEmpty(info.LaunchEffectSequence);
        }

        public void Tick(World world)
        {
        	if (info.TrackSelf)
                source = args.CurrentSource();

        	if (hasLaunchEffect && ticks == 0)
                world.AddFrameEndTask(w => w.Add(new SpriteEffect(args.CurrentSource, args.CurrentMuzzleFacing, world,
                	info.LaunchEffectImage, info.LaunchEffectSequence, info.LaunchEffectPalette)));

        	// Beam tracks target
        	if (info.TrackTarget && args.GuidedTarget.IsValidFor(args.SourceActor))
                target = args.Weapon.TargetActorCenter ? args.GuidedTarget.CenterPosition : args.GuidedTarget.Positions.PositionClosestTo(source);

        	// Check for blocking actors
        	WPos blockedPos;
        	if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(world, source, target,
                info.Width, out blockedPos))
        	{
                target = blockedPos;
        	}

        	if (ticks < info.DamageDuration && --interval <= 0)
        	{
                args.Weapon.Impact(Target.FromPos(target), new WarheadArgs(args));
                interval = info.DamageInterval;
        	}

        	if (showHitAnim)
        	{
                if (ticks == 0)
                	hitanim.PlayThen(info.HitAnimSequence, () => showHitAnim = false);

                hitanim.Tick();
        	}

        	if (++ticks >= info.Duration && !showHitAnim)
                world.AddFrameEndTask(w => w.Remove(this));
        }

        public IEnumerable<IRenderable> Render(WorldRenderer wr)
        {
        	if (wr.World.FogObscures(target) &&
                wr.World.FogObscures(source))
                yield break;

        	if (ticks < info.Duration)
        	{
                var diffTick = info.Duration - ticks;
                var irc = Color.FromArgb(255, diffTick * innerColor.R / info.Duration, diffTick * innerColor.G / info.Duration, diffTick * innerColor.B / info.Duration);
                var orc = Color.FromArgb(255, diffTick * outerColor.R / info.Duration, diffTick * outerColor.G / info.Duration, diffTick * outerColor.B / info.Duration);
                yield return new Ra2LaserRenderable(source, info.ZOffset, target - source, diffTick * info.Width / info.Duration, diffTick * info.InnerWidth / info.Duration, irc, orc);
        	}

        	if (showHitAnim)
                foreach (var r in hitanim.Render(target, wr.Palette(info.HitAnimPalette)))
                	yield return r;
        }
	}
}
