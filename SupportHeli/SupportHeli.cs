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
			if (e.KeyCode == activateKey)
			{
				supportHeli = spawnSupportHeli(heliModel, supportHeliHeight, haloRadius, heliBulletProof);
				spawnNpcsIntoHeli(supportHeli, heliModel, fPatt);
			}
		}



		int iTick = 0;
		int N = 100;
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
			if (heliActive)
			{
				// if heli is destroyed (undriveable), delete its blip and set heliActive to false
				if (!supportHeli.IsDriveable)
					cleanUp();

				// make the driver chase to player's offset
				try
				{
					supportHeli.Driver.Task.ChaseWithHelicopter(Game.Player.Character, getOffsetVector3(supportHeliHeight, haloRadius));
					supportHeli.Driver.AlwaysKeepTask = true;
				}
				catch
				{
					cleanUp();
				}
			}
		}





		bool heliActive = false;
		SupportHeliModel heliModel;
		Vehicle supportHeli;
		float haloRadius = 20.0f;
		float supportHeliHeight = 40.0f;
		readonly Vector3 nullVector3 = new Vector3(0f, 0f, 0f);
		bool heliBulletProof = true;
		const WeaponHash defaultPrimary = WeaponHash.AdvancedRifle;
		const WeaponHash defaultSidearm = WeaponHash.CombatPistol;

		public Vehicle spawnSupportHeli(SupportHeliModel model, float height, float radius, bool bulletProof)
		{
			// if a heli is already active, return immediately
			if (heliActive)
			{
				GTA.UI.Notification.Show("Support heli is already active.");
				return supportHeli;
			}

			// determine player position
			Vector3 heliPos = Game.Player.Character.Position + getOffsetVector3(70f, radius);

			// spawn a heli
			Vehicle heli = World.CreateVehicle((Model)((int)model), heliPos);
			heli.IsBulletProof = true;
			heli.AddBlip();
			heli.AttachedBlip.Sprite = BlipSprite.HelicopterAnimated;
			heli.AttachedBlip.Color = BlipColor.Green;
			
			// activate engine & freeze the entity so it doesn't fall
			heli.IsEngineRunning = true;

			// mark support heli as active and return the vehicle instance
			heliActive = true;
			return heli;
		}



		FiringPattern fPatt = FiringPattern.FullAuto;
		private void spawnNpcsIntoHeli(Vehicle heli, SupportHeliModel model, FiringPattern fp)
		{
			// spawn pilot & set into heli
			Ped pilot = World.CreatePed(PedHash.Pilot01SMY, nullVector3);
			pilot.SetIntoVehicle(heli, VehicleSeat.Driver);

			// add pilot to player's RelationshipGroup
			RelationshipGroup playerRelGroup = Game.Player.Character.RelationshipGroup;
			pilot.RelationshipGroup = playerRelGroup;

			// task pilot with chasing player with heli always
			Vector3 playerPos = Game.Player.Character.Position;
			pilot.Task.ChaseWithHelicopter(Game.Player.Character, getOffsetVector3(supportHeliHeight, haloRadius));
			pilot.AlwaysKeepTask = true;

			// give pilot sidearm
			pilot.Weapons.Give(defaultSidearm, 9999, true, true);

			// if multi-seat heli, spawn more shooters
			switch (model)
			{
				case SupportHeliModel.Akula:
					spawnGunners(heli, (int)model, 0);
					break;

				case SupportHeliModel.Valkyrie:
					spawnGunners(heli, (int) model, 2, WeaponHash.AdvancedRifle);
					break;

				case SupportHeliModel.Buzzard:
					spawnGunners(heli, (int) model, 2, WeaponHash.AdvancedRifle);
					break;

			}
		}



		private void spawnGunners(Vehicle heli, int model, int num, WeaponHash weapon = defaultPrimary, WeaponHash sidearm = WeaponHash.CombatPistol)
		{
			// spawn copilot if needed
			if (model == (int) SupportHeliModel.Akula || model == (int) SupportHeliModel.Valkyrie)
			{
				// spawn copilot & set into heli
				Ped coplt = World.CreatePed(PedHash.Pilot02SMM, nullVector3);
				coplt.SetIntoVehicle(heli, VehicleSeat.Passenger);

				// set relationship group to player's group
				RelationshipGroup playerRelGroup = Game.Player.Character.RelationshipGroup;
				coplt.RelationshipGroup = playerRelGroup;
				Game.Player.Character.PedGroup.Add(coplt, false);

				// give copilot sidearm
				coplt.Weapons.Give(defaultSidearm, 9999, true, true);

				// task copilot with shooting baddies
				coplt.Task.FightAgainstHatedTargets(50000);
				coplt.AlwaysKeepTask = true;

				// set copilot firing mode, depending on the model
				if (model == (int)SupportHeliModel.Akula) coplt.FiringPattern = FiringPattern.FullAuto;
				else if (model == (int)SupportHeliModel.Valkyrie) coplt.FiringPattern = FiringPattern.BurstFireHeli;
			}


			// spawn gunners
			for (int i = 0; i < num; i++)
			{
				// spawn the gunner and put into a seat
				Ped gunner = World.CreatePed(PedHash.Blackops01SMY, nullVector3);
				gunner.SetIntoVehicle(heli, VehicleSeat.Any);
				gunner.FiringPattern = FiringPattern.FullAuto;

				// give the gunner weapons & sidearm
				gunner.Weapons.Give(weapon, 9999, true, true);
				gunner.Weapons.Give(sidearm, 9999, true, true);

				// set the gunner to fight
				gunner.Task.FightAgainstHatedTargets(50000);
				gunner.AlwaysKeepTask = true;
			}
		}



		private Vector3 getOffsetVector3 (float height, float haloRadius = 0.0f){
			float x = 0.0f, y = 0.0f;

			// if haloRadius is > 0.0, then randomly generate x & y within the radius
			if (haloRadius > 0.0f){
				Random rand = new Random();
				double x2 = rand.NextDouble() * haloRadius * haloRadius;			// 0.0 < x < haloRadius^2
				double y2 = Math.Pow(haloRadius, 2.0f) - x2;						// y = haloRadius^2 - x
				x = (float) Math.Sqrt(x2);
				y = (float) Math.Sqrt(y2);
			}

			return new Vector3(x, y, height);
		}


		
		private void readSettings () {
			activateKey = (Keys)Enum.Parse(typeof(Keys), ini.Read("hotkey") ?? "F10");
			heliModel = (SupportHeliModel)Enum.Parse(typeof(SupportHeliModel), ini.Read("heliModel") ?? "Akula");
			supportHeliHeight = float.Parse(ini.Read("height") ?? "40.0");
			haloRadius = float.Parse(ini.Read("radius") ?? "20.0");
			heliBulletProof = int.Parse(ini.Read("bulletproof") ?? "1") > 0 ? true : false;

			GTA.UI.Notification.Show("Heli: " + heliModel + "~n~Height: " + supportHeliHeight + "~n~Radius: " + haloRadius);
		}



		private void cleanUp()
		{
			heliActive = false;
			supportHeli.AttachedBlip.Delete();
		}
	}



	public enum SupportHeliModel : int 
	{
		Akula = 0x46699F47,
		Valkyrie = -1600252419,
		Buzzard = 0x2F03547B,
	}
}


// Useful Links
// All Vehicles - https://pastebin.com/uTxZnhaN
// All Player Models - https://pastebin.com/i5c1zA0W
// All Weapons - https://pastebin.com/M3kD9pnJ
// GTA V ScriptHook V Dot Net - https://www.gta5-mods.com/tools/scripthookv-net