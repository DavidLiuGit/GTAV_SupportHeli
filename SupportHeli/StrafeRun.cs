using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;

using Priority_Queue;


namespace GFPS
{
	class StrafeRun
	{
		#region properties
		// settings
		public bool _cinematic = true;
		protected float _height;
		protected float _radius;
		protected int _timeout = 20000;		// _timeout after 15 seconds
		protected float _searchRadius = 30f;
		protected int numVehicles = 3;

		// flags
		protected bool _isActive;
		public bool isActive { get { return _isActive; } }

		// variables
		protected int _spawnTime;
		protected Vector3 _targetPos;
		protected float _lastDistance = float.PositiveInfinity;		// on each tick, measure the 2D distance to the target
		protected bool _playerInvincibilityState;

		// consts
		protected const BlipColor defaultBlipColor = BlipColor.Orange;
		protected const float initialAirSpeed = 25f;
		protected const float cinematicCamFov = 30f;
		protected readonly Vector3 cinematicCameraOffset = new Vector3(1.5f, -30f, 5f);
		protected readonly Model strafeVehicleModel = (Model)((int)1692272545u);	// B11 Strikeforce
		protected const int vehiclesPerInitialTarget = 3;							// # vehs = # targets / vehiclesPerInitialTarget
		protected readonly Vector3 spawnPositionEvaluationOffset = new Vector3(0f, 0f, -65f);

		// formation consts
		protected const float formationOffsetUnit = 50f;
		protected readonly Vector3[] formationOffsets = new Vector3[] {				// Vic formation
			Vector3.Zero, 
			new Vector3(-formationOffsetUnit, -formationOffsetUnit, -10f),
 			new Vector3(formationOffsetUnit, -formationOffsetUnit, -10f),
			new Vector3(-2 * formationOffsetUnit, -2 * formationOffsetUnit, -20f),
			new Vector3(2 * formationOffsetUnit, -2 * formationOffsetUnit, -20f)
		};
		protected readonly uint[] formationWeapons = new uint[] { 955522731, 519052682, 955522731, 519052682, 955522731 };

		// object references
		protected List<Vehicle> strafeVehiclesList = new List<Vehicle>();
		protected Camera cinematicCam;
		protected RelationshipGroup relGroup;
		protected List<Ped> initialTargetList;
		protected ParticleEffect targetMarkerPtfx;
		protected ParticleEffectAsset targetMarkerPtfxAsset = new ParticleEffectAsset("core");
		#endregion





		#region constructorDestructor
		public StrafeRun(float radius, float height, float targetRadius, bool cinematic = true)
		{
			// settings
			_height = height;
			_radius = radius;
			_cinematic = cinematic;
			_searchRadius = targetRadius;

			// other preparations
			relGroup = Game.Player.Character.RelationshipGroup;
			targetMarkerPtfxAsset.Request();
		}



		/// <summary>
		/// Destroy assets used for Strafe Run, either by force or gracefully
		/// </summary>
		/// <param name="force"></param>
		public virtual void destructor(bool force = false){
			// if the strafe run is not currently active, and the force flag is false, then do nothing
			if (!_isActive && !force)
				return;

			try
			{
				foreach (Vehicle strafeVehicle in strafeVehiclesList)
				{
					strafeVehicle.AttachedBlip.Delete();

					// if destroying by force
					if (force)
					{
						strafeVehicle.Driver.Delete();
						strafeVehicle.Delete();
					}

					// if destroying gracefully, command the pilot to fly away. Mark crew and vehicle as no longer needed
					else
					{
						strafeVehicle.Driver.Task.FleeFrom(_targetPos);
						strafeVehicle.Driver.MarkAsNoLongerNeeded();
						strafeVehicle.MarkAsNoLongerNeeded();
					}
				}

				strafeVehiclesList.Clear();

				// free each ped in the initial targets list
				foreach (Ped p in initialTargetList)
					p.MarkAsNoLongerNeeded();
			}
			catch { }

			// free PTFX assets from memory, and remove end any particle FX
			try
			{
				if (force) targetMarkerPtfxAsset.MarkAsNoLongerNeeded();
				targetMarkerPtfx.Delete();
			}
			catch { Screen.ShowHelpTextThisFrame("Error trying to delete targetMarker"); }

			// try to reset cinematic camera to default game cam
			try
			{
				if (_cinematic)
				{
					Game.Player.Character.IsInvincible = _playerInvincibilityState;
					World.RenderingCamera = null;
					cinematicCam.Delete();
				}
			}
			catch { Screen.ShowHelpTextThisFrame("Error trying to reset camera"); }

			// reset settings
			_isActive = false;
			_lastDistance = float.PositiveInfinity;
		}
		#endregion





