using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.TA.Traits
{
	public class ShakeOnMakeInfo : ITraitInfo
	{
		public readonly int Duration = 10;
		public readonly int Intensity = 1;
		public object Create(ActorInitializer init) { return new ShakeOnMake(this); }
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
			self.World.WorldActor.Trait<ScreenShaker>().AddEffect(info.Duration, self.CenterPosition, info.Intensity);
		}

		void INotifyDeployTriggered.Deploy(Actor self, bool skipMakeAnim)
		{
			if (skipMakeAnim)
			{
				return;
			}
			self.World.WorldActor.Trait<ScreenShaker>().AddEffect(info.Duration, self.CenterPosition, info.Intensity);
		}

		void INotifyDeployTriggered.Undeploy(Actor self, bool skipMakeAnim){}
	}
}
