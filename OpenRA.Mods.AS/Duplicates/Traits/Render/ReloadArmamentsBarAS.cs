#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits.Render
{
	[Desc("Visualizes the minimum remaining time for reloading the armaments.")]
	class ReloadArmamentsBarASInfo : ITraitInfo
	{
		[Desc("Armament names")]
		public readonly string[] Armaments = { "primary", "secondary" };

		public readonly Color Color = Color.Red;

		public object Create(ActorInitializer init) { return new ReloadArmamentsBarAS(init.Self, this); }
	}

	class ReloadArmamentsBarAS : ISelectionBar, INotifyCreated
	{
		readonly ReloadArmamentsBarASInfo info;
		readonly Actor self;
		IEnumerable<Armament> armaments;

		public ReloadArmamentsBarAS(Actor self, ReloadArmamentsBarASInfo info)
		{
			this.self = self;
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			// Name check can be cached but enabled check can't.
			armaments = self.TraitsImplementing<Armament>().Where(a => info.Armaments.Contains(a.Info.Name)).ToArray().Where(Exts.IsTraitEnabled);
		}

		float ISelectionBar.GetValue()
		{
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer) || armaments.All(a => !a.IsReloading))
				return 0;

			return 1.0f - armaments.Where(a => a.IsReloading).Min(a => a.FireDelay / (float)a.Weapon.ReloadDelay);
		}

		Color ISelectionBar.GetColor() { return info.Color; }
		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }
	}
}
