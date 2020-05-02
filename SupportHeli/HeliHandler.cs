using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTA;
using GTA.Math;
using GTA.Native;


namespace GFPS
{
	public class Heli
	{
		// settings
		protected Model _model;
		protected float _height;
		protected float _radius;
		protected bool _isBulletproof;

		// flags 
		protected bool _isActive = false;
		protected bool canRappel = false;
		protected bool pilotLand = false;

		// consts
		protected const WeaponHash sidearm = WeaponHash.Pistol;
		protected const FiringPattern fp = FiringPattern.FullAuto;
		protected const float warpIntoDistanceThreshold = 6f;
		protected const float cruiseAltitudeMultiplier = 1.5f;		// when cruising, heli will fly at a different _height
		protected const BlipColor defaultBlipColor = BlipColor.Green;
		protected virtual Model fallbackModel { get { return (Model) VehicleHash.Buzzard; } }

		// object references
		protected Ped _leader;
		public Vehicle heli;
		public Ped pilot;
		public Ped[] passengers;
		public RelationshipGroup _rg;
		protected Random rng = new Random();

		// state machines
		protected HeliPilotTask _pilotTask;

		// accessors & mutators
		public HeliPilotTask pilotTask { get { return _pilotTask; }	}
		public Ped leader { get { return _leader; } }
		public bool isActive { get { return _isActive; } }

		// pilot tasking
		public enum HeliPilotTask : int
		{
			ChaseLeader,
			HoldPosition,
			Land,
			FleePed,			// flee from a ped; used during soft delete
			FlyToDestination,
			ChaseEngagePed,
		}

		

		#region constructorDestructor
		/// <summary>
		/// Instantiate Heli with parameters.
		/// </summary>
		/// <param name="model">Model name of the helicopter to use</param>
		/// <param name="height">hover _height of the helicopter</param>
		/// <param name="radius">hover _radius of the helicopter</param>
		/// <param name="bulletproof">Whether the helicopter is _isBulletproof</param>
		public Heli(string model, float height, float radius, bool bulletproof)
		{
			//_model = model;
			_model = (Model)Game.GenerateHash(model);
			_height = height;
			_radius = radius;
			_isBulletproof = bulletproof;

			// instantiate a relationship group
			_leader = Game.Player.Character;
			_rg = _leader.RelationshipGroup;
		}


		/// <summary>
		/// Clean up remnants of the heli.
		/// </summary>
		/// <param name="force">If <c>true</c>, all occupants (pilot & crew) will be deleted as well</param>
		public virtual void destructor(bool force = false)
		{
			_isActive = false;

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
				}

				// if destroying gracefully, command the pilot to fly away. Mark the Heli and crew as no longer needed.
				else
				{
					pilot.Task.FleeFrom(_leader);
					pilot.MarkAsNoLongerNeeded();
					foreach (Ped passenger in passengers)
						passenger.MarkAsNoLongerNeeded();
					heli.MarkAsNoLongerNeeded();
				}
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
			if (_isActive)
				GTA.UI.Notification.Show("Heli is already active.");

			// otherwise, spawn a heli and place a pilot in the driver seat
			else
			{
				heli = spawnHeli(Helper.getOffsetVector3(_height, _radius), _model);
				pilot = spawnPilotIntoHeli();
				passengers = spawnCrewIntoHeli();
				_isActive = true;
			}
			
			return this.heli;
		}

		/// <summary>
		/// Spawn in a helicopter that follows the specified <c>Ped</c>, along with helicopter's pilot
		/// </summary>
		/// <param name="leader"><c>Ped</c> to follow</param>
		/// <returns></returns>
		public Vehicle spawnMannedHeli(Ped leader)
		{
			_leader = leader;
			return spawnMannedHeli();
		}



		/// <summary>
		/// Perform sanity checks, then assign task to the pilot if necessary
		/// </summary>
		/// <param name="nextTask">Specify to update the pilot's task</param>
		public virtual void pilotTasking(HeliPilotTask? nextTask = null) {
			if (!isHeliServiceable())
				return;

			// if nextTask is specified, attempt to make pilot perform that task
			if (nextTask != null){
				switch (nextTask)
				{
					case HeliPilotTask.ChaseLeader:
						pilotTaskChasePed(); break;

					case HeliPilotTask.Land:
						landNearLeader(); break;

					case HeliPilotTask.FlyToDestination:
						flyToDestination(null); break;
				}
			}

			// if nextTask is NOT specified (i.e. null)
			else
			{
				// task the pilot based on the currently active task
				switch (_pilotTask)
				{
					case HeliPilotTask.ChaseLeader:
						pilotTaskChasePed(); break;

					case HeliPilotTask.Land:
						heliLandingHandler(); break;
				}
			}
		}