		#region publicMethods
		/// <summary>
		/// Begin strafe run.
		/// </summary>
		public void spawnStrafeRun(Vector3 targetPos)
		{
			// do nothing if a strafe run is already active
			if (_isActive) return;

			// sanity check the target position
			if (targetPos == Vector3.Zero) return;

			// otherwise, spawn the strafe run
			_isActive = true;
			_spawnTime = Game.GameTime;
			_targetPos = targetPos;

			// acquire initial targets
			SimplePriorityQueue<Ped> targetQ = buildTargetPriorityQueue(_targetPos, _searchRadius, true);
			initialTargetList = targetQ.ToList();

			// spawn a strafing vehicle formation
			strafeVehiclesList = spawnStrafeVehiclesInFormation(targetPos, (targetQ.Count+2) / vehiclesPerInitialTarget);
			taskAllPilotsEngage(targetQ, true);		// if no targets, fire at targetPos

			// mark the target position with flare ptfx
			targetMarkerPtfx = World.CreateParticleEffect(targetMarkerPtfxAsset, "exp_grd_flare", targetPos);

			// render from cinematic cam if requested
			if (_cinematic)
			{
				cinematicCam = initCinematicCam(strafeVehiclesList[strafeVehiclesList.Count - 1]);
				World.RenderingCamera = cinematicCam;
			}
		}



		/// <summary>
		/// Invoke periodically. Handles 
		/// </summary>
		public void strafeRunOnTick()
		{
			// check if active
			if (!_isActive) return;

			// if active, check if timed out;
			if (Game.GameTime - _spawnTime > _timeout)
			{
				//Notification.Show("Strafe run timed out; dismissing");
				destructor(false);				// dismiss gracefully
				return;
			}

			// compute the last vehicle's distance to the target
			float currDistance = strafeVehiclesList[strafeVehiclesList.Count - 1].Position.DistanceTo2D(_targetPos);
			if (currDistance > _lastDistance)
			{
				Notification.Show("Strafe run complete. " + getKillCount(initialTargetList) + 
					" of " + initialTargetList.Count + " initial targets KIA.");
				destructor(false);				// if the vehicle is getting further away from target, dismiss
			}

			// otherwise, task the pilots to engage targets in the priority queue
			else
			{
				// update distance
				_lastDistance = currDistance;

				// assign pilots to targets in the target priority queue
				SimplePriorityQueue<Ped> targetQ = buildTargetPriorityQueue(_targetPos, _searchRadius);
				taskAllPilotsEngage(targetQ);
			}
		}
		#endregion




		#region helperMethods
		/// <summary>
		/// Spawn a strafing formation comprised of <c>N</c> vehicles.
		/// </summary>
		/// <param name="targetPos">Current target position</param>
		/// <param name="N">Number of vehicles in the formation</param>
		/// <returns>Collection of strafing vehicles in the formation</returns>
		protected List<Vehicle> spawnStrafeVehiclesInFormation(Vector3 targetPos, int N)
		{
			// impose lower & upper limits on N. 1 < N < # vehicles in formation definition
			if (N > formationOffsets.Length) N = formationOffsets.Length;
			else if (N < 1) N = 1;
			List<Vehicle> strafeVehicles = new List<Vehicle>(N);

			// compute the formation anchor's position, and initial orientation
			Vector3 formationAnchorPos = getValidSpawnPosition(targetPos, 10);//Helper.getOffsetVector3(_height, _radius) + targetPos;
			Vector3 initialEulerAngle = Helper.getEulerAngles((targetPos - formationAnchorPos).Normalized);
			initialEulerAngle.X = 0f;				// reduce initial pitch 
			initialEulerAngle.Z += 15.0f;			// offset initial yaw by 30 degrees (clockwise)

			// spawn individual strafe vehicles and push onto the stack
			for (int n = 0; n < N; n++)
			{
				Vehicle strafeVehicle = spawnStrafeVehicle(formationAnchorPos, initialEulerAngle, n);
				Ped pilot = spawnStrafePilot(strafeVehicle, n);			// spawn pilot into vehicle
				strafeVehicles.Add(strafeVehicle);
			}

			return strafeVehicles;
		}



