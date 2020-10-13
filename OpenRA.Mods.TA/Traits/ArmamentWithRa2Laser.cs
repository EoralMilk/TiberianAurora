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
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.TA.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.TA.Traits
{
	[Desc("Allows you to attach weapons to the unit (use @IdentifierSuffix for > 1)")]
	public class ArmamentWithRa2LaseInfo : ArmamentInfo
	{
		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ZOffset = 0;

		[Desc("The maximum duration (in ticks) of the beam's existence.")]
		public readonly int Duration = 10;

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

		public override object Create(ActorInitializer init) { return new ArmamentWithRa2Lase(init.Self, this); }
	}

	public class ArmamentWithRa2Lase : Armament
	{
		private new readonly ArmamentWithRa2LaseInfo Info;
		public readonly Color InnerColor;
		public readonly Color OuterColor;

		public INotifyAttack[] NotifyAttacks;
		public IEnumerable<int> DamageModifiers;
		public IEnumerable<int> InaccuracyModifiers;
		public IEnumerable<int> RangeModifiers;

		private int attenuationBurst;
		private bool multiBurst;
		private WPos targetPos;
		public ArmamentWithRa2Lase(Actor self, ArmamentWithRa2LaseInfo info)
			: base(self, info)
		{
			Info = info;
			multiBurst = Weapon.FirstBurstTargetOffset != WVec.Zero || Weapon.FollowingBurstTargetOffset != WVec.Zero;
			if (multiBurst)
				if (Weapon.BurstDelays.Length == 1)
					attenuationBurst = info.Duration / Weapon.BurstDelays[0];
				else
				{
					int counter = 0, i = 0;
					for (i = 0; i < Weapon.Burst; i++)
					{
						counter += Weapon.BurstDelays[Weapon.Burst - (i + 1)];
						if (counter > info.Duration)
							break;
					}

					attenuationBurst = i - 1;
				}

			if (info.UsePlayerColor)
			{
				int a;
				float h, s, v;
				self.Owner.Color.ToAhsv(out a, out h, out s, out v);
				InnerColor = Color.FromAhsv(255, h, s / 1.5f, v);
				OuterColor = Color.FromAhsv(255, h, s, v);
			}
			else
			{
				InnerColor = info.InnerColor;
				OuterColor = info.OuterColor;
			}
		}

		protected override void Created(Actor self)
		{
			NotifyAttacks = self.TraitsImplementing<INotifyAttack>().ToArray();
			RangeModifiers = self.TraitsImplementing<IRangeModifier>().ToArray().Select(m => m.GetRangeModifier());
			DamageModifiers = self.TraitsImplementing<IFirepowerModifier>().ToArray().Select(m => m.GetFirepowerModifier());
			InaccuracyModifiers = self.TraitsImplementing<IInaccuracyModifier>().ToArray().Select(m => m.GetInaccuracyModifier());
			base.Created(self);
		}

		protected override void FireBarrel(Actor self, IFacing facing, Target target, Barrel barrel)
		{
			foreach (var na in NotifyAttacks)
				na.PreparingAttack(self, target, this, barrel);

			Func<WPos> muzzlePosition = () => self.CenterPosition + MuzzleOffset(self, barrel);
			var legacyFacing = MuzzleOrientation(self, barrel).Yaw.Angle / 4;
			Func<int> legacyMuzzleFacing = () => MuzzleOrientation(self, barrel).Yaw.Angle / 4;

			var passiveTarget = Weapon.TargetActorCenter ? target.CenterPosition : target.Positions.PositionClosestTo(muzzlePosition());
			var initialOffset = Weapon.FirstBurstTargetOffset;
			if (initialOffset != WVec.Zero)
			{
				// We want this to match Armament.LocalOffset, so we need to convert it to forward, right, up
				initialOffset = new WVec(initialOffset.Y, -initialOffset.X, initialOffset.Z);
				passiveTarget += initialOffset.Rotate(WRot.FromFacing(legacyFacing));
			}

			var followingOffset = Weapon.FollowingBurstTargetOffset;
			if (followingOffset != WVec.Zero)
			{
				// We want this to match Armament.LocalOffset, so we need to convert it to forward, right, up
				followingOffset = new WVec(followingOffset.Y, -followingOffset.X, followingOffset.Z);
				passiveTarget += ((Weapon.Burst - Burst) * followingOffset).Rotate(WRot.FromFacing(legacyFacing));
			}

			targetPos = passiveTarget;

			if (Burst >= attenuationBurst)
				self.World.Add(
					new Ra2LaserEffect(
						Burst > attenuationBurst ? 2 : Info.Duration,
						Info.Width, Info.InnerWidth, InnerColor, OuterColor,
						() => muzzlePosition(),
						() => targetPos,
						Info.ZOffset));

			var args = new ProjectileArgs
			{
				Weapon = Weapon,
				Facing = legacyFacing,
				CurrentMuzzleFacing = legacyMuzzleFacing,

				DamageModifiers = DamageModifiers.ToArray(),

				InaccuracyModifiers = InaccuracyModifiers.ToArray(),

				RangeModifiers = RangeModifiers.ToArray(),

				Source = muzzlePosition(),
				CurrentSource = muzzlePosition,
				SourceActor = self,
				PassiveTarget = passiveTarget,
				GuidedTarget = target
			};

			ScheduleDelayedAction(Info.FireDelay, () =>
			{
				if (args.Weapon.Projectile != null)
				{
					var projectile = args.Weapon.Projectile.Create(args);
					if (projectile != null)
						self.World.Add(projectile);

					if (args.Weapon.Report != null && args.Weapon.Report.Any())
						Game.Sound.Play(SoundType.World, args.Weapon.Report, self.World, self.CenterPosition);

					if (Burst == args.Weapon.Burst && args.Weapon.StartBurstReport != null && args.Weapon.StartBurstReport.Any())
						Game.Sound.Play(SoundType.World, args.Weapon.StartBurstReport, self.World, self.CenterPosition);

					foreach (var na in NotifyAttacks)
						na.Attacking(self, target, this, barrel);

					Recoil = Info.Recoil;
				}
			});
		}
	}
}
