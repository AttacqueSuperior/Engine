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
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Warheads
{
	public class GrantExternalConditionToPlayerWarhead : WarheadAS
	{
		[FieldLoader.Require]
		[Desc("The condition to apply. Must be included in the target actor's ExternalConditions list.")]
		public readonly string Condition = null;

		[Desc("Duration of the condition (in ticks). Set to 0 for a permanent condition.")]
		public readonly int Duration = 0;

		[Desc("Range where the warhead look for actors owned by the players. If set to 0, it applies to all.")]
		public readonly WDist Range = WDist.FromCells(1);

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			if (!target.IsValidFor(firedBy))
				return;

			HashSet<Actor> players;

			if (Range == WDist.Zero)
				players = firedBy.World.Players.Where(p => ValidRelationships.HasStance(firedBy.Owner.RelationshipWith(p))).Select(p => p.PlayerActor).ToHashSet();
			else
			{
				var actors = target.Type == TargetType.Actor ? new[] { target.Actor } :
					firedBy.World.FindActorsInCircle(target.CenterPosition, Range);

				players = actors.Where(a => !IsValidAgainst(a, firedBy)).Select(a => a.Owner.PlayerActor).ToHashSet();
			}

			foreach (var p in players)
			{
				var external = p.TraitsImplementing<ExternalCondition>()
					.FirstOrDefault(t => t.Info.Condition == Condition && t.CanGrantCondition(p, firedBy));

				if (external != null)
					external.GrantCondition(p, firedBy, Duration);
			}
		}
	}
}