		/// <summary>
		/// Spawn a single strafing vehicle, as part of a strafing formation.
		/// </summary>
		/// <param name="formationAnchorPos">The position of the formation's anchor</param>
		/// <param name="spawnRotation">The initial rotation of the vehicle</param>
		/// <param name="n">The nth vehicle in the strafing formation</param>
		/// <returns>Returns an instance of <c>Vehicle</c></returns>
		protected Vehicle spawnStrafeVehicle(Vector3 formationAnchorPos, Vector3 spawnRotation, int n)
		{
			// compute the position to spawn the vehicle at
			Vector3 spawnPos = formationAnchorPos + Helper.rotateVectorZAxis(formationOffsets[n], spawnRotation.Z);

			// spawn strafe run vehicle
			Vehicle veh = World.CreateVehicle(strafeVehicleModel, spawnPos);
			
			// orient the vehicle towards the target
			veh.Rotation = spawnRotation;
			veh.ForwardSpeed = initialAirSpeed;

			// apply settings the the vehicle
			veh.IsEngineRunning = true;
			veh.LandingGearState = VehicleLandingGearState.Retracted;

			// attach a blip to the vehicle
			veh.AddBlip();
			veh.AttachedBlip.Sprite = BlipSprite.B11StrikeForce;
			veh.AttachedBlip.Color = defaultBlipColor;

			return veh;
		}



		/// <summary>
		/// Build a queue of potential targets to engage from peds found within a World search. Highest
		/// priority targets are at the beginning of the queue.
		/// </summary>
		/// <param name="searchOrigin">center (origin) of Ped search</param>
		/// <param name="searchRadius">radius of Ped search</param>
		/// <param name="targetNeutral">Whether not to target Peds with neutral relationship</param>
		/// <returns></returns>
		protected SimplePriorityQueue<Ped> buildTargetPriorityQueue(Vector3 searchOrigin, float searchRadius, bool persistTarget = false)
		{
			// init empty list
			SimplePriorityQueue<Ped> targetQ = new SimplePriorityQueue<Ped>();

			// get all Peds near the search origin
			Ped[] nearbyPeds = World.GetNearbyPeds(searchOrigin, searchRadius);

			// add Peds to the queue based on their relationship with the player
			Ped player = Game.Player.Character;
			foreach (Ped ped in nearbyPeds)
			{
				// if ped is not alive, do not add to queue
				if (!ped.IsAlive) continue;

				// check if the ped is the player; do not add to queue if so, and warn the player of danger
				else if (player == ped)
				{
					if (!_cinematic) Screen.ShowHelpTextThisFrame("Warning: you are in the air strike splash zone!");
					continue;
				}

				// determine the Ped's relationship with the player
				Relationship rel = ped.GetRelationshipWithPed(player);

				// if the relationship is positive, do not add ped to target queue
				if ((int)rel <= 2) continue;

				// if relationship is Neutral or Pedestrian, add to queue with rank = 3
				else if (rel == Relationship.Neutral || rel == Relationship.Pedestrians)
				{
					targetQ.Enqueue(ped, 3);
					if (persistTarget) ped.IsPersistent = true;
				}

				// otherwise, the relationship is negative (dislike/hate); add to queue
				else
				{
					targetQ.Enqueue(ped, 5 - (int)rel);
					if (persistTarget) ped.IsPersistent = true;
				}
			}

			return targetQ;
		}



		/// <summary>
		/// Spawn a pilot into the strafe run vehicle, and task the pilot with flying towards the target,
		/// while simultaneously shooting at the target.
		/// </summary>
		/// <param name="veh">Reference t othe strafe run vehicle</param>
		/// <param name="n">The nth pilot in the strafing formation</param>
		/// <returns></returns>
		protected Ped spawnStrafePilot(Vehicle veh, int n)
		{
			// spawn the pilot in the driver seat
			Ped p = veh.CreatePedOnSeat(VehicleSeat.Driver, PedHash.Pilot01SMY);
			p.RelationshipGroup = relGroup;

			// configure weapons
			Function.Call(Hash.SET_CURRENT_PED_VEHICLE_WEAPON, p, formationWeapons[n]);
			p.FiringPattern = FiringPattern.FullAuto;

			return p;
		}



