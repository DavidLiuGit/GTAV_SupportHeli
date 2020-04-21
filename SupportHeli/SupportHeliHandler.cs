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
	public class SupportHeli : Heli
	{
		#region properties
		// by default, give each (non-copilot) crew heavy weapons
		WeaponHash[] gunnerWeapons = CrewHandler.weaponsOfRoles[GroundCrewRole.Heavy];
		const float blipScale = 0.7f;
		public PedGroup leaderPedGroup;
		protected const int maxConfigureAttempts = 5;
		protected override Model fallbackModel { get { return (Model)VehicleHash.Polmav; } }

		protected int seatIndex = 0;
		protected VehicleSeat[] seatSelection;
		#endregion



		/// <summary>
		/// Instantiate Heli with parameters.
		/// </summary>
		/// <param name="model">Model name of the helicopter to use</param>
		/// <param name="height">hover _height of the helicopter</param>
		/// <param name="radius">hover _radius of the helicopter</param>
		/// <param name="bulletproof">Whether the helicopter is _isBulletproof</param>
		public SupportHeli(string model, float height, float radius, bool bulletproof)
			: base(model, height, radius, bulletproof)
		{
			// get the player's current PedGroup (or create a new one if player is not in one)
			leaderPedGroup = _leader.PedGroup;
			if (leaderPedGroup == null)
			{
				leaderPedGroup = new PedGroup();
				leaderPedGroup.Add(_leader, true);
			}
			leaderPedGroup.SeparationRange = 99999f;
			leaderPedGroup.Formation = Formation.Circle2;

			// set the list of seats (based on helicopter _model, but temporarily all the same)
			seatSelection = new VehicleSeat[] { VehicleSeat.LeftRear, VehicleSeat.RightRear };
		}


		#region publicMethods
		/// <summary>
		/// Spawn and task 2 ground crew NPCs to rappel down from the SupportHeli. If no <c>GroundCrewRole</c>
		/// is specified, one will be chosen at random.
		/// </summary>
		/// <returns>Array of <c>Ped</c></returns>
		public Ped[] groundCrewRappelDown(GroundCrewSettings crewSettings)
		{
			Array roles = Enum.GetValues(typeof(GroundCrewRole));
			return groundCrewRappelDown((GroundCrewRole)roles.GetValue(rng.Next(0, roles.Length)), crewSettings);
		}


		/// <summary>
		/// Task SupportHeli gunners to rappel down to the ground (and become ground crew).
		/// </summary>
		/// <param name="role">Ground crew role; weapons are assigned based on role</param>
		/// <returns>Array of <c>Ped</c> rappeling</returns>
		public Ped[] groundCrewRappelDown(GroundCrewRole role, GroundCrewSettings crewSettings)
		{
			// make sure there are gunners in the crew seats
			Ped[] newGroundCrew = new Ped[] {
				spawnCrewGunner(seatSelection[seatIndex % seatSelection.Length], CrewHandler.weaponsOfRoles[role]),
				//spawnCrewGunner(VehicleSeat.RightRear, CrewHandler.weaponsOfRoles[role]),
			};
			seatIndex++;

			// apply settings to each gunner, instruct each gunner to rappel, and add to player's PedGroup
			foreach (Ped crew in newGroundCrew)
			{
				crewSettings.applySettingsToPed(crew);
				crew.Task.RappelFromHelicopter();

				bool res;
				int i = 0;
				do
				{
					res = configureGroundCrew(crew);
					i++;
				}		// if unsuccessful, do again until it does succeed
				while (!res && i < maxConfigureAttempts);
			}

			GTA.UI.Notification.Show("Active Ground Crew: " + leaderPedGroup.MemberCount);
			return newGroundCrew;
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

			newCrew.Add(spawnCrewGunner(VehicleSeat.LeftRear, gunnerWeapons));
			newCrew.Add(spawnCrewGunner(VehicleSeat.RightRear, gunnerWeapons));

			return newCrew.ToArray<Ped>();
		}



		/// <summary>
		/// Give all weapons in <c>weaponArray</c> to the <c>Ped</c> specified.
		/// </summary>
		/// <param name="crew">Ped to give weapons to</param>
		/// <param name="weaponArray">array of <c>WeaponHash</c> to give</param>
		protected override void giveWeapons(Ped crew, WeaponHash[] weaponArray)
		{
			// give sidearms in addition to primaries
			foreach (WeaponHash sidearm in CrewHandler.sidearms)
				crew.Weapons.Give(sidearm, 9999, false, true);
			base.giveWeapons(crew, weaponArray);
		}



		/// <summary>
		/// Apply configurations to ground crew
		/// </summary>
		/// <param name="crew"><c>Ped</c> to apply configuration to</param>
		/// <returns><c>true</c> if the specified <c>Ped</c> is in the leader's PedGroup</returns>
		protected bool configureGroundCrew(Ped crew)
		{
			// add to player's PedGroup if there is space
			leaderPedGroup.Add(crew, false);
			crew.NeverLeavesGroup = true;

			// verify that the crew is part of the player's PedGroup
			if (!crew.IsInGroup)
				return false;

			// draw blip
			crew.AddBlip();
			crew.AttachedBlip.Scale = blipScale;
			crew.AttachedBlip.Color = BlipColor.Green;

			return true;
		}
		#endregion
	}
}
