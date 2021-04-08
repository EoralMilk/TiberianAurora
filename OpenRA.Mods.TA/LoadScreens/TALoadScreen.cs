
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.LoadScreens
{
	public sealed class TALoadScreen : SheetLoadScreen
	{
		Rectangle stripeRect;
		Rectangle logoRect;
		float2 logoPos;
		Sprite stripe, logo;

		Sheet lastSheet;
		int lastDensity;
		Size lastResolution;

		string[] messages = { "Loading..." };

		public override void Init(ModData modData, Dictionary<string, string> info)
		{
			base.Init(modData, info);

			if (info.ContainsKey("Text"))
				messages = info["Text"].Split(',');
		}

		public override void DisplayInner(Renderer r, Sheet s, int density)
		{
			if (s != lastSheet || density != lastDensity)
			{
				lastSheet = s;
				lastDensity = density;
				logo = CreateSprite(s, density, new Rectangle(0, 0, 512, 512));
				stripe = CreateSprite(s, density, new Rectangle(0, 0, 1, 1));
			}

			if (r.Resolution != lastResolution)
			{
				lastResolution = r.Resolution;
				stripeRect = new Rectangle(0, 0, lastResolution.Width, lastResolution.Height);
				//int temp = lastResolution.Height / 2;
				//logoRect = new Rectangle(lastResolution.Width / 2 - temp, lastResolution.Height / 2 - temp, temp, temp);
				logoPos = new float2(lastResolution.Width / 2 - 256, lastResolution.Height / 2 - 256);
			}

			if (stripe != null)
				WidgetUtils.FillRectWithSprite(stripeRect, stripe);

			if (logo != null)
				r.RgbaSpriteRenderer.DrawSprite(logo, logoPos); 

			if (r.Fonts != null)
			{
				var text = messages.Random(Game.CosmeticRandom);
				var textSize = r.Fonts["Bold"].Measure(text);
				r.Fonts["Bold"].DrawText(text, new float2(r.Resolution.Width - textSize.X - 20, r.Resolution.Height - textSize.Y - 20), Color.White);
			}
		}
	}
}
