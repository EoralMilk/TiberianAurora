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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.TA.Projectiles
{
	[Desc("Fake projectile, swap target with other weapon.")]
	public class WeaponSwapFakePInfo : IProjectileInfo, IRulesetLoaded<WeaponInfo>
	{
		[WeaponReference]
		[FieldLoader.Require]
		[Desc("Has to be defined in weapons.yaml as well.")]
		public readonly string Weapon = null;

		[Desc("The maximum duration (in ticks) of the swaping.")]
		public readonly int SwapDuration = 10;

		[Desc("Number of burst.")]
		public readonly int Bursts = 1;

		[Desc("First burst point offset.")]
		public readonly WVec FirstBurstTargetOffset = WVec.Zero;

		[Desc("Last burst point offset.")]
		public readonly WVec LastBurstTargetOffset = WVec.Zero;

		public WeaponInfo WeaponInfo;

		public virtual IProjectile Create(ProjectileArgs args)
		{
			return new WeaponSwapFakeP(this, args);
		}

		public void RulesetLoaded(Ruleset rules, WeaponInfo ai)
		{
			if (!rules.Weapons.TryGetValue(Weapon.ToLowerInvariant(), out WeaponInfo))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(Weapon.ToLowerInvariant()));
		}
	}

	public class WeaponSwapFakeP : IProjectile
	{
		WeaponSwapFakePInfo info;
		protected readonly ProjectileArgs Args;

		WeaponInfo weapon;

		Actor source;

		List<(int Ticks, Action Func)> delayedActions = new List<(int, Action)>();

		public WeaponSwapFakeP(WeaponSwapFakePInfo info, ProjectileArgs args)
		{
			this.info = info;
			Args = args;
			weapon = info.WeaponInfo;
			source = args.SourceActor;

			var delay = (double)info.SwapDuration / (double)info.Bursts;
			var yawFormTargetToSelf = WRot.FromYaw((args.PassiveTarget - args.Source).Yaw);
			var swapFirstOffset = info.FirstBurstTargetOffset.Rotate(yawFormTargetToSelf);
			var swapStepOffset = ((info.LastBurstTargetOffset - info.FirstBurstTargetOffset) / info.Bursts).Rotate(yawFormTargetToSelf);
			for (int i = 0; i <= info.Bursts; i++)
			{
				var newArgs = new ProjectileArgs
				{
					Weapon = weapon,
					Facing = args.Facing,
					CurrentMuzzleFacing = args.CurrentMuzzleFacing,

					DamageModifiers = args.DamageModifiers,

					InaccuracyModifiers = args.InaccuracyModifiers,

					RangeModifiers = args.RangeModifiers,

					Source = args.Source,
					CurrentSource = args.CurrentSource,
					SourceActor = args.SourceActor,
					PassiveTarget = args.PassiveTarget + swapFirstOffset + swapStepOffset * i,
					GuidedTarget = args.GuidedTarget
				};

				// Lambdas can't use 'in' variables, so capture a copy for later
				var delayedTarget = args.GuidedTarget;
				ScheduleDelayedAction((int)(i * delay), () =>
				{
					if (newArgs.Weapon.Projectile != null)
					{
						var projectile = newArgs.Weapon.Projectile.Create(newArgs);
						if (projectile != null)
						{
							source.World.Add(projectile);
							projectile.Tick(source.World);
						}

						if (newArgs.Weapon.Report != null && newArgs.Weapon.Report.Any())
							Game.Sound.Play(SoundType.World, newArgs.Weapon.Report, source.World, source.CenterPosition);
					}
				});
			}
		}

		public virtual void Tick(World world)
		{
			world.AddFrameEndTask(w =>
			{
				for (var i = 0; i < delayedActions.Count; i++)
				{
					var x = delayedActions[i];
					if (--x.Ticks <= 0)
						x.Func();
					delayedActions[i] = x;
				}

				delayedActions.RemoveAll(a => a.Ticks <= 0);
				if (delayedActions.Count == 0)
					w.Remove(this);
			});
		}

		public virtual IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			yield break;
		}

		protected void ScheduleDelayedAction(int t, Action a)
		{
			if (t > 0)
				delayedActions.Add((t, a));
			else
				a();
		}
	}
}
