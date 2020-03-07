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
			// activateKey pressed
			if (e.KeyCode == activateKey)
			{
				// Shift also pressed
				if (e.Modifiers == Keys.Shift)
				{
					// if supportHeli is not active, spawn one
					if (!supportHeli.isActive)
						supportHeli.spawnMannedHeli();

					// otherwise, task gunners with rappeling down
					else
						gunnersRappelDown();
				}

				// Delete also pressed
				else if (Game.IsKeyPressed(Keys.Delete))
					cleanUp(false);			// soft clean-up (helis fly away)

				// PgDown also pressed
				else if (Game.IsKeyPressed(Keys.PageDown))
					supportHeli.pilotTasking(Heli.HeliPilotTask.Land);

				// PgUp also pressed
				else if (Game.IsKeyPressed(Keys.PageUp))
					supportHeli.pilotTasking(Heli.HeliPilotTask.ChasePed);

				// Tab also pressed
				else if (Game.IsKeyPressed(Keys.Tab))
					supportHeli.pilotTasking(Heli.HeliPilotTask.FlyToDestination);

				// no modifiers
				else
					attackHeli.spawnMannedHeli();
			}

		}



		int iTick = 0;
		int N = 200;
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

			// handle ground crew actions
			updateGroundCrewActions();
		}
		

		// instances of Heli to track
		Attackheli attackHeli;
		SupportHeli supportHeli;
		GroundCrewSettings crewSettings = new GroundCrewSettings();

		/// <summary>
		/// Read settings from INI file and instantiate necessary data structures with the settings.
		/// </summary>
		private void readSettings (bool verbose = false) {
			// read in general settings
			string sec = "General";
			activateKey = (Keys)Enum.Parse(typeof(Keys), ini.Read("hotkey", sec) ?? "F10");
			
			// read in settings for Attack Heli
			sec = "AttackHeli";
			attackHeli = new Attackheli(ini.Read("heliModel", sec), ini.Read("height", sec), ini.Read("radius", sec), ini.Read("bulletproof", sec));
			RelationshipGroup heliRg = attackHeli.rg;

			// read in settings for Support Heli
			sec = "SupportHeli";
			supportHeli = new SupportHeli(ini.Read("heliModel", sec), ini.Read("height", sec), ini.Read("radius", sec), ini.Read("bulletproof", sec));
			supportHeli.rg = heliRg;

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

		private void cleanUp(bool force)
		{
			// clean up helis
			attackHeli.destructor(force);
			supportHeli.destructor(force);
			
			// clean up any ground crew
			PedGroup playerPedGrp = Game.Player.Character.PedGroup;
			foreach (Ped p in groundCrew.Keys.ToArray())
			{
				playerPedGrp.Remove(p);			// remove the ped from player's PedGroup
				CrewHandler.crewDestructor(p, force);
			}
		}
	}

}


// Useful Links
// All Vehicles - https://pastebin.com/uTxZnhaN
// All Player Models - https://pastebin.com/i5c1zA0W
// All Weapons - https://pastebin.com/M3kD9pnJ
// GTA V ScriptHook V Dot Net - https://www.gta5-mods.com/tools/scripthookv-net