using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using GTA;
using GTA.Math;
//using GTA.Native;


namespace GFPS
{
	public class Attackheli : Heli
	{
		// by default, give each (non-copilot) crew assault weapons
		WeaponHash[] gunnerWeapons = CrewHandler.weaponsOfRoles[GroundCrewRole.Assault];

		public Attackheli(string iniName, string iniHeight, string iniRadius, string iniBulletproof) :
			base(iniName, iniHeight, iniRadius, iniBulletproof)
		{
			isAttackHeli = true;
		}



		#region mainLogic

		public override void pilotTasking(HeliPilotTask? nextTask = null)
		{
			if (!isHeliServiceable())
				return;

			switch (nextTask)
			{
				case null: break;

				case HeliPilotTask.ChaseEngagePed:
					chaseAndEngageTargetedPeds(); break;

				default:
					base.pilotTasking(nextTask);
					break;
			}
		}
		#endregion



		#region helpers
		protected override Ped[] spawnCrewIntoHeli()
		{
			List<Ped> newCrew = new List<Ped>();

			// if multi-seat heli, spawn more shooters
			switch (model)
			{
				case HeliModel.Akula:
				case HeliModel.Hunter:
					newCrew.Add(spawnCrewGunner(VehicleSeat.Passenger, new WeaponHash[0]));
					break;

				case HeliModel.Valkyrie:
					newCrew.Add(spawnCrewGunner(VehicleSeat.Passenger, new WeaponHash[0]));
					goto default;

				case HeliModel.Buzzard:
				default:
					// spawn a pair of rear-door gunners
					newCrew.Add(spawnCrewGunner(VehicleSeat.LeftRear, gunnerWeapons));
					newCrew.Add(spawnCrewGunner(VehicleSeat.RightRear, gunnerWeapons));
					break;
			}

			return newCrew.ToArray<Ped>();
		}




		protected void chaseAndEngageTargetedPeds()
		{
			// get the Ped that the player is targeting
			Entity ent = Game.Player.TargetedEntity;

		}
		#endregion
	}
}
