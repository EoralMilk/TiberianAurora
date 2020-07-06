#region Copyright & License Information
/*
 * Copyright 2020- The OpenTA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Lint;
using OpenRA.Widgets;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Widgets.Logic;

namespace OpenRA.Mods.RA2.Widgets.Logic.Ingame
{
	[ChromeLogicArgsHotkeys("PlaceBeaconKey")]
	public class BeaconHotkeyLogic : SingleHotkeyBaseLogic
	{
		readonly World world;

		[ObjectCreator.UseCtor]
		public BeaconHotkeyLogic(Widget widget, ModData modData, World world, Dictionary<string, MiniYaml> logicArgs)
			: base(widget, modData, "PlaceBeaconKey", "WORLD_KEYHANDLER", logicArgs)
		{
			this.world = world;
		}

		protected override bool OnHotkeyActivated(KeyInput e)
		{
			world.ToggleInputMode<BeaconOrderGenerator>();
			return true;
		}
	}
}
