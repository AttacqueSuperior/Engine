using System.Linq;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;
namespace OpenRA.Mods.AS.Activities
{
	public class EnterCloningVat : Enter
	{
		public EnterCloningVat(Actor self, Actor target, EnterBehaviour enterBehaviour)
			: base(self, target, enterBehaviour)
		{
		}

		protected override void OnInside(Actor self)
		{
			var targetActor = Target.Actor;
			
			if (targetActor.IsDead || self.IsDead)
				return;

			var vat = targetActor.TraitsImplementing<CloningVat>().First();
			vat.StartProduction(self);
		}
	}
}
