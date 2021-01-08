using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.TA.Traits
{
	public class ShakeOnMakeInfo : TraitInfo
	{
		public readonly int Duration = 10;
		public readonly int Intensity = 1;
		public readonly float2 Multiplier = new float2(1, 1);
		public override object Create(ActorInitializer init) { return new ShakeOnMake(this); }
	}

	public class ShakeOnMake : INotifyCreated, INotifyDeployTriggered
	{
		readonly ShakeOnMakeInfo info;

		public ShakeOnMake(ShakeOnMakeInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			self.World.WorldActor.Trait<ScreenShaker>().AddEffect(info.Duration, self.CenterPosition, info.Intensity, info.Multiplier);
		}

		void INotifyDeployTriggered.Deploy(Actor self, bool skipMakeAnim)
		{
			if (skipMakeAnim)
			{
				return;
			}
			self.World.WorldActor.Trait<ScreenShaker>().AddEffect(info.Duration, self.CenterPosition, info.Intensity, info.Multiplier);
		}

		void INotifyDeployTriggered.Undeploy(Actor self, bool skipMakeAnim){}
	}
}
