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
using System.Linq;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
    public class ProductionPaletteBackgroundLogic : ChromeLogic
    {
        [ObjectCreator.UseCtor]
        public ProductionPaletteBackgroundLogic(Widget widget, World world)
        {
            var background = widget.GetOrNull("PRODUCTION_BACKGROUND");
			if (background != null)
			{
				var palette = widget.Get<ProductionPaletteWidget>("PRODUCTION_PALETTE");
				var icontemplate = background.Get("ICON_TEMPLATE");

				Action<int, int> updateBackground = (oldCount, newCount) =>
				{
					background.RemoveChildren();

					for (var i = 0; i < newCount; i++)
					{
						var x = i % palette.Columns;
						var y = i / palette.Columns;

						var bg = icontemplate.Clone();
						bg.Bounds.X = palette.IconSize.X * x;
						bg.Bounds.Y = palette.IconSize.Y * y;
						background.AddChild(bg);
					}
				};

				palette.OnIconCountChanged += updateBackground;

				// Set the initial palette state
				updateBackground(0, 0);
			}
        }
    }
}
        
        
