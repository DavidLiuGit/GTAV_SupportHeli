using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTA;
using GTA.Math;


namespace GFPS
{
	public class Heli
	{
		// settings
		protected HeliModel model;
		protected float height;
		protected float radius;
		protected bool bulletproof;

		// flags 
		protected bool isAttackHeli;
		public bool isActive = false;
		public bool pilotHoldPosition = false;
		protected bool canRappel = false;

		// consts
		protected const WeaponHash sidearm = WeaponHash.Pistol;
		protected const FiringPattern fp = FiringPattern.FullAuto;

		// object references
		public Vehicle heli;
		public Ped pilot;
		public Ped[] passengers;
		public RelationshipGroup rg;
		protected Random rng = new Random();



		#region constructorDestructor
		/// <summary>
		/// Instantiate Heli with string settings read in from INi file. If the settings are invalid, use default settings.
		/// </summary>
		/// <param name="iniName">Model name of </param>
		/// <param name="iniHeight"></param>
		/// <param name="iniRadius"></param>
		/// <param name="iniBulletproof"></param>
		public Heli(string iniName, string iniHeight, string iniRadius, string iniBulletproof)
			: this (
				(HeliModel) Enum.Parse(typeof(HeliModel), iniName ?? "Akula" ),
				float.Parse(iniHeight ?? "30.0"),
				float.Parse(iniRadius ?? "20.0"),
				Convert.ToBoolean(int.Parse(iniBulletproof ?? "1"))
			)
		{}

		public Heli(HeliModel m, float h, float r, bool bp)
		{
			model = m;
			height = h;
			radius = r;
			bulletproof = bp;

			// instantiate a relationship group
			rg = Game.Player.Character.RelationshipGroup;
		}


		/// <summary>
		/// Clean up remnants of the heli.
		/// </summary>
		/// <param name="force">If <c>true</c>, all occupants (pilot & crew) will be deleted as well</param>
		public void destructor(bool force = false)
		{
			isActive = false;

			try
			{
				// delete the blip
				heli.AttachedBlip.Delete();

				// if destroying by force
				if (force)
				{
					pilot.Delete();
					foreach (Ped passenger in passengers)
						passenger.Delete();
					heli.Delete();
				} else
					heli.MarkAsNoLongerNeeded();
			}
			catch { }
		}
		#endregion



		#region publicMethods
		/// <summary>
		/// Spawn in a new helicopter with its pilot inside.
		/// </summary>
		/// <returns>Instance of <c>Vehicle</c> spawned in</returns>
		public Vehicle spawnMannedHeli()
		{
			// if a heli is already active, return immediately
			if (isActive)
			{
				GTA.UI.Notification.Show("Heli is already active.");
			}

			// otherwise, spawn a heli and place a pilot in the driver seat
			else
			{
				heli = spawnHeli(Helper.getOffsetVector3(height, radius));
				pilot = spawnPilotIntoHeli();
				passengers = spawnCrewIntoHeli();
				isActive = true;
			}
			
			return this.heli;
		}


		/// <summary>
		/// Get summary of settings in a string.
		/// </summary>
		/// <returns>Summary of settings</returns>
		public string getSettingsString()
		{
			return string.Format("Heli: {0}~n~ Height: {1}~n~ Radius: {2}~n~ Bulletproof: {3}",
				model, height, radius, bulletproof);
		}



		/// <summary>
		/// Perform pre-flight checks, then task the pilot with flying to the player's location.
		/// </summary>
		public void flyToPlayer() {
			// if heli is not active or pilot is being instructed to hold position, do nothing
			if (!isActive || pilotHoldPosition)
				return;

			// if heli is not driveable or the pilot is no longer in the heli
			else if (!heli.IsDriveable)
			{
				destructor(false);
				return;
			}

			try
			{
				Vector3 playerPos = Game.Player.Character.Position;
				pilot.Task.ChaseWithHelicopter(Game.Player.Character, Helper.getOffsetVector3(height, radius));
				pilot.AlwaysKeepTask = true;
			}
			catch
			{
				destructor();
				throw;
			}

		}