		/// <summary>
		/// Task all strafe run pilots with engaging Ped targets. If there are more strafing vehicles
		/// than targets, the remaining pilots will fire at the original target position.
		/// </summary>
		/// <param name="targetQ"><c>SimplePriorityQueue</c> of Peds, from which targets will be Dequeued</param>
		/// <param name="fireAtPosition">if true, jets with no Ped to engage will instead shoot at targetPos</param>
		protected void taskAllPilotsEngage(SimplePriorityQueue<Ped> targetQ, bool fireAtPosition = false)
		{
			for (int i = 0; i < strafeVehiclesList.Count; i++)
			{
				Vehicle veh = strafeVehiclesList[i];

				if (i < targetQ.Count)
					taskPilotEngage(veh.Driver, veh, targetQ.Dequeue());

				else if (fireAtPosition)
					taskPilotEngage(veh.Driver, veh, _targetPos);
			}
		}



		/// <summary>
		/// Task the pilot of a specified vehicle to engage a specified target <c>Ped</c>
		/// </summary>
		/// <param name="pilot">Pilot <c>Ped</c></param>
		/// <param name="veh"><c>Vehicle</c> pilot is flying</param>
		/// <param name="target">Target <c>Ped</c></param>
		protected void taskPilotEngage(Ped pilot, Vehicle veh, Ped target)
		{
			// task the pilot with shooting
			Function.Call(Hash.TASK_PLANE_MISSION, pilot, veh, 0, target, 0f, 0f, 0f,
				6, 0f, 0f, (target.Position - veh.Position).ToHeading(), 2500f, 5f);
			pilot.AlwaysKeepTask = true;
		}



		/// <summary>
		/// Task the pilot of a specified vehicle to fire at a specified position.
		/// </summary>
		/// <param name="pilot">Pilot <c>Ped</c></param>
		/// <param name="veh"><c>Vehicle</c> pilot is flying</param>
		/// <param name="target">Target position <c>Vector3</c></param>
		protected void taskPilotEngage(Ped pilot, Vehicle veh, Vector3 target)
		{
			// task the pilot with shooting
			Function.Call(Hash.TASK_PLANE_MISSION, pilot, veh, 0, 0, target.X, target.Y, target.Z,
				6, 0f, 0f, (target - veh.Position).ToHeading(), 2500f, 5f);
			pilot.AlwaysKeepTask = true;
		}



		/// <summary>
		/// Initialize the cinematic camera for the strafe run.
		/// </summary>
		/// <param name="strafingVeh">Camera will be attached to this strafing vehicle</param>
		/// <returns></returns>
		protected Camera initCinematicCam(Vehicle strafingVeh)
		{
			// store whether the player is invincible before activation of cinematic cam; then make player invincible
			_playerInvincibilityState = Game.Player.IsInvincible;		// restored when the cinematic cam is destroyed
			Game.Player.IsInvincible = true;

			// create and manipulate the cinematic cam
			Camera cam = World.CreateCamera(Vector3.Zero, Vector3.Zero, cinematicCamFov);
			cam.AttachTo(strafingVeh, cinematicCameraOffset);
			cam.PointAt(_targetPos);
			
			//cam.InterpTo()

			return cam;
		}



		/// <summary>
		/// Determine how many Peds in a List are deceased.
		/// </summary>
		/// <param name="initialTargets"><c>List</c> of Peds</param>
		/// <returns>number of Peds in the provided list that are dead</returns>
		protected int getKillCount(List<Ped> initialTargets)
		{
			int count = 0;

			foreach (Ped p in initialTargets)
				if (p.IsDead) count++;
			
			return count;
		}



		/// <summary>
		/// Get a suitable spawn position for the strafe run, based on Raycast results. A spawn
		/// position is considered suitable if it has a clear LOS to the target position.
		/// </summary>
		/// <param name="targetPos">Target position</param>
		/// <param name="maxTrials">Max tries before giving up and returning a random position</param>
		/// <returns></returns>
		protected Vector3 getValidSpawnPosition(Vector3 targetPos, int maxTrials = 5)
		{
			Vector3 trialPosition;
			for (int i = 0; i < maxTrials; i++)
			{
				trialPosition = Helper.getOffsetVector3(_height, _radius) + targetPos;

				// if a raycast from the trialPosition to the target position is good, return this position
				if (Helper.evaluateRaycast(trialPosition + spawnPositionEvaluationOffset, targetPos))
				{
					//Notification.Show("Found valid position after " + i + " tries.");
					return trialPosition;
				}
			}

			//Notification.Show("No valid positions found after " + maxTrials + " tries.");
			return Helper.getOffsetVector3(_height, _radius) + targetPos;
		}
		#endregion
	}
}
