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
		#region properties
		// by default, give each (non-copilot) crew assault weapons
		WeaponHash[] gunnerWeapons = CrewHandler.weaponsOfRoles[GroundCrewRole.Assault];

		// references
		List<Ped> targetedPeds = new List<Ped>();
		#endregion




		#region constructor
		public Attackheli(string iniName, string iniHeight, string iniRadius, string iniBulletproof) :
			base(iniName, iniHeight, iniRadius, iniBulletproof)
		{
			isAttackHeli = true;
		}
		#endregion




		#region mainLogic
		/// <summary>
		/// Assign a task to the Attack Heli pilot
		/// </summary>
		/// <param name="nextTask"><c>HeliPilotTask</c></param>
		public override void pilotTasking(HeliPilotTask? nextTask = null)
		{
			if (!isHeliServiceable())
				return;

			// if nextTask is not null, then update 
			if (nextTask != null)
			{
				switch (nextTask)
				{
					case HeliPilotTask.ChaseEngagePed:
						chaseAndEngageTargetedPeds(); break;

					default:
						base.pilotTasking(nextTask);
						break;
				}
			}

			// if nextTask is null:
			else
			{
				// task the pilot based on the currently active task
				switch (_pilotTask)
				{
					case HeliPilotTask.ChaseLeader:
						pilotTaskChasePed(); break;

					case HeliPilotTask.ChaseEngagePed:
						chaseEngagePedHandler(); break;
				}
			}

		}
		#endregion




		#region helpers
		/// <summary>
		/// Spawn the heli's passengers
		/// </summary>
		/// <returns>Array of <c>Ped</c> spawned as passengers</returns>
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



		/// <summary>
		/// Task the Attack Heli's crew to chase and fight against targeted Peds. Update _pilotTask if needed
		/// </summary>
		protected List<Ped> chaseAndEngageTargetedPeds()
		{
			List<Ped> newTargetedPeds = new List<Ped>();

			// get the Ped that the player is targeting
			Entity ent = Game.Player.TargetedEntity;

			// if the entity the player is targeting is a vehicle, get the vehicle's occupants
			if (ent.EntityType == EntityType.Vehicle)
			{
				Vehicle vehEnt = (Vehicle)ent;
				newTargetedPeds = vehEnt.Occupants.ToList();
			}

			// if the entity the player is targeting is a Ped, add the ped to the list
			else if (ent.EntityType == EntityType.Ped)
			{
				newTargetedPeds.Add((Ped)ent);
			}

			// if there is at least 1 new targeted Ped, update the pilot's task
			if (newTargetedPeds.Count > 0)
				_pilotTask = HeliPilotTask.ChaseEngagePed;

			return newTargetedPeds;
		}



		protected void chaseEngagePedHandler()
		{

		}
		#endregion
	}
}