		/// <summary>
		/// Task pilot with holding a position, instead of "orbitting" the player as normal.
		/// </summary>
		public void holdPositionAbovePlayer()
		{
			pilot.AlwaysKeepTask = false;
			Vector3 positionToHold = Game.Player.Character.Position + Helper.getOffsetVector3(height, radius);
			pilot.Task.DriveTo(heli, positionToHold, 2.5f, 20.0f);
		}
		#endregion



		#region helpers
		
		/// <summary>
		/// Spawn a helicopter at the offset relative to the player.
		/// </summary>
		/// <param name="mdl">Model of the helicopter to spawn</param>
		/// <param name="offset">offset, relative to the player, as a vector</param>
		/// <returns>Instance of <c>Vehicle</c></returns>
		protected Vehicle spawnHeli(Vector3 offset)
		{
			// spawn in heli and apply settings
			Vehicle heli = World.CreateVehicle((Model)((int)model), Game.Player.Character.Position + offset);
			heli.IsEngineRunning = true;
			heli.HeliBladesSpeed = 1.0f;
			heli.LandingGearState = VehicleLandingGearState.Retracted;
			heli.IsBulletProof = bulletproof;

			// attach a blip (rotating helicopter)
			heli.AddBlip();
			heli.AttachedBlip.Sprite = BlipSprite.HelicopterAnimated;
			heli.AttachedBlip.Color = BlipColor.Green;

			return heli;
		}


		/// <summary>
		/// Spawn a pilot into the driver seat of the helicopter
		/// </summary>
		/// <param name="veh">Instance of a helicopter</param>
		/// <returns>Newly-spawned pilot</returns>
		protected Ped spawnPilotIntoHeli()
		{
			// spawn pilot & set into heli driver seat
			Ped pilot = heli.CreatePedOnSeat(VehicleSeat.Driver, PedHash.Pilot01SMY);
			pilot.CanBeDraggedOutOfVehicle = false;

			// create a heli RelationshipGroup and add pilot to it; make heli and player's group allies
			pilot.RelationshipGroup = rg;

			// give pilot sidearm
			pilot.Weapons.Give(sidearm, 9999, true, true);

			// task pilot with chasing player with heli always
			flyToPlayer();

			return pilot;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="seat"></param>
		/// <param name="weaponArray"></param>
		/// <returns></returns>
		protected Ped spawnCrewGunner(VehicleSeat seat, WeaponHash[] weaponArray)
		{
			// if seat is occupied, delete the NPC in the seat
			Ped seatOccupant = heli.GetPedOnSeat(seat);
			if (seatOccupant != null)
				seatOccupant.Delete();

			// spawn the crew into the specified seat
			Ped gunner = heli.CreatePedOnSeat(seat, PedHash.Blackops01SMY);

			// ally the crew to the player
			gunner.RelationshipGroup = rg;

			// give crew weapons in weaponArray, plus a standard issue sidearm
			giveWeapons(gunner, weaponArray);

			// task crew with fighting any enemies
			gunner.FiringPattern = fp;
			gunner.Task.FightAgainstHatedTargets(99999);
			gunner.AlwaysKeepTask = true;
			gunner.CanRagdoll = false;
			gunner.CanWrithe = false;

			return gunner;
		}



		protected virtual void giveWeapons (Ped crew, WeaponHash[] weaponArray) {
			crew.Weapons.Give(sidearm, 9999, true, true);
			foreach (WeaponHash weapon in weaponArray)
				crew.Weapons.Give(weapon, 9999, true, true);

			// automatically select the best weapon
			crew.Weapons.Select(crew.Weapons.BestWeapon);
		}


		#region virtualHelpers
		/// <summary>
		/// Based on the model of the heli, spawn in the required personel.
		/// </summary>
		/// <returns>Array of NPCs spawned into the heli</returns>
		protected virtual Ped[] spawnCrewIntoHeli()
		{
			return new Ped[0];
		}
		#endregion
		#endregion
	}



	public class Attackheli : Heli
	{
		// by default, give each (non-copilot) crew assault weapons
		WeaponHash[] gunnerWeapons = CrewHandler.weaponsOfRoles[GroundCrewRole.Assault];

		public Attackheli(string iniName, string iniHeight, string iniRadius, string iniBulletproof) :
			base(iniName, iniHeight, iniRadius, iniBulletproof)
		{
			isAttackHeli = true;
		}


		#region helpers
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
		#endregion
	}





