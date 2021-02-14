#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.TA.Effects;
using OpenRA.Mods.TA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.TA.Projectiles
{
	public class BulletAS2TAInfo : IProjectileInfo
	{
		[Desc("Projectile speed in WDist / tick, two values indicate variable velocity.")]
		public readonly WDist[] Speed = { new WDist(17) };

		[Desc("Maximum offset at the maximum range.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Image to display.")]
		public readonly string Image = null;

		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("Loop a randomly chosen sequence of Image from this list while this projectile is moving.")]
		public readonly string[] Sequences = { "idle" };

		[Desc("The palette used to draw this projectile.")]
		[PaletteReference("IsPlayerPalette")]
		public readonly string Palette = "effect";

		public readonly bool IsPlayerPalette = false;

		[Desc("Does this projectile have a shadow?")]
		public readonly bool Shadow = false;

		[Desc("Palette to use for this projectile's shadow if Shadow is true.")]
		[PaletteReference]
		public readonly string ShadowPalette = "shadow";

		[Desc("Trail animation.")]
		public readonly string TrailImage = null;

		[Desc("Loop a randomly chosen sequence of TrailImage from this list while this projectile is moving.")]
		[SequenceReference(nameof(TrailImage), allowNullImage: true)]
		public readonly string[] TrailSequences = { "idle" };

		[Desc("Is this blocked by actors with BlocksProjectiles trait.")]
		public readonly bool Blockable = true;

		[Desc("Width of projectile (used for finding blocking actors).")]
		public readonly WDist Width = new WDist(1);

		[Desc("Arc in WAngles, two values indicate variable arc.")]
		public readonly WAngle[] LaunchAngle = { WAngle.Zero };

		[Desc("Up to how many times does this bullet bounce when touching ground without hitting a target.",
			"0 implies exploding on contact with the originally targeted position.")]
		public readonly int BounceCount = 0;

		[Desc("Sound to play when the projectile hits the ground, but not the target.")]
		public readonly string BounceSound = null;

		[Desc("Terrain where the projectile explodes instead of bouncing.")]
		public readonly HashSet<string> InvalidBounceTerrain = new HashSet<string>();

		[Desc("Modify distance of each bounce by this percentage of previous distance.")]
		public readonly int BounceRangeModifier = 60;

		[Desc("If projectile touches an actor with one of these stances during or after the first bounce, trigger explosion.")]
		public readonly PlayerRelationship ValidBounceBlockerStances = PlayerRelationship.Enemy | PlayerRelationship.Neutral;

		[Desc("Altitude above terrain below which to explode. Zero effectively deactivates airburst.")]
		public readonly WDist AirburstAltitude = WDist.Zero;

		[Desc("Altitude where this bullet should explode when reached.",
			"Negative values allow this bullet to pass cliffs and terrain bumps.")]
		public readonly WDist ExplodeUnderThisAltitude = new WDist(-1536);

		[Desc("Interval in ticks between each spawned Trail animation.")]
		public readonly int TrailInterval = 2;

		[Desc("Delay in ticks until trail animation is spawned.")]
		public readonly int TrailDelay = 1;

		[Desc("Palette used to render the trail sequence.")]
		[PaletteReference("TrailUsePlayerPalette")]
		public readonly string TrailPalette = "effect";

		[Desc("Use the Player Palette to render the trail sequence.")]
		public readonly bool TrailUsePlayerPalette = false;

		[Desc("Image that contains the jet animation")]
		public readonly string JetImage = null;

		[SequenceReference(nameof(JetImage), allowNullImage: true)]
		[Desc("Loop a randomly chosen sequence of JetImage from this list while this projectile is moving.")]
		public readonly string[] JetSequence = { "idle" };

		[PaletteReference(nameof(JetUsePlayerPalette))]
		[Desc("Palette used to render the jet sequence. ")]
		public readonly string JetPalette = "effect";

		[Desc("Use the Player Palette to render the jet sequence.")]
		public readonly bool JetUsePlayerPalette = false;

		public readonly int ContrailLength = 0;
		public readonly int ContrailZOffset = 2047;
		public readonly Color ContrailColor = Color.White;
		public readonly bool ContrailUsePlayerColor = false;
		public readonly int ContrailDelay = 0;
		public readonly WDist ContrailWidth = new WDist(64);

		public readonly int ContrailLength2 = 0;
		public readonly int ContrailZOffset2 = 2047;
		public readonly Color ContrailColor2 = Color.White;
		public readonly bool ContrailUsePlayerColor2 = false;
		public readonly int ContrailDelay2 = 0;
		public readonly WDist ContrailWidth2 = new WDist(64);

		[Desc("How long this projectile can live, LifeTime < 0 means lives forever if not hit the target.")]
		public readonly int[] LifeTime = { -1 };

		public IProjectile Create(ProjectileArgs args) { return new BulletAS2TA(this, args); }
	}

	public class BulletAS2TA : IProjectile, ISync
	{
		readonly BulletAS2TAInfo info;
		readonly ProjectileArgs args;
		readonly Animation anim;
		readonly Animation jetanim;

		[Sync]
		readonly WAngle angle;
		[Sync]
		readonly WDist speed;
		[Sync]
		readonly WAngle facing;

		readonly string trailPalette;
		readonly string palette;

		InstantContrailRenderable contrail;
		InstantContrailRenderable contrail2;


		[Sync]
		WPos pos, lastPos, target, source;
		int length;
		int ticks, smokeTicks, liveTicks, lifetime;
		int remainingBounces;

		public Actor SourceActor { get { return args.SourceActor; } }

		public BulletAS2TA(BulletAS2TAInfo info, ProjectileArgs args)
		{
			this.info = info;
			this.args = args;
			pos = args.Source;
			source = args.Source;

			var world = args.SourceActor.World;

			palette = info.Palette;
			if (info.IsPlayerPalette)
				palette += args.SourceActor.Owner.InternalName;

			if (info.LaunchAngle.Length > 1)
				angle = new WAngle(world.SharedRandom.Next(info.LaunchAngle[0].Angle, info.LaunchAngle[1].Angle));
			else
				angle = info.LaunchAngle[0];

			if (info.Speed.Length > 1)
				speed = new WDist(world.SharedRandom.Next(info.Speed[0].Length, info.Speed[1].Length));
			else
				speed = info.Speed[0];

			target = args.PassiveTarget;
			if (info.Inaccuracy.Length > 0)
			{
				var inaccuracy = Common.Util.ApplyPercentageModifiers(info.Inaccuracy.Length, args.InaccuracyModifiers);
				var range = Common.Util.ApplyPercentageModifiers(args.Weapon.Range.Length, args.RangeModifiers);
				var maxOffset = inaccuracy * (target - pos).Length / range;
				target += WVec.FromPDF(world.SharedRandom, 2) * maxOffset / 1024;
			}

			if (info.AirburstAltitude > WDist.Zero)
			{
				target += new WVec(WDist.Zero, WDist.Zero, info.AirburstAltitude);
			}

			facing = (target - pos).Yaw;
			length = Math.Max((target - pos).Length / speed.Length, 1);

			if (!string.IsNullOrEmpty(info.Image))
			{
				anim = new Animation(world, info.Image, new Func<WAngle>(GetEffectiveFacing));
				anim.PlayRepeating(info.Sequences.Random(world.SharedRandom));
			}

			if (!string.IsNullOrEmpty(info.JetImage))
			{
				jetanim = new Animation(world, info.JetImage);
				jetanim.PlayRepeating(info.Sequences.Random(world.SharedRandom));
			}

			if (info.ContrailLength > 0)
			{
				var color = info.ContrailUsePlayerColor ? InstantContrailRenderable.ChooseColor(args.SourceActor) : info.ContrailColor;
				contrail = new InstantContrailRenderable(world, color, info.ContrailWidth, info.ContrailLength, info.ContrailDelay, info.ContrailZOffset);
			}


			if (info.ContrailLength2 > 0)
			{
				var color2 = info.ContrailUsePlayerColor2 ? InstantContrailRenderable.ChooseColor(args.SourceActor) : info.ContrailColor2;
				contrail2 = new InstantContrailRenderable(world, color2, info.ContrailWidth2, info.ContrailLength2, info.ContrailDelay2, info.ContrailZOffset2);
			}

			trailPalette = info.TrailPalette;
			if (info.TrailUsePlayerPalette)
				trailPalette += args.SourceActor.Owner.InternalName;

			smokeTicks = info.TrailDelay;
			remainingBounces = info.BounceCount;
			lifetime = info.LifeTime.Length == 2
					? world.SharedRandom.Next(info.LifeTime[0], info.LifeTime[1])
					: info.LifeTime[0];
			liveTicks = 0;
		}

		WAngle GetEffectiveFacing()
		{
			var at = (float)ticks / (length - 1);
			var attitude = angle.Tan() * (1 - 2 * at) / (4 * 1024);

			var u = (facing.Angle % 512) / 512f;
			var scale = 2048 * u * (1 - u);

			var effective = (int)(facing.Angle < 512
				? facing.Angle - scale * attitude
				: facing.Angle + scale * attitude);

			return new WAngle(effective);
		}

		public void Tick(World world)
		{
			anim?.Tick();
			jetanim?.Tick();

			lastPos = pos;
			pos = WPos.LerpQuadratic(source, target, angle, ticks, length);

			if (ShouldExplode(world))
				Explode(world);

			liveTicks++;
		}

		bool ShouldExplode(World world)
		{
			if (lifetime > 0 && liveTicks > lifetime)
			{
				return true;
			}

			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(world, lastPos, pos, info.Width,
				out var blockedPos))
			{
				pos = blockedPos;
				return true;
			}

			if (!string.IsNullOrEmpty(info.TrailImage) && --smokeTicks < 0)
			{
				var delayedPos = WPos.LerpQuadratic(source, target, angle, ticks - info.TrailDelay, length);
				world.AddFrameEndTask(w => w.Add(new SpriteEffect(delayedPos, GetEffectiveFacing(), w,
					info.TrailImage, info.TrailSequences.Random(world.SharedRandom), trailPalette)));

				smokeTicks = info.TrailInterval;
			}

			if (info.ContrailLength > 0)
				contrail.Update(pos);

			if (info.ContrailLength2 > 0)
				contrail2.Update(pos);

			var flightLengthReached = ticks++ >= length;
			var shouldBounce = remainingBounces > 0;

			if (flightLengthReached && shouldBounce)
			{
				var cell = world.Map.CellContaining(pos);
				if (!world.Map.Contains(cell))
					return true;

				if (info.InvalidBounceTerrain.Contains(world.Map.GetTerrainInfo(cell).Type))
					return true;

				if (AnyValidTargetsInRadius(world, pos, info.Width, args.SourceActor, true))
					return true;

				target += (pos - source) * info.BounceRangeModifier / 100;
				var dat = world.Map.DistanceAboveTerrain(target);
				target += new WVec(0, 0, -dat.Length);
				length = Math.Max((target - pos).Length / speed.Length, 1);

				ticks = 0;
				source = pos;
				Game.Sound.Play(SoundType.World, info.BounceSound, source);
				remainingBounces--;
			}

			// Flight length reached / exceeded
			if (flightLengthReached && !shouldBounce)
				return true;

			// Driving into cell with different height level
			if (world.Map.DistanceAboveTerrain(pos) < info.ExplodeUnderThisAltitude)
				return true;

			// After first bounce, check for targets each tick
			if (remainingBounces < info.BounceCount && AnyValidTargetsInRadius(world, pos, info.Width, args.SourceActor, true))
				return true;

			return false;
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (info.ContrailLength > 0)
				yield return contrail;

			if (info.ContrailLength2 > 0)
				yield return contrail2;

			var world = args.SourceActor.World;
			if (!world.FogObscures(pos))
			{
				if (anim != null)
				{
					var palette = wr.Palette(info.Palette + (info.IsPlayerPalette ? args.SourceActor.Owner.InternalName : ""));
					foreach (var r in anim.Render(pos, palette))
						yield return r;

					if (info.Shadow)
					{
						var dat = world.Map.DistanceAboveTerrain(pos);
						var shadowPos = pos - new WVec(0, 0, dat.Length);
						foreach (var r in anim.Render(shadowPos, wr.Palette("shadow")))
							yield return r;
					}
				}

				if (jetanim != null)
				{
					var palette = wr.Palette(info.JetPalette + (info.JetUsePlayerPalette ? args.SourceActor.Owner.InternalName : ""));
					foreach (var r in jetanim.Render(pos, palette))
						yield return r;
				}
			}
		}

		void Explode(World world)
		{
			if (info.ContrailLength > 0)
				world.AddFrameEndTask(w => w.Add(new InstantContrailFader(pos, contrail)));
			if (info.ContrailLength2 > 0)
				world.AddFrameEndTask(w => w.Add(new InstantContrailFader(pos, contrail2)));

			world.AddFrameEndTask(w => w.Remove(this));

			var warheadArgs = new WarheadArgs(args)
			{
				ImpactOrientation = new WRot(WAngle.Zero, Common.Util.GetVerticalAngle(lastPos, pos), args.Facing),
				ImpactPosition = pos,
			};

			args.Weapon.Impact(Target.FromPos(pos), warheadArgs);
		}

		bool AnyValidTargetsInRadius(World world, WPos pos, WDist radius, Actor firedBy, bool checkTargetType)
		{
			foreach (var victim in world.FindActorsInCircle(pos, radius))
			{
				if (checkTargetType && !Target.FromActor(victim).IsValidFor(firedBy))
					continue;

				if (!info.ValidBounceBlockerStances.HasStance(firedBy.Owner.RelationshipWith(victim.Owner)))
					continue;

				// If the impact position is within any actor's HitShape, we have a direct hit
				var activeShapes = victim.TraitsImplementing<HitShape>().Where(Exts.IsTraitEnabled);
				if (activeShapes.Any(i => i.DistanceFromEdge(victim, pos).Length <= 0))
					return true;
			}

			return false;
		}
	}
}
