using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.TA.Traits
{
	public class ShakeOnSellInfo : ITraitInfo
	{
		public readonly int Duration = 10;
		public readonly int Intensity = 1;
		public readonly float2 Multiplier = new float2(1, 1);
		public object Create(ActorInitializer init) { return new ShakeOnSell(this); }
	}

	public class ShakeOnSell : INotifySold
	{
		readonly ShakeOnSellInfo info;

		public ShakeOnSell(ShakeOnSellInfo info)
		{
			this.info = info;
		}
		void INotifySold.Selling(Actor self) 
		{
			self.World.WorldActor.Trait<ScreenShaker>().AddEffect(info.Duration, self.CenterPosition, info.Intensity, info.Multiplier);
		}
		void INotifySold.Sold(Actor self) { }

	}
}
