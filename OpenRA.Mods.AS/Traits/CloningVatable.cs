using System.Collections.Generic;
using System.Drawing;
using OpenRA.Mods.AS.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Can clone Actor if it enters certain buildings.")]
	public class CloningVatableInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Defines the build duration in ticks until the actor will get cloned.")]
		public readonly int BuildDuration = -1;

		[FieldLoader.Require]
		[Desc("Defines the production type to be used e.g. for exists.")]
		public readonly string ProductionType = "Vat";

		[Desc("Defines which actor will get produced.")]
		public readonly string ProductionActor = "";

		[VoiceReference]
		public readonly string Voice = "Action";
		public readonly string EnterCursor = "enter";
		public readonly string EnterBlockedCursor = "enter-blocked";
		public override object Create(ActorInitializer init) { return new CloningVatable(init.Self, this); }
	}

	public class CloningVatable : ConditionalTrait<CloningVatableInfo>, IIssueOrder, IResolveOrder, IOrderVoice
	{
		public CloningVatable(Actor self, CloningVatableInfo info) : base(info)
		{
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new CloningVatableOrderTargeter(Info); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "EnterCloningVat")
				return null;

			return new Order(order.OrderID, self, target, queued) { };
		}

		static bool IsValidOrder(Actor self, Order order)
		{
			// Not targeting a frozen actor
			if (order.ExtraData == 0 && order.TargetActor == null)
				return false;

			var cloningVatTrait = order.TargetActor.TraitOrDefault<CloningVat>();

			if (cloningVatTrait == null)
				return false;

			return !order.TargetActor.IsDead;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "EnterCloningVat" && IsValidOrder(self, order)
				? Info.Voice : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "EnterCloningVat" || !IsValidOrder(self, order))
				return;

			var target = self.ResolveFrozenActorOrder(order, Color.Yellow);
			if (target.Type != TargetType.Actor)
				return;

			var targettrait = order.TargetActor.TraitOrDefault<CloningVat>();

			if (targettrait == null)
				return;

			if (!order.Queued)
				self.CancelActivity();

			self.SetTargetLine(target, Color.Yellow);
			self.QueueActivity(new EnterCloningVat(self, target.Actor, EnterBehaviour.Exit));
		}

		class CloningVatableOrderTargeter : UnitOrderTargeter
		{
			CloningVatableInfo info;

			public CloningVatableOrderTargeter(CloningVatableInfo info)
				: base("EnterCloningVat", 6, info.EnterCursor, true, true)
			{
				this.info = info;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				if (modifiers.HasFlag(TargetModifiers.ForceAttack))
					return false;

				// Valid enemy CloningVats entrances should still be offered to be destroyed first.
				if (self.Owner.Stances[target.Owner] == Stance.Enemy && !modifiers.HasFlag(TargetModifiers.ForceMove))
					return false;

				var trait = target.TraitOrDefault<CloningVat>();
				if (trait == null)
					return false;
				
				return true;
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				// You can't enter frozen actor.
				return false;
			}
		}
	}
}
