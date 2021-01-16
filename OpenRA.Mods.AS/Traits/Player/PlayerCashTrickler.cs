#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Lets the player actor generate cash in a set periodic time.")]
	public class PlayerCashTricklerInfo : PausableConditionalTraitInfo, Requires<PlayerResourcesInfo>
	{
		[Desc("Number of ticks to wait between giving money.")]
		public readonly int Interval = 50;

		[Desc("Number of ticks to wait before giving first money.")]
		public readonly int InitialDelay = 0;

		[Desc("Amount of money to give each time.")]
		public readonly int Amount = 15;

		public override object Create(ActorInitializer init) { return new PlayerCashTrickler(init.Self, this); }
	}

	public class PlayerCashTrickler : PausableConditionalTrait<PlayerCashTricklerInfo>, ITick, ISync
	{
		readonly PlayerCashTricklerInfo info;
		readonly PlayerResources resources;
		[Sync]
		public int Ticks { get; private set; }

		public PlayerCashTrickler(Actor self, PlayerCashTricklerInfo info)
			: base(info)
		{
			this.info = info;
			resources = self.Trait<PlayerResources>();

			Ticks = info.InitialDelay;
		}

		void ITick.Tick(Actor self)
		{
			if (self.Owner.WinState != WinState.Undefined || self.Owner.NonCombatant)
				return;

			if (IsTraitDisabled)
				Ticks = info.Interval;

			if (IsTraitPaused || IsTraitDisabled)
				return;

			if (--Ticks < 0)
			{
				var cashTricklerModifier = self.TraitsImplementing<ICashTricklerModifier>().Select(x => x.GetCashTricklerModifier());

				Ticks = info.Interval;
				resources.ChangeCash(Util.ApplyPercentageModifiers(info.Amount, cashTricklerModifier));
			}
		}
	}
}
