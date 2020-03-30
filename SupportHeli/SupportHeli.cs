// SupportHeli 1.0 - Abel Software
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

		// activation keys
		Keys activateKey = Keys.F10;
		Keys strafeRunActivateKey = Keys.F12;

		// flags
		private bool markStrafeWithFlareShellActive = false;
		private readonly Model flareShellModel = (Model)665801196;


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
			// activateKey pressed
			if (e.KeyCode == activateKey)
			{
				// Shift also pressed
				if (e.Modifiers == Keys.Shift)
				{
					// if supportHeli is not active, spawn one
					if (!supportHeli.isActive)
						supportHeli.spawnMannedHeli(Game.Player.Character);

					// otherwise, task gunners with rappeling down
					else
						gunnersRappelDown();
				}

				// player is currently aiming
				else if (Game.Player.IsAiming)
					attackHeli.pilotTasking(Heli.HeliPilotTask.ChaseEngagePed);

				// Delete also pressed
				else if (Game.IsKeyPressed(Keys.Delete))
					cleanUp(false);			// soft clean-up (helis fly away)

				// PgDown also pressed
				else if (Game.IsKeyPressed(Keys.PageDown))
					supportHeli.pilotTasking(Heli.HeliPilotTask.Land);

				// PgUp also pressed
				else if (Game.IsKeyPressed(Keys.PageUp))
					supportHeli.pilotTasking(Heli.HeliPilotTask.ChaseLeader);

				// Tab also pressed
				else if (Game.IsKeyPressed(Keys.Tab))
					supportHeli.pilotTasking(Heli.HeliPilotTask.FlyToDestination);

				// End also pressed
				else if (Game.IsKeyPressed(Keys.End))
					strafeRun.spawnStrafeRun(Game.Player.Character.Position);

				// no modifiers
				else
					attackHeli.spawnMannedHeli(Game.Player.Character);
			}


			// strafe run activateKey pressed
			else if (e.KeyCode == strafeRunActivateKey)
			{
				// Delete also pressed
				if (Game.IsKeyPressed(Keys.Delete))
					cleanUp(false);

				// if player is currently aiming
				else if (Game.Player.IsAiming)
					strafeRun.spawnStrafeRun(World.GetCrosshairCoordinates().HitPosition);

				// no modifiers
				else
					activateMarkStrafeRunWithFlareGun();
				//strafeRun.spawnStrafeRun(Game.Player.Character.Position);
			}

		}




		int iTick = 0;
		int N = 75;
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
			attackHeli.pilotTasking();
			supportHeli.pilotTasking();

			// manipulate strafe run if active
			strafeRun.strafeRunOnTick();

			if (markStrafeWithFlareShellActive)
				markStrafeRunWithFlareGunListener();

			// handle ground crew actions
			updateGroundCrewActions();
		}
		


		// instances of Heli to track
		Attackheli attackHeli;
		SupportHeli supportHeli;
		StrafeRun strafeRun;
		GroundCrewSettings crewSettings = new GroundCrewSettings();

		/// <summary>
		/// Read settings from INI file and instantiate necessary data structures with the settings.
		/// </summary>
		private void readSettings (bool verbose = false) {
			// init ScriptSettings
			ScriptSettings ss = base.Settings;

			// read in general settings
			string sec = "General";
			activateKey = ss.GetValue<Keys>(sec, "activate", activateKey);
			
			// read in settings for Attack Heli
			sec = "AttackHeli";
			attackHeli = new Attackheli(
				ss.GetValue<HeliModel>(sec, "HeliModel", HeliModel.Hunter),
				ss.GetValue<float>(sec, "height", 20f),
				ss.GetValue<float>(sec, "radius", 20f),
				ss.GetValue<bool>(sec, "bulletproof", true)
				);
			RelationshipGroup heliRg = attackHeli._rg;

			// read in settings for Support Heli
			sec = "SupportHeli";
			supportHeli = new SupportHeli(
				ss.GetValue<HeliModel>(sec, "HeliModel", HeliModel.Hunter),
				ss.GetValue<float>(sec, "height", 20f),
				ss.GetValue<float>(sec, "radius", 20f),
				ss.GetValue<bool>(sec, "bulletproof", true)
				);
			supportHeli._rg = heliRg;

			// read in settings for Strafe Run
			sec = "JetStrafeRun";
			strafeRunActivateKey = ss.GetValue<Keys>(sec, "activateKey", strafeRunActivateKey);
			strafeRun = new StrafeRun(
				ss.GetValue<float>(sec, "spawnRadius", 375f),
				ss.GetValue<float>(sec, "spawnHeight", 275f),
				ss.GetValue<float>(sec, "targetRadius", 50f),
				ss.GetValue<bool>(sec, "cinematic", true)
				);

			// read in settings for ground crew
			crewSettings = new GroundCrewSettings(ini);

			// manipulate heliRg
			Helper.makeRelationshipGroupHate(heliRg, Helper.defaultHateGroups);

			// debug printouts
			if (verbose)
			{
				GTA.UI.Notification.Show(attackHeli.getSettingsString());
				GTA.UI.Notification.Show(supportHeli.getSettingsString());
			}
		}



		Dictionary<Ped, GroundCrewAction> groundCrew = new Dictionary<Ped, GroundCrewAction>(101);

		/// <summary>
		/// When prompted by user, spawn ground crew and instruct them to rappel from the SupportHeli
		/// </summary>
		private void gunnersRappelDown()
		{
			// make sure there are gunners in the crew seats
			Ped[] crew = supportHeli.groundCrewRappelDown(crewSettings);

			foreach (Ped gunner in crew)
				groundCrew.Add(gunner, GroundCrewAction.Descending);
		}


		/// <summary>
		/// Update the actions of the ground crew using <c>CrewHandler.groundGunnerHandler</c>.
		/// </summary>
		private void updateGroundCrewActions()
		{
			// iterate over each groundCrew that is being tracked
			var crew = groundCrew.Keys;
			for (int i = 0; i < crew.Count; i++)
			{
				// check if the NPC is dead; if so, remove from list and continue
				Ped p = crew.ElementAt(i);
				if (p.IsDead)
				{
					CrewHandler.crewDestructor(p);
					groundCrew.Remove(p);
					continue;
				}
			}
		}

		
		
		private void cleanUp(object sender, EventArgs e)
		{
			cleanUp(true);
		}

		/// <summary>
		/// Script destructor; cleans up assets created by the script as necessary
		/// </summary>
		/// <param name="force"></param>
		private void cleanUp(bool force)
		{
			// try to clean up helis
			try
			{
				attackHeli.destructor(force);
				supportHeli.destructor(force);
				strafeRun.destructor(force);
			}
			catch { }

			
			// clean up any ground crew
			PedGroup playerPedGrp = Game.Player.Character.PedGroup;
			foreach (Ped p in groundCrew.Keys.ToArray())
			{
				playerPedGrp.Remove(p);			// remove the ped from player's PedGroup
				CrewHandler.crewDestructor(p, force);
			}
		}



		/// <summary>
		/// Give the player a flare gun to mark the position of a strafe run with
		/// </summary>
		private void activateMarkStrafeRunWithFlareGun()
		{
			if (markStrafeRunWithFlareGunListener())
				return;

			// give player flare gun and ammo
			Weapon flareGun = Game.Player.Character.Weapons.Give(WeaponHash.FlareGun, 1, true, true);
			flareGun.AmmoInClip = 1;

			// set the flag as active
			markStrafeWithFlareShellActive = true;

			// display prompt on screen
			GTA.UI.Notification.Show("Mark target position of strafe run with the ~o~flare gun.");
		}

		/// <summary>
		/// while markStrafeWithFlareShellActive is true, call this method; If a flare shell is found,
		/// and it has collided with something, launch strafe run at the flare's position.
		/// </summary>
		/// <returns><c>true</c> if a strafe run was spawned</returns>
		private bool markStrafeRunWithFlareGunListener()
		{
			Prop[] nearbyFlareShells = World.GetNearbyProps(Game.Player.Character.Position, 300f, flareShellModel);
			if (nearbyFlareShells.Length > 0)
			{
				if (nearbyFlareShells[0].HasCollided || nearbyFlareShells[0].Speed < 1.0f)
				{
					strafeRun.spawnStrafeRun(nearbyFlareShells[0].Position);
					markStrafeWithFlareShellActive = false;
					nearbyFlareShells[0].Delete();
					return true;
				}
			}

			return false;
		}
	}
}


// Useful Links
// All Vehicles - https://pastebin.com/uTxZnhaN
// All Player Models - https://pastebin.com/i5c1zA0W
// All Weapons - https://pastebin.com/M3kD9pnJ
// GTA V ScriptHook V Dot Net - https://www.gta5-mods.com/tools/scripthookv-net