		/// <summary>
		/// Task pilot with holding a position, instead of "orbitting" the player as normal.
		/// </summary>
		public void holdPositionAbovePlayer()
		{
			pilot.AlwaysKeepTask = false;
			Vector3 positionToHold = _leader.Position + Helper.getOffsetVector3(_height, _radius);
			pilot.Task.DriveTo(heli, positionToHold, 2.5f, 20.0f);
		}



		/// <summary>
		/// Task the pilot with landing the heli near the <c>Ped</c> specified.
		/// </summary>
		/// <param name="p">Ped to land near</param>
		/// <param name="maxSpeed">max speed</param>
		/// <param name="targetRadius">how close the heli should be landed to the ped</param>
		public void landNearLeader(float maxSpeed = 100f, float targetRadius = 3f, bool verbose = true)
		{
			Vector3 pedPos = Helper.getVector3NearTarget(targetRadius, _leader.Position);
			const int missionFlag = 20;			// 20 = LandNearPed
			const int landingFlag = 8225;			// 32 = Land on destination

			/* void TASK_HELI_MISSION(Ped pilot, Vehicle aircraft, Vehicle targetVehicle, Ped targetPed, 
			float destinationX, float destinationY, float destinationZ, int missionFlag, float maxSpeed, 
			 * float landingRadius, float targetHeading, int unk1, int unk2, Hash unk3, int landingFlags)
			 */
			Function.Call(Hash.TASK_HELI_MISSION, pilot, heli, 0, 0,
				pedPos.X, pedPos.Y, pedPos.Z - 5f, missionFlag, maxSpeed,
				targetRadius, (pedPos - heli.Position).ToHeading(), -1, -1, -1, landingFlag);

			// update the pilot's task
			pilot.BlockPermanentEvents = true;
			_pilotTask = HeliPilotTask.Land;
			if (verbose) GTA.UI.Notification.Show("Heli: landing near player");

			// mark the landing zone with a flare
			float gndHeight = World.GetGroundHeight((Vector2)pedPos);
		}



		/// <summary>
		/// Fly to the destination. If no destination is specified, fly to waypoint. If no
		/// waypoint is set, hover above the ground in the current position.
		/// </summary>
		public void flyToDestination(Vector3? destination = null, float maxSpeed = 100f)
		{
			_pilotTask = HeliPilotTask.FlyToDestination;
			Vector3 target;

			// if a destination was specified, fly there
			if (destination != null)
				target = destination ?? Vector3.Zero;

			// if no destination was specified, but a waypoint is active, fly to waypoint
			else if (Game.IsWaypointActive)
			{
				target = World.WaypointPosition;
				GTA.UI.Notification.Show("Support Heli: flying to waypoint");
			}

			// otherwise, set the target to some point above the current position
			else
			{
				target = heli.Position + Helper.getOffsetVector3(_height);
				GTA.UI.Notification.Show("Support Heli: hovering.");
			}

			int cruiseAltitude = Convert.ToInt32(cruiseAltitudeMultiplier * _height);

			/* void TASK_HELI_MISSION(Ped pilot, Vehicle aircraft, Vehicle targetVehicle, Ped targetPed, 
			float destinationX, float destinationY, float destinationZ, int missionFlag, float maxSpeed, 
			 * float landingRadius, float targetHeading, int unk1, int unk2, Hash unk3, int landingFlags)
			 */
			Function.Call(Hash.TASK_HELI_MISSION, pilot, heli, 0, 0,
				target.X, target.Y, target.Z, 4, maxSpeed,
				10f, (target - heli.Position).ToHeading(), cruiseAltitude, cruiseAltitude, -1, 0);
		}
		#endregion




		#region helpers
		/// <summary>
		/// Check whether the heli is serviceable.
		/// </summary>
		/// <returns><c>true</c> if heli is active and serviceable.</returns>
		protected bool isHeliServiceable (){
			// if heli is not active, do nothing
			if (!_isActive)
				return false;

			// if heli is not driveable or the pilot is no longer in the heli
			else if (!heli.IsDriveable || !pilot.IsInVehicle(heli))
			{
				destructor(false);
				return false;
			}

			return true;
		}



