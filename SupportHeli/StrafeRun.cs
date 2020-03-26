using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;


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

		// flags
		protected bool _isActive;
		public bool isActive { get { return _isActive; } }

		// variables
		protected int _spawnTime;
		protected Vector3 _targetPos;
		protected float _lastDistance = float.PositiveInfinity;		// on each tick, measure the 2D distance to the target

		// consts
		protected const BlipColor defaultBlipColor = BlipColor.Orange;
		protected const float initialAirSpeed = 30f;
		protected const float cinematicCamFov = 85f;
		protected readonly Vector3 cinematicCameraOffset = new Vector3(25f, -25f, 3f);
		protected readonly Model strafeVehicleModel = (Model)((int)1692272545u);	// B11 Strikeforce

		// formation consts
		protected const float formationOffsetUnit = 50f;
		protected readonly Vector3[] formationOffsets = new Vector3[] {				// fingertip formation strong right
			Vector3.Zero, 
			new Vector3(-formationOffsetUnit, -formationOffsetUnit, -10f),
 			new Vector3(formationOffsetUnit, -formationOffsetUnit, -10f),
			new Vector3(-2 * formationOffsetUnit, -2 * formationOffsetUnit, -20f)
		};
		protected readonly uint[] formationWeapons = new uint[] { 955522731, 519052682, 955522731, 519052682 };

		// object references
		protected Stack<Vehicle> strafeVehicleStack = new Stack<Vehicle>();
		protected Camera cinematicCam;
		protected RelationshipGroup relGroup;
		#endregion





		#region constructor
		public StrafeRun(float radius, float height, bool cinematic = true)
		{
			_height = height;
			_radius = radius;
			_cinematic = cinematic;
			relGroup = Game.Player.Character.RelationshipGroup;
		}



		/// <summary>
		/// Destroy assets used for Strafe Run, either by force or gracefully
		/// </summary>
		/// <param name="force"></param>
		public virtual void destructor(bool force = false){
			// reset settings
			_isActive = false;
			_lastDistance = float.PositiveInfinity;

			// reset to default camera
			if (_cinematic)
			{
				World.RenderingCamera = null;
				cinematicCam.Delete();
			}

			try
			{
				foreach (Vehicle strafeVehicle in strafeVehicleStack)
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

				strafeVehicleStack.Clear();
			}
			catch { }
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

			// otherwise, spawn the strafe run
			_isActive = true;
			_spawnTime = Game.GameTime;
			_targetPos = targetPos;

			// spawn a strafing vehicle formation
			strafeVehicleStack = spawnStrafeVehiclesInFormation(targetPos, 4);

			// render from cinematic cam if requested
			if (_cinematic)
			{
				cinematicCam = initCinematicCam(strafeVehicleStack.Peek());
				World.RenderingCamera = cinematicCam;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		public void strafeRunOnTick()
		{
			// check if active
			if (!_isActive) return;

			// if active, check if timed out;
			if (Game.GameTime - _spawnTime > _timeout)
			{
				Notification.Show("Strafe run timed out; dismissing");
				destructor(false);				// dismiss gracefully
				return;
			}

			// compute the last vehicle's distance to the target
			float currDistance = strafeVehicleStack.Peek().Position.DistanceTo2D(_targetPos);
			if (currDistance > _lastDistance)
			{
				Notification.Show("Strafe run complete; dimissing");
				destructor(false);				// if the vehicle is getting further away from target, dismiss
			}
			else
			{
				_lastDistance = currDistance;
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
		protected Stack<Vehicle> spawnStrafeVehiclesInFormation(Vector3 targetPos, int N)
		{
			// impose limit on N; init empty stack of size N
			if (N > formationOffsets.Length) N = formationOffsets.Length;
			Stack<Vehicle> strafeVehicles = new Stack<Vehicle>(N);

			// compute the formation anchor's position, and initial orientation
			Vector3 formationAnchorPos = Helper.getOffsetVector3(_height, _radius) + targetPos;
			Vector3 initialEulerAngle = Helper.getEulerAngles((targetPos - formationAnchorPos).Normalized);
			initialEulerAngle.X = 0f;				// reduce initial pitch 
			initialEulerAngle.Z += 20.0f;			// offset initial yaw by 30 degrees (clockwise)

			// spawn individual strafe vehicles and push onto the stack
			for (int n = 0; n < N; n++)
			{
				Vehicle strafeVehicle = spawnStrafeVehicle(formationAnchorPos, initialEulerAngle, n);
				Ped pilot = spawnStrafePilot(strafeVehicle, n);			// spawn pilot into vehicle
				taskPilotEngage(pilot, strafeVehicle, Game.Player.Character.Position);
				strafeVehicles.Push(strafeVehicle);
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
		/// Spawn a pilot into the strafe run vehicle, and task the pilot with flying towards the target,
		/// while simultaneously shooting at the target.
		/// </summary>
		/// <param name="veh">Reference t othe strafe run vehicle</param>
		/// 
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
			Camera cam = World.CreateCamera(Vector3.Zero, Vector3.Zero, cinematicCamFov);
			cam.AttachTo(strafingVeh, cinematicCameraOffset);
			cam.PointAt(strafingVeh);
			
			//cam.InterpTo()

			return cam;
		}
		#endregion
	}
}
