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
		/// <param name="crew"></param>
		/// <param name="?"></param>
		public static GroundCrewAction groundGunnerHandler (Ped gunner, GroundCrewAction currAction){
			GroundCrewAction newAction = currAction;
			Vector3 playerPos = Game.Player.Character.Position;

			switch (currAction)
			{
				// if the crew is currently in the air (i.e. not on the ground), do nothing
				case GroundCrewAction.Descending:
					if (!gunner.IsInAir) newAction = GroundCrewAction.Regrouping;
					break;


				// handle depending on how far away from player
				case GroundCrewAction.Regrouping:
					float distance = playerPos.DistanceTo(gunner.Position);
					if (distance > regroupThreshold)
						handleMoveToPlayer(gunner, distance, regroupThreshold, playerPos);
					else
					{
						gunner.BlockPermanentEvents = false;
						gunner.Task.FightAgainstHatedTargets(50000);
						newAction = GroundCrewAction.Fighting;
					}
					break;

				// fight unless too far from player
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


		private static void handleMoveToPlayer(Ped crew, float distance, float threshold, Vector3 playerPos)
		{
			Vector3 positionNearPlayer = Helper.getVector3NearPlayer(threshold / 2, playerPos);

			// teleport near player
			if (distance > 20 * threshold)
				crew.Position = Helper.getVector3NearPlayer(threshold * 10, playerPos);

			// run to player
			if (distance > 2 * threshold)
			{
				crew.BlockPermanentEvents = true;
				crew.Task.RunTo(positionNearPlayer);
			}

			// walk to player
			else
			{
				crew.BlockPermanentEvents = false;
				crew.Task.GoTo(positionNearPlayer);
			}
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
					WeaponHash.MicroSMG, WeaponHash.Gusenberg, WeaponHash.MG, WeaponHash.CombatMG,
				}
			},
			//{
			//	GroundCrewRole.Marksman,
			//	new WeaponHash[] {
			//		 WeaponHash.Revolver, WeaponHash.SniperRifle, WeaponHash.HeavySniper, WeaponHash.MarksmanRifle,
			//	}
			//},
			{
				GroundCrewRole.SpecOps,
				new WeaponHash[] {
					WeaponHash.SMG, WeaponHash.AssaultSMG, WeaponHash.MiniSMG, WeaponHash.CombatPDW
				}
			},
			{
				GroundCrewRole.Breacher,
				new WeaponHash[] {
					 WeaponHash.PumpShotgun, WeaponHash.SweeperShotgun, WeaponHash.HeavyShotgun, WeaponHash.AssaultShotgun
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
		Assembling,
		KIA,			// dead
	}


	public enum GroundCrewRole {
		Assault,
		Demolition,
		//Marksman,
		SpecOps,
		Breacher
	}


	public class GroundCrewSettings
	{
		// creation-time settings
		public PedHash[] modelArray = new PedHash[1] { PedHash.Blackops01SMY };
		public bool drawBlip = false;

		// behavior settings
		public float regroupThreshold = 15.0f;

		// settings that can be applied to Peds
		public int health = 1000;
		public bool isInvincible = false;
		public bool canRagdoll = false;
		public WeaponHash sidearm = WeaponHash.Pistol;


		/// <summary>
		/// Apply all applicable settings to the specified Ped
		/// </summary>
		/// <param name="npc">Target Ped</param>
		public void applySettingsToPed(Ped npc)
		{
			npc.Health = health;
			npc.IsInvincible = isInvincible;
			npc.CanRagdoll = canRagdoll;
			npc.Weapons.Give(sidearm, 9999, false, true);
		}
	}
}