		/// <summary>
		/// Task the heli's pilot with chasing the specified <c>Ped</c>, with some preset offset
		/// </summary>
		/// <param name="p">Ped to chase</param>
		protected void pilotTaskChasePed()
		{
			_pilotTask = HeliPilotTask.ChaseLeader;
			try
			{
				Vector3 playerPos = _leader.Position;
				pilot.Task.ChaseWithHelicopter(_leader, Helper.getOffsetVector3(_height, _radius));
				pilot.AlwaysKeepTask = true;
			}
			catch
			{
				destructor();
				throw;
			}
		}


		
		/// <summary>
		/// Spawn a helicopter at the offset relative to the player.
		/// </summary>
		/// <param name="mdl">Model of the helicopter to spawn</param>
		/// <param name="offset">offset, relative to the player, as a vector</param>
		/// <returns>Instance of <c>Vehicle</c></returns>
		protected Vehicle spawnHeli(Vector3 offset, Model model)
		{
			// spawn in heli
			Vehicle heli = World.CreateVehicle(model, _leader.Position + offset);

			// verify that the vehicle was successfully spawned
			if (heli == null)
			{
				// check if the model is the same as the fallback model
				if (model == fallbackModel)
				{
					GTA.UI.Notification.Show("~r~Failed the spawn default vehicle. Check that you have required DLC.");
					return null;
				}

				// otherwise, try again with the fallback model
				GTA.UI.Notification.Show("~r~Failed to spawn the vehicle. " +
					"Check that you have you spelled the vehicle name correctly in INI. Falling back to default model");
				return spawnHeli(offset, fallbackModel);
			}

			// apply settings to heli
			heli.IsEngineRunning = true;
			heli.HeliBladesSpeed = 1.0f;
			heli.LandingGearState = VehicleLandingGearState.Retracted;
			heli.IsBulletProof = _isBulletproof;

			// attach a blip (rotating helicopter)
			heli.AddBlip();
			heli.AttachedBlip.Sprite = BlipSprite.HelicopterAnimated;
			heli.AttachedBlip.Color = defaultBlipColor;

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
			pilot = heli.CreatePedOnSeat(VehicleSeat.Driver, PedHash.Pilot01SMY);
			pilot.CanBeDraggedOutOfVehicle = false;

			// create a heli RelationshipGroup and add pilot to it; make heli and player's group allies
			pilot.RelationshipGroup = _rg;

			// give pilot sidearm
			pilot.Weapons.Give(sidearm, 9999, true, true);

			// task pilot with chasing player with heli always
			pilotTaskChasePed();

			return pilot;
		}



		/// <summary>
		/// Spawn an allied <c>Ped</c> into a seat of the Heli, and give the specified weapons
		/// </summary>
		/// <param name="seat">Seat to spawn Ped into</param>
		/// <param name="weaponArray">Array of <c>WeaponHash</c> to give the gunner</param>
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
			gunner.RelationshipGroup = _rg;

			// give crew weapons in weaponArray, plus a standard issue sidearm
			giveWeapons(gunner, weaponArray);

			// task crew with fighting any enemies
			gunner.FiringPattern = fp;
			gunner.Task.FightAgainstHatedTargets(9999f);
			gunner.AlwaysKeepTask = true;
			gunner.CanRagdoll = false;
			gunner.CanWrithe = false;

			return gunner;
		}



		/// <summary>
		/// Give a specified <c>Ped</c> the specified weapons
		/// </summary>
		/// <param name="crew">Ped receiving weapons</param>
		/// <param name="weaponArray">Array of <c>WeaponHash</c> to assign</param>
		protected virtual void giveWeapons (Ped crew, WeaponHash[] weaponArray) {
			// provide the default sidearm
			crew.Weapons.Give(sidearm, 9999, true, true);
			foreach (WeaponHash weapon in weaponArray)
				crew.Weapons.Give(weapon, 9999, true, true);

			// automatically select the best weapon
			crew.Weapons.Select(crew.Weapons.BestWeapon);
		}
		#endregion



		#region virtualHelpers
		/// <summary>
		/// Based on the _model of the heli, spawn in the required personel.
		/// </summary>
		/// <returns>Array of NPCs spawned into the heli</returns>
		protected virtual Ped[] spawnCrewIntoHeli()
		{
			return new Ped[0];
		}



		/// <summary>
		/// If the _leader is close enough to the heli, and is close enough to the heli, set the _leader
		/// into the hlicopter 
		/// </summary>
		protected virtual void heliLandingHandler(VehicleSeat preferredSeat = VehicleSeat.Passenger, bool cmdRequired = true)
		{
			// check if _leader is close enough to the heli
			if (_leader.Position.DistanceTo(heli.Position) < warpIntoDistanceThreshold)
			{
				// if the "enter vehicle" command is required, but was not pressed, do nothing
				if (cmdRequired && !Game.IsControlPressed(Control.Enter))
					return;

				// set the _leader into the heli on the preferredSeat
				_leader.SetIntoVehicle(heli, preferredSeat);

				// task each Ground crew member with entering the heli with the player
				foreach (Ped p in _leader.PedGroup.ToList(false))
					p.Task.EnterVehicle(heli, VehicleSeat.Any, -1, 2f);

				// once _leader is in the vehicle, display help message
				GTA.UI.Notification.Show("Press Tab+F10 (or your custom activation key) to fly to waypoint or hover");
			}
		}
		#endregion
	}
}
