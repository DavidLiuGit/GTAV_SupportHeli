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
		static float regroupThreshold = 12.5f;

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
					if (playerPos.DistanceTo(gunner.Position) > 2 * regroupThreshold)
					{
						gunner.BlockPermanentEvents = true;
						gunner.Task.RunTo(playerPos);
					}
					else if (playerPos.DistanceTo(gunner.Position) > regroupThreshold)
					{
						gunner.BlockPermanentEvents = false;
						gunner.Task.GoTo(playerPos);
					}
					else
					{
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


		public static void giveGroundCrewGuns(Ped crew, GroundCrewRole role)
		{
			// give the ground crew member sidearms
			foreach (WeaponHash sa in sidearms)
				crew.Weapons.Give(sa, 9999, true, true);

			// give the ground crew member primary weapons for their role
			WeaponHash[] primaryWeapons = weaponsOfRoles[role];
			foreach (WeaponHash wp in primaryWeapons)
				crew.Weapons.Give(wp, 9999, true, true);
		}
		#endregion




		#region lookupTables
		// declare the weapons that ground crew of each role will be assigned
		public static Dictionary<GroundCrewRole, WeaponHash[]> weaponsOfRoles = new Dictionary<GroundCrewRole, WeaponHash[]>()
		{
			{ 
				GroundCrewRole.Assault, 
				new WeaponHash[] {
					WeaponHash.SpecialCarbine, WeaponHash.AssaultRifle, WeaponHash.AdvancedRifle, WeaponHash.CarbineRifle
				}
			},
			{
				GroundCrewRole.Demolition,
				new WeaponHash[] {
					WeaponHash.AssaultShotgun, WeaponHash.PumpShotgun, WeaponHash.MG, WeaponHash.CombatMG,
				}
			},
			{
				GroundCrewRole.Marksman,
				new WeaponHash[] {
					 WeaponHash.Revolver, WeaponHash.SniperRifle, WeaponHash.HeavySniper, WeaponHash.MarksmanRifle,
				}
			},
			{
				GroundCrewRole.SpecOps,
				new WeaponHash[] {
					WeaponHash.SMG, WeaponHash.AssaultSMG, WeaponHash.MiniSMG, WeaponHash.MicroSMG
				}
			}
		};

		public static WeaponHash[] sidearms = new WeaponHash[] {
			WeaponHash.SwitchBlade, WeaponHash.Knife, 
			WeaponHash.SNSPistol, WeaponHash.HeavyPistol, WeaponHash.Pistol50, WeaponHash.CombatPistol, WeaponHash.APPistol,
			 
		};
		#endregion
	}



	public enum GroundCrewAction : int
	{
		Descending,
		Regrouping,
		Fighting,
		Gathering,
	}


	public enum GroundCrewRole {
		Assault,
		Demolition,
		Marksman,
		SpecOps
	}
}
