﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using GTA;
using GTA.Math;
using GTA.Native;


namespace GFPS
{
	public class Attackheli : Heli
	{
		#region properties
		// defaults
		protected WeaponHash[] gunnerWeapons = CrewHandler.weaponsOfRoles[GroundCrewRole.Assault];
		protected const HeliPilotTask defaultPilotTask = HeliPilotTask.ChaseLeader;
		protected override Model fallbackModel { get { return (Model) VehicleHash.Hunter; } }

		// references
		Stack<Ped> targetedPedsStack = new Stack<Ped>();
		#endregion




		#region constructor
		/// <summary>
		/// Instantiate Heli with parameters.
		/// </summary>
		/// <param name="model">Model name of the helicopter to use</param>
		/// <param name="height">hover _height of the helicopter</param>
		/// <param name="radius">hover _radius of the helicopter</param>
		/// <param name="bulletproof">Whether the helicopter is _isBulletproof</param>
		public Attackheli(string model, float height, float radius, bool bulletproof, bool spawnFarAway)
			: base(model, height, radius, bulletproof, spawnFarAway)
		{ }


		/// <summary>
		/// Invoke to clean up assets
		/// </summary>
		/// <param name="force">If <c>true</c>, assets will be forcibly deleted</param>
		public override void destructor(bool force = false)
		{
			// clear target stack
			targetedPedsStack.Clear();
			
			base.destructor(force);
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
						foreach (Ped p in chaseAndEngageTargetedPeds())
							targetedPedsStack.Push(p);
						if (pilotTask == HeliPilotTask.ChaseEngagePed) chaseEngagePedHandler();
						break;

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
			newCrew.Add(spawnCrewGunner(VehicleSeat.Passenger, new WeaponHash[0]));
			
			return newCrew.ToArray<Ped>();
		}



		/// <summary>
		/// Task the Attack Heli's crew to chase and fight against targeted Peds. Update _pilotTask if needed
		/// </summary>
		protected Ped[] chaseAndEngageTargetedPeds()
		{
			Ped[] newTargetedPeds = new Ped[] { };

			// get the Ped that the player is targeting
			Entity ent = Game.Player.TargetedEntity;

			// if player is not targeting anything, stop
			if (ent == null)
				return newTargetedPeds;

			// if the entity the player is targeting is a vehicle, get the vehicle's occupants
			if (ent.EntityType == EntityType.Vehicle)
			{
				Vehicle vehEnt = (Vehicle)ent;
				newTargetedPeds = vehEnt.Occupants;
			}

			// if the entity the player is targeting is a Ped, add the ped to the list
			else if (ent.EntityType == EntityType.Ped)
			{
				Ped pedEnt = (Ped)ent;

				// if the targeted Ped is in a vehicle, target every occupant of that vehicle
				if (pedEnt.IsInVehicle())
					newTargetedPeds = pedEnt.CurrentVehicle.Occupants;

				// otherwise, target the Ped
				else 
					newTargetedPeds = new Ped[] { pedEnt };
			}
				
			// if there is at least 1 new targeted Ped, update the pilot's task
			if (newTargetedPeds.Length > 0)
			{
				_pilotTask = HeliPilotTask.ChaseEngagePed;
				heli.AttachedBlip.Color = BlipColor.Orange;
			}
			
			return newTargetedPeds;
		}



		/// <summary>
		/// Task the pilot and gunner with engaging the top target in targetedPedsStack
		/// </summary>
		protected void chaseEngagePedHandler()
		{
			// if the targetedPeds stack is empty, reset pilot to default task
			if (targetedPedsStack.Count == 0)
			{
				exitChaseEngageMode(defaultPilotTask);
				return;
			}

			// get the last Ped in the targetedPeds Stack
			Ped currTarget = targetedPedsStack.Peek();

			// if the target is not alive, pop it from the stack;
			// repeat until a living target is found, or the stack is depleted
			while (!currTarget.IsAlive)
			{
				targetedPedsStack.Pop();

				// if there are more targets on the stack, get the next one
				if (targetedPedsStack.Count > 0)
					currTarget = targetedPedsStack.Peek();

				// if the stack is now empty, return to default task
				else
				{
					exitChaseEngageMode(defaultPilotTask);
					return;
				}
			}

			// task the pilot with chasing the target
			pilot.Task.ChaseWithHelicopter(currTarget, Helper.getOffsetVector3(_height, _radius));
			//Function.Call(Hash.TASK_HELI_MISSION, pilot, heli, 0, currTarget, 0f, 0f, 0f, 6, 
			//	200f, 5f, (currTarget.Position - heli.Position).ToHeading(), -1, -1, -1, 0);
			//pilot.Task.FightAgainst(currTarget);
			
			// task the passenger(s) with fighting the target
			foreach (Ped passenger in heli.Passengers)
				passenger.Task.FightAgainst(currTarget);
		}



		/// <summary>
		/// Call when targetedPedsStack is depleted
		/// </summary>
		/// <param name="nextTask">Optional; define next task for pilot</param>
		protected void exitChaseEngageMode(HeliPilotTask nextTask = HeliPilotTask.ChaseLeader)
		{
			// reset the attached blip back to normal
			heli.AttachedBlip.Color = defaultBlipColor;

			// request next pilot task
			pilotTasking(nextTask);

			// task all passengers (gunners) with fighting hated Peds
			foreach (Ped passenger in heli.Passengers)
				passenger.Task.FightAgainstHatedTargets(9999f);
		}
		#endregion
	}
}
