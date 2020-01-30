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
		IniFile ini;
		Keys activateKey = Keys.F10;

		public Main()
		{
			Tick += onTick;
			KeyDown += onKeyDown;
			Interval = 1;
			Tick += onNthTick;

			// read in settings
			ini = new IniFile("scripts/SupportHeli.ini");
			activateKey = (Keys) Enum.Parse(typeof(Keys), ini.Read("hotkey"));
			heliModel = (SupportHeliModel)Enum.Parse(typeof(SupportHeliModel), ini.Read("heliModel"));
			supportHeliHeight = float.Parse(ini.Read("height"));
			haloRadius = float.Parse(ini.Read("radius"));
			heliBulletProof = int.Parse(ini.Read("bulletproof")) > 0 ? true : false;
		}


		private void onTick(object sender, EventArgs e)
		{
			if (firstTime) // if this is the users first time loading the mod, this information will appear
			{
				GTA.UI.Notification.Show(ModName + " " + Version + " by " + Developer + " Loaded");
				firstTime = false;
			}
		}

		private void onKeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == activateKey)
			{
				spawnSupportHeli(SupportHeliModel.Akula, supportHeliHeight, haloRadius, fPatt, heliBulletProof);
			}
		}



		int iTick = 0;
		int N = 1000;
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
				{
					heliActive = false;
					supportHeli.AttachedBlip.Delete();
				}

				// make the driver chase to player's offset
				supportHeli.Driver.Task.ChaseWithHelicopter(Game.Player.Character, getOffsetVector3(supportHeliHeight, haloRadius));
				supportHeli.Driver.AlwaysKeepTask = true;
			}
		}





		bool heliActive = false;
		SupportHeliModel heliModel;
		Vehicle supportHeli;
		float haloRadius = 20.0f;
		float supportHeliHeight = 40.0f;
		FiringPattern fPatt = FiringPattern.FullAuto;
		readonly Vector3 nullVector3 = new Vector3(0f, 0f, 0f);
		bool heliBulletProof = true;

		public Vehicle spawnSupportHeli(SupportHeliModel model, float height, float radius, FiringPattern fp, bool bulletProof)
		{
			// if a heli is already active, return immediately
			if (heliActive)
			{
				GTA.UI.Notification.Show("Support heli is already active.");
				return supportHeli;
			}

			// determine player position
			Vector3 playerPos = Game.Player.Character.Position;
			playerPos.Z += height;

			// spawn a heli
			Vehicle heli = World.CreateVehicle(0x46699F47, playerPos);
			heli.IsBulletProof = true;
			heli.AddBlip();
			heli.AttachedBlip.Sprite = BlipSprite.HelicopterAnimated;
			heli.AttachedBlip.Color = BlipColor.Green;
			supportHeli = heli;

			// activate engine & freeze the entity so it doesn't fall
			heli.IsEngineRunning = true;
			heli.IsPositionFrozen = true;
			Script.Wait(500);
			heli.IsPositionFrozen = false;

			// spawn pilot & copilot
			Ped pilot = World.CreatePed(PedHash.Pilot01SMY, nullVector3);
			pilot.SetIntoVehicle(heli, VehicleSeat.Driver);
			Ped coplt = World.CreatePed(PedHash.Pilot02SMM, nullVector3);
			coplt.SetIntoVehicle(heli, VehicleSeat.Passenger);

			// add pilot & copilot to player's RelationshipGroup
			RelationshipGroup playerRelGroup = Game.Player.Character.RelationshipGroup;
			pilot.RelationshipGroup = playerRelGroup;
			coplt.RelationshipGroup = playerRelGroup;

			// task pilot with chasing player with heli always
			pilot.Task.ChaseWithHelicopter(Game.Player.Character, playerPos);
			pilot.AlwaysKeepTask = true;

			// task copilot with shooting baddies using the gun
			coplt.FiringPattern = FiringPattern.BurstFireHeli;
			coplt.Task.FightAgainstHatedTargets(50000);
			coplt.AlwaysKeepTask = true;

			// mark support heli as active and return the vehicle instance
			heliActive = true;
			return heli;
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
	}



	public enum SupportHeliModel : uint 
	{
		Akula = 0x46699F47,
		Valkyrie = 2694714877u,
		Buzzard = 788747387u,
	}
}


// Useful Links
// All Vehicles - https://pastebin.com/uTxZnhaN
// All Player Models - https://pastebin.com/i5c1zA0W
// All Weapons - https://pastebin.com/M3kD9pnJ
// GTA V ScriptHook V Dot Net - https://www.gta5-mods.com/tools/scripthookv-net