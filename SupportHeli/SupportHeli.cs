﻿// SupportHeli 1.0 - Abel Software
// You must download and use Scripthook V Dot Net Reference (LINKS AT BOTTOM OF THE TEMPLATE)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using GTA.Math;

using LapTimer;


namespace GFPS
{
	public class Main : Script
	{
		// You can set your mod information below! Be sure to do this!
		bool firstTime = true;
		string ModName = "Support Heli";
		string Developer = "iLike2Teabag";
		string Version = "1.0";
		IniFile ini = new IniFile("./scripts/SupportHeli.ini");
		Keys activateKey = Keys.F10;


		public Main()
		{
			Tick += onTick;
			KeyDown += onKeyDown;
			Interval = 1;
			Tick += onNthTick;
			Aborted += cleanUp;
		}


		private void onTick(object sender, EventArgs e)
		{
			if (firstTime) // if this is the users first time loading the mod, this information will appear
			{
				GTA.UI.Notification.Show(ModName + " " + Version + " by " + Developer + " Loaded");
				firstTime = false;

				readSettings();
			}
		}

		private void onKeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == activateKey && e.Modifiers == Keys.Control)
			{
				// spawn police maverick
			}
			else if (e.KeyCode == activateKey)
			{
				atkheli.spawnMannedHeli();

				//// if no heli is active, spawn a heli and its crew
				//if (!heliActive)
				//{
				//	supportHeli = spawnSupportHeli(heliModel, supportHeliHeight, haloRadius, heliBulletProof);
				//	spawnNpcsIntoHeli(supportHeli, heliModel, fPatt);
				//	GTA.UI.Notification.Show("Support heli and crew spawned.");
				//}

				//// if a heli is already active, spawn gunners and let them rappel down
				//else
				//{
				//	gunnersRappelDown(supportHeli);
				//	GTA.UI.Notification.Show("Gunners rappeling.");
				//}
			}
		}



		int iTick = 0;
		int N = 250;
		private void onNthTick (object sender, EventArgs e) 
		{
			// if not the Nth tick, reset
			if (iTick != N)
			{
				iTick++;
				return;
			}
			else iTick = 0;			// reset tick counter if reached N


			// manipulate the heli if it is active
			atkheli.flyToPlayer();
		}





		bool heliActive = false;
		HeliModel heliModel;
		Vehicle supportHeli;
		float haloRadius = 20.0f;
		float supportHeliHeight = 40.0f;
		readonly Vector3 nullVector3 = new Vector3(0f, 0f, 0f);
		bool heliBulletProof = true;
		const WeaponHash defaultPrimary = WeaponHash.SpecialCarbine;
		const WeaponHash defaultSidearm = WeaponHash.CombatPistol;
		RelationshipGroup heliRelGroup;

		public Vehicle spawnSupportHeli(HeliModel model, float height, float radius, bool bulletProof)
		{
			// if a heli is already active, return immediately
			if (heliActive)
			{
				GTA.UI.Notification.Show("Support heli is already active.");
				return supportHeli;
			}

			// determine player position
			Vector3 heliPos = Game.Player.Character.Position + Helper.getOffsetVector3(70f, radius);

			// spawn a heli
			Vehicle heli = World.CreateVehicle((Model)((int)model), heliPos);
			heli.IsBulletProof = true;
			heli.AddBlip();
			heli.AttachedBlip.Sprite = BlipSprite.HelicopterAnimated;
			heli.AttachedBlip.Color = BlipColor.Green;
			
			// activate engine & freeze the entity so it doesn't fall
			heli.IsEngineRunning = true;
			heli.LandingGearState = VehicleLandingGearState.Retracted;

			// mark support heli as active and return the vehicle instance
			heliActive = true;
			return heli;
		}



		FiringPattern fPatt = FiringPattern.FullAuto;
		private void spawnNpcsIntoHeli(Vehicle heli, HeliModel model, FiringPattern fp)
		{
			// spawn pilot & set into heli
			Ped pilot = World.CreatePed(PedHash.Pilot01SMY, nullVector3);
			pilot.SetIntoVehicle(heli, VehicleSeat.Driver);

			// create a heli RelationshipGroup and add pilot to it; make heli and player's group allies
			heliRelGroup = World.AddRelationshipGroup("heliGroup");
			pilot.RelationshipGroup = heliRelGroup;
			heliRelGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Companion, true);

			// task pilot with chasing player with heli always
			Vector3 playerPos = Game.Player.Character.Position;
			pilot.Task.ChaseWithHelicopter(Game.Player.Character, Helper.getOffsetVector3(supportHeliHeight, haloRadius));
			pilot.AlwaysKeepTask = true;

			// give pilot sidearm
			pilot.Weapons.Give(defaultSidearm, 9999, true, true);

			// if multi-seat heli, spawn more shooters
			switch (model)
			{
				case HeliModel.Akula:
					spawnCopilot(heli, heliRelGroup);
					break;

				case HeliModel.Valkyrie:
					spawnCopilot(heli, heliRelGroup);
					spawnGunners(heli, 2, heliRelGroup, gunnerSeats);
					break;

				case HeliModel.Buzzard:
					spawnGunners(heli, 2, heliRelGroup, gunnerSeats);
					break;
			}
		}



		private void spawnCopilot(Vehicle heli, RelationshipGroup rg, WeaponHash sidearm = defaultSidearm)
		{
			// spawn copilot & set into heli
			Ped coplt = heli.CreatePedOnSeat(VehicleSeat.Passenger, PedHash.Pilot02SMM);

			// set relationship group to player's group
			coplt.RelationshipGroup = rg;
			Game.Player.Character.PedGroup.Add(coplt, false);

			// give copilot sidearm
			coplt.Weapons.Give(defaultSidearm, 9999, true, true);

			// task copilot with shooting baddies
			coplt.FiringPattern = FiringPattern.FullAuto;
			coplt.Task.FightAgainstHatedTargets(50000);
			coplt.AlwaysKeepTask = true;
		}



		VehicleSeat[] gunnerSeats = new VehicleSeat[2] { VehicleSeat.LeftRear, VehicleSeat.RightRear };
		private Ped[] spawnGunners(Vehicle heli, int numGunners, RelationshipGroup rg, VehicleSeat[] seats, WeaponHash weapon = defaultPrimary, WeaponHash sidearm = WeaponHash.CombatPistol)
		{
			Ped[] ret = new Ped[numGunners];

			// spawn gunners as needed
			for (int i = 0; i < numGunners; i++)
			{
				// spawn the gunner and put into a seat; if seat is occupied, add the Ped to the array
				Ped seatOccupant = heli.GetPedOnSeat(seats[i]);
				if (seatOccupant != null) seatOccupant.Delete();
				Ped gunner = World.CreatePed(PedHash.Blackops01SMY, getVector3NearPlayer());

				// get the gunner into the player's PedGroup; make sure player is the group's leader
				PedGroup playerPg = Game.Player.Character.PedGroup;
				Function.Call(Hash.SET_PED_AS_GROUP_LEADER, Game.Player.Character, playerPg);
				playerPg.Add(gunner, false);
				Function.Call(Hash.SET_PED_CAN_TELEPORT_TO_GROUP_LEADER, gunner.Handle, playerPg.Handle, true);
				gunner.NeverLeavesGroup = true;

				// put the gunner into a heli seat
				gunner.SetIntoVehicle(heli, seats[i]);

				// add to heli RelationshipGroup
				gunner.RelationshipGroup = rg;

				// give the gunner weapons & sidearm
				gunner.Weapons.Give(weapon, 9999, true, true);
				gunner.Weapons.Give(sidearm, 9999, true, true);

				// set the gunner to fight
				gunner.FiringPattern = FiringPattern.FullAuto;
				gunner.Task.FightAgainstHatedTargets(50000);
				gunner.AlwaysKeepTask = true;

				// add the new gunner to return array
				ret[i] = gunner;
			}

			return ret;
		}


		// instances of Heli to track
		Attackheli atkheli;



		/// <summary>
		/// Read settings from INI file and instantiate necessary data structures with the settings.
		/// </summary>
		private void readSettings (bool verbose = true) {
			// read in general settings
			string sec = "General";
			activateKey = (Keys)Enum.Parse(typeof(Keys), ini.Read("hotkey", sec) ?? "F10");
			
			// read in settings for Attack Heli
			sec = "AttackHeli";
			atkheli = new Attackheli(ini.Read("heliModel", sec), ini.Read("height", sec), ini.Read("radius", sec), ini.Read("bulletproof", sec) );
		
			// debug printouts
			if (verbose)
			{
				GTA.UI.Notification.Show(atkheli.getSettingsString());
			}
		}



		Dictionary<Ped, GroundCrewAction> groundCrew = new Dictionary<Ped, GroundCrewAction>(101);

		private void gunnersRappelDown(Vehicle heli, int health = 1000, bool canRagdoll = false)
		{
			// make sure there are gunners in the gunner seats
			Ped[] gunners = spawnGunners(heli, 2, heliRelGroup, gunnerSeats);

			// create a RNG
			Random rng = new Random();
			
			foreach (Ped gunner in gunners)
			{
				// add each gunner to the player's PedGroup
				gunner.CanRagdoll = canRagdoll;
				gunner.Health = health;

				// make gunner rappel
				gunner.Task.RappelFromHelicopter();
				groundCrew.Add(gunner, GroundCrewAction.Descending);

				// generate a random role for the ground crew gunner, and give guns accordingly
				Array roles = Enum.GetValues(typeof(GroundCrewRole));
				GroundCrewRole randomRole = (GroundCrewRole)roles.GetValue(rng.Next(0, roles.Length));
				CrewHandler.giveGroundCrewGuns(gunner, randomRole);
			}
		}



		private Vector3 getVector3NearPlayer (float radius = 2.0f)
		{
			return Helper.getOffsetVector3(0.0f, radius);
		}



		private void cleanUp(object sender, EventArgs e)
		{
			atkheli.destructor(true);
		}
	}




}


// Useful Links
// All Vehicles - https://pastebin.com/uTxZnhaN
// All Player Models - https://pastebin.com/i5c1zA0W
// All Weapons - https://pastebin.com/M3kD9pnJ
// GTA V ScriptHook V Dot Net - https://www.gta5-mods.com/tools/scripthookv-net