	public class SupportHeli : Heli
	{
		// by default, give each (non-copilot) crew heavy weapons
		WeaponHash[] gunnerWeapons = CrewHandler.weaponsOfRoles[GroundCrewRole.Heavy];
		const float blipScale = 0.7f;
		public PedGroup playerPedGroup;
		protected const int maxConfigureAttempts = 5;
		
		protected int seatIndex = 0;
		protected VehicleSeat[] seatSelection;


		public SupportHeli (string iniName, string iniHeight, string iniRadius, string iniBulletproof) :
			base(iniName, iniHeight, iniRadius, iniBulletproof)
		{ 
			isAttackHeli = false;

			// get the player's current PedGroup (or create a new one if player is not in one)
			playerPedGroup = Game.Player.Character.PedGroup;
			if (playerPedGroup == null)
			{
				playerPedGroup = new PedGroup();
				playerPedGroup.Add(Game.Player.Character, true);
			}
			playerPedGroup.SeparationRange = 99999f;
			playerPedGroup.Formation = Formation.Circle2;

			// set the list of seats (based on helicopter model, but temporarily all the same)
			seatSelection = new VehicleSeat[] { VehicleSeat.LeftRear, VehicleSeat.RightRear };
		}



		#region publicMethods
		/// <summary>
		/// Spawn and task 2 ground crew NPCs to rappel down from the SupportHeli. If no <c>GroundCrewRole</c>
		/// is specified, one will be chosen at random.
		/// </summary>
		/// <returns>Array of <c>Ped</c></returns>
		public Ped[] groundCrewRappelDown()
		{
			Array roles = Enum.GetValues(typeof(GroundCrewRole));
			return groundCrewRappelDown((GroundCrewRole)roles.GetValue(rng.Next(0, roles.Length)));
		}


		/// <summary>
		/// Task SupportHeli gunners to rappel down to the ground (and become ground crew).
		/// </summary>
		/// <param name="role">Ground crew role; weapons are assigned based on role</param>
		/// <returns>Array of <c>Ped</c> rappeling</returns>
		public Ped[] groundCrewRappelDown(GroundCrewRole role)
		{
			// make sure there are gunners in the crew seats
			Ped[] newGroundCrew = new Ped[] {
				spawnCrewGunner(seatSelection[seatIndex % seatSelection.Length], CrewHandler.weaponsOfRoles[role]),
				//spawnCrewGunner(VehicleSeat.RightRear, CrewHandler.weaponsOfRoles[role]),
			};
			seatIndex++;

			// instruct gunners to rappel
			foreach (Ped crew in newGroundCrew)
			{
				crew.Task.RappelFromHelicopter();

				bool res;
				int i = 0;
				do { 
					res = configureGroundCrew(crew); 
					i++;
				}		// if unsuccessful, do again until it does succeed
				while (!res && i < maxConfigureAttempts);
			}

			GTA.UI.Notification.Show("Active Ground Crew: " + playerPedGroup.MemberCount);
			return newGroundCrew;
		}
		#endregion



		#region helpers
		protected override Ped[] spawnCrewIntoHeli()
		{
			List<Ped> newCrew = new List<Ped>();

			switch (model)
			{
				case HeliModel.Polmav:
				case HeliModel.Maverick:
				default:
					newCrew.Add(spawnCrewGunner(VehicleSeat.LeftRear, gunnerWeapons));
					newCrew.Add(spawnCrewGunner(VehicleSeat.RightRear, gunnerWeapons));
					break;
			}

			return newCrew.ToArray<Ped>();
		}



		protected override void giveWeapons(Ped crew, WeaponHash[] weaponArray)
		{
			// give sidearms in addition to primaries
			foreach (WeaponHash sidearm in CrewHandler.sidearms)
				crew.Weapons.Give(sidearm, 9999, false, true);
			base.giveWeapons(crew, weaponArray);
		}



		protected bool configureGroundCrew(Ped crew)
		{
			// add to player's PedGroup if there is space
			playerPedGroup.Add(crew, false);
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


	public enum HeliModel : int
	{
		Akula = 0x46699F47,
		Valkyrie = -1600252419,
		Buzzard = 0x2F03547B,
		Maverick = -1660661558,
		Polmav = 353883353,
		Hunter = -42959138,
	}
}
