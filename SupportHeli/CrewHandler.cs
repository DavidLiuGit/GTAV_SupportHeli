using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTA;
using GTA.Math;


namespace GFPS
{
	class CrewHandler
	{
		#region groundCrew
		static float regroupThreshold = 15.0f;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="gunner"></param>
		/// <param name="?"></param>
		public static GroundCrewAction groundGunnerHandler (Ped gunner, GroundCrewAction currAction){
			GroundCrewAction newAction = currAction;
			Vector3 playerPos = Game.Player.Character.Position;

			switch (currAction)
			{
				// if the gunner is currently in the air (i.e. not on the ground), do nothing
				case GroundCrewAction.Descending:
					if (!gunner.IsInAir) newAction = GroundCrewAction.Regrouping;
					break;


				// calculate 
				case GroundCrewAction.Regrouping:
					if (playerPos.DistanceTo(gunner.Position) > regroupThreshold){
						gunner.BlockPermanentEvents = true;
						gunner.Task.RunTo(playerPos);
					}
					else {
						gunner.BlockPermanentEvents = false;
						gunner.Task.FightAgainstHatedTargets(50000);
						newAction = GroundCrewAction.Fighting;
					}
					break;

				case GroundCrewAction.Fighting:
					if (playerPos.DistanceTo(gunner.Position) > regroupThreshold)
						newAction = GroundCrewAction.Regrouping;
					break;
			}

			return newAction;
		}


		public static GroundCrewAction groundGunnerHandler(KeyValuePair<Ped, GroundCrewAction> entry)
		{
			return groundGunnerHandler(entry.Key, entry.Value);
		}

		#endregion
	}



	public enum GroundCrewAction : int
	{
		Descending,
		Regrouping,
		Fighting,
	}
}
