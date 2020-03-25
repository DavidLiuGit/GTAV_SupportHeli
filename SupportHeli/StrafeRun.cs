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
	class StrafeRun
	{
		#region properties
		// settings
		public bool _cinematic = true;
		protected float _height;
		protected float _radius;
		protected uint modelHash;

		// flags
		protected bool _isActive;
		public bool isActive { get { return _isActive; } }

		// consts
		protected const BlipColor defaultBlipColor = BlipColor.Green;

		// object references
		protected Vehicle strafeVehicle;
		protected Ped pilot;
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

			try
			{
				strafeVehicle.AttachedBlip.Delete();

				// if destroying by force
				if (force)
				{
					pilot.Delete();
					strafeVehicle.Delete();
				}

				// if destroying gracefully, command the pilot to fly away. Mark crew and vehicle as no longer needed
				else
				{
					pilot.Task.FleeFrom(Game.Player.Character);
					pilot.MarkAsNoLongerNeeded();
					strafeVehicle.MarkAsNoLongerNeeded();
				}
			}
			catch { }
		}
		#endregion





		#region publicMethods
		/// <summary>
		/// Begin strafe run
		/// </summary>
		public void spawnStrafeRun(Vector3 targetPos)
		{
			_isActive = true;

			// spawn a strafing vehicle
			strafeVehicle = spawnStrafeVehicle(targetPos);
			pilot = spawnStrafePilot(strafeVehicle, targetPos);
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
			// spawn strafe run vehicle and apply settings
			Vehicle veh = World.CreateVehicle((Model)((int)1692272545u), Helper.getOffsetVector3(_height, _radius) + targetPos);
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

			// task the pilot with shooting
			p.Task.SwapWeapon();
			Function.Call(Hash.TASK_PLANE_MISSION, p, veh, 0, Game.Player.Character, 0f, 0f, 0f,
				6, 0f, 0f, (targetPos - veh.Position).ToHeading(), 2500f, 0f);
			p.AlwaysKeepTask = true;

			return p;
		}

		#endregion
	}
}
