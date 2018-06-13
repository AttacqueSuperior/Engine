using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public class CloningVatInfo : ConditionalTraitInfo
	{
		[Desc("Indicates whenever the actor should continue to be cloned or if cloning should stop after one iteration.")]
		public readonly bool ContinuesProduction = false;

		[Desc("Show a bar indicating the progress until spawning a new actor.")]
		public readonly bool ShowProgressBar = true;

		public readonly Color ProgressBarColor = Color.Blue;

		public override object Create(ActorInitializer init) { return new CloningVat(init.Self, this, init.World); }
	}

	public class CloningVat : ConditionalTrait<CloningVatInfo>, Requires<ProductionInfo>, ITick, ISelectionBar, INotifyProduction
	{
		private Actor self;
		private World world;
		private Production production;
		private ActorInfo cloningVatableActorInfo;
		private CloningVatableInfo cloningVatableInfo;
		private int remainingTime;

		public CloningVat(Actor self, CloningVatInfo info, World world) : base(info)
		{
			this.self = self;
			this.production = self.TraitsImplementing<Production>().First();
			this.world = world;
		}

		public void StartProduction(Actor producee)
		{
			SetCloningActor(producee);
		}

		public void UnitProduced(Actor self, Actor other, CPos exit)
		{
			if (Info.ContinuesProduction || (cloningVatableActorInfo != null && other.Info.Name != cloningVatableActorInfo.Name))
			{
				SetCloningActor(other);
			}
		}

		private void SetCloningActor(Actor source)
		{
			var cloningVatable = source.TraitsImplementing<CloningVatable>().FirstOrDefault();
			if(cloningVatable != null)
			{
				var actorInfo = source.Info;

				// Will cause an exception if defined actor does not exist
				if (!string.IsNullOrWhiteSpace(cloningVatable.Info.ProductionActor))
					actorInfo = world.Map.Rules.Actors[cloningVatable.Info.ProductionActor.ToLower()];

				cloningVatableActorInfo = actorInfo;
				cloningVatableInfo = cloningVatable.Info;
				remainingTime = cloningVatable.Info.BuildDuration;
			}
		}

		public void Tick(Actor self)
		{
			if (cloningVatableActorInfo == null)
				return;

			if (remainingTime-- <= 0)
			{
				var inits = new TypeDictionary
				{
					new OwnerInit(self.Owner),
					new FactionInit(production.Faction)
				};

				production.Produce(self, cloningVatableActorInfo, cloningVatableInfo.ProductionType, inits);
				
				if (Info.ContinuesProduction)
				{
					remainingTime = cloningVatableInfo.BuildDuration;
				}
				else
				{
					cloningVatableActorInfo = null;
					cloningVatableInfo = null;
				}
			}
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

		float ISelectionBar.GetValue()
		{
			var value = 0f;

			if (!Info.ShowProgressBar || cloningVatableActorInfo == null || cloningVatableInfo == null)
				return value;

			return remainingTime * 1.0f / cloningVatableInfo.BuildDuration;
		}

		Color ISelectionBar.GetColor()
		{
			return Info.ProgressBarColor;
		}
	}
}
