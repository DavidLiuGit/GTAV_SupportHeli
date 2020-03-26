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

		// object references
		protected Stack<Vehicle> strafeVehicleStack = new Stack<Vehicle>();
		#endregion





		#region constructor
		public StrafeRun(float height, float radius, bool cinematic = true)
		{
			_height = height;
			_radius = radius;
			_cinematic = cinematic;
		}



		/// <summary>
		/// Destroy assets used for Strafe Run, either by force or gracefully
		/// </summary>
		/// <param name="force"></param>
		public virtual void destructor(bool force = false){
			_isActive = false;
			_lastDistance = float.PositiveInfinity;

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
						strafeVehicle.Driver.Task.FleeFrom(Game.Player.Character);
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
			_isActive = true;
			_spawnTime = Game.GameTime;
			_targetPos = targetPos;

			// spawn a strafing vehicle
			Vehicle strafeVehicle = spawnStrafeVehicle(targetPos);
			spawnStrafePilot(strafeVehicle, targetPos);

			// add the vehicle to the list (stack) of active strafing vehicles
			strafeVehicleStack.Push(strafeVehicle);
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
		/// Spawn the vehicle to be used for the strafe run
		/// </summary>
		/// <param name="targetPos"></param>
		/// <returns>Reference to strafe run vehicle spawned</returns>
		protected Vehicle spawnStrafeVehicle(Vector3 targetPos)
		{
			// spawn strafe run vehicle
			Vehicle veh = World.CreateVehicle((Model)((int)1692272545u), Helper.getOffsetVector3(_height, _radius) + targetPos);
			
			// orient the vehicle towards the target
			veh.Rotation = Helper.getEulerAngles((targetPos - veh.Position).Normalized);

			// apply settings the the vehicle
			veh.IsEngineRunning = true;
			veh.IsBulletProof = true;
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
		/// <returns></returns>
		protected Ped spawnStrafePilot(Vehicle veh, Vector3 targetPos)
		{
			// spawn the pilot in the driver seat
			Ped p = veh.CreatePedOnSeat(VehicleSeat.Driver, PedHash.Pilot01SMY);

			// configure weapons
			Function.Call(Hash.SET_CURRENT_PED_VEHICLE_WEAPON, p, 955522731);		// cannon
			//Function.Call(Hash.SET_CURRENT_PED_VEHICLE_WEAPON, p, 519052682);		// homing
			p.FiringPattern = FiringPattern.FullAuto;
			

			// task the pilot with shooting
			Function.Call(Hash.TASK_PLANE_MISSION, p, veh, 0, Game.Player.Character, 0f, 0f, 0f,
				6, 0f, 0f, (targetPos - veh.Position).ToHeading(), 2500f, 0f);
			p.AlwaysKeepTask = true;

			return p;
		}
		#endregion
	}
}
