﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTA;
using GTA.Math;

// type alias shortcuts
using SRCC = GFPS.StrafeRunCinematicCam;
using SRCC_CCT = GFPS.StrafeRunCinematicCam.cinematicCamType;
using SRCC_CT = GFPS.StrafeRunCinematicCam.CameraTransition;



namespace GFPS
{
	public class StrafeRunCinematicCamController
	{
		#region properties
		public static Random rng = new Random();

		// camera properties
		private bool _isActive = false;
		private Queue<StrafeRunCinematicCam> _activeSequence;
		private StrafeRunCinematicCam _activeSrcc;
		private Camera _activeCam;
		private int _activationTime = int.MaxValue;

		// active strafe run properties
		private StrafeRun.StrafeRunPropertiesSummary _activeSrps;

		// player settings
		private bool _invincibleWhileActive;
		private bool _initialInvincibilityState;
		private Vehicle _initialPlayerVeh = null;
		private bool _initialPlayerVehInvincibilityState;
		#endregion




		#region constructorDestructor
		public StrafeRunCinematicCamController(bool invincible = true)
		{
			_invincibleWhileActive = invincible;
		}


		public void destructor()
		{
			World.RenderingCamera = null;
			World.DestroyAllCameras();
			_activeSequence = null;
			_activeCam = null;
			_isActive = false;
			_activationTime = int.MaxValue;

			// reset player invincibility state
			if (_invincibleWhileActive)
			{
				Game.Player.IsInvincible = _initialInvincibilityState;

				// if player was in a vehicle and it still exists, restore its invincibility state
				if (_initialPlayerVeh != null && _initialPlayerVeh.Exists())
					_initialPlayerVeh.IsInvincible = _initialPlayerVehInvincibilityState;
				_initialPlayerVeh = null;
			}
		}
		#endregion




		#region publicMethods
		public void initializeCinematicCamSequence(StrafeRun.StrafeRunPropertiesSummary srps)
		{
			_activeSrps = srps;

			// randomly select a sequence from available sequences
			int randIdx = rng.Next(0, _sequences.Length);
			StrafeRunCinematicCam[] selectedSequence = _sequences[randIdx];
			_activeSequence = new Queue<SRCC>(selectedSequence);

			// initialize an duplicated GameplayCamera, and render from it
			Camera gpCam = Helper.duplicateGameplayCam();
			World.RenderingCamera = gpCam;
			_activeCam = gpCam;

			// activate the 1st cam in selectedSequence and interp to it
			activateAndInterpToNextCam();

			// handle player invincibility
			if (_invincibleWhileActive)
			{
				// remember player's current invincibility state, then set player to invincible
				_initialInvincibilityState = Game.Player.IsInvincible;
				Game.Player.IsInvincible = true;

				// if player is in vehicle, remember its invincibility state, then set vehicle to 
				if (Game.Player.Character.IsInVehicle())
				{
					_initialPlayerVeh = Game.Player.Character.CurrentVehicle;
					_initialPlayerVehInvincibilityState = _initialPlayerVeh.IsInvincible;
					_initialPlayerVeh.IsInvincible = true;
				}

			}
		}



		/// <summary>
		/// Call this method on each tick to control the active StrafeRun
		/// </summary>
		/// <returns>Whether any StrafeRunCinematicCam is still active</returns>
		public bool onTick()
		{
			// if not currently active, return false
			if (!_isActive) return false;

			// check if it's time to activate the next cam in the sequence
			if (Game.GameTime - _activationTime > _activeSrcc.transition._camDuration)
			{
				activateAndInterpToNextCam();
			}

			return _isActive;
		}
		#endregion



		#region sequences
		private StrafeRunCinematicCam[][] _sequences = {
			// strafeVeh fly-by, then strafeVeh follow
			new StrafeRunCinematicCam[] {
				new SRCC(SRCC_CCT.StrafeVehFlyBy, new SRCC_CT(2000, 1000, 25, 25)),
				new SRCC(SRCC_CCT.FollowStrafeVeh, new SRCC_CT(int.MaxValue, 1000, 25, 25)),
			},

			// look at strike target from player's perspective
			new StrafeRunCinematicCam[] {
				new SRCC(SRCC_CCT.PlayerLookAtStrafeVeh, new SRCC_CT(2200, 500, 25, 25)),
				new SRCC(SRCC_CCT.PlayerLookAtTarget, new SRCC_CT(701, 700, 25, 25)),
				new SRCC(SRCC_CCT.PlayerLookAtTargetZoomed, new SRCC_CT(int.MaxValue, 5000, 25, 25)),
			},

			// strafeVeh fly-by; birds eye random cams
			new StrafeRunCinematicCam[] {
				new SRCC(SRCC_CCT.StrafeVehFlyBy, new SRCC_CT(2000, 1000, 25, 25)),
				new SRCC(SRCC_CCT.BirdsEyeTargetRandomAngle, new SRCC_CT(2501, 2500, 25, 25)),
				new SRCC(SRCC_CCT.BirdsEyeTargetRandomAngle, new SRCC_CT(int.MaxValue, 10000, 10, 10))
			},

			// player look at strafeVeh, then follow strafeVeh
			new StrafeRunCinematicCam[] {
				new SRCC(SRCC_CCT.PlayerLookAtStrafeVeh, new SRCC_CT(2000, 500, 25, 25)),
				new SRCC(SRCC_CCT.FollowStrafeVeh, new SRCC_CT(int.MaxValue, 1250, 25, 25)),
			},

			// strafeVeh fly-by, 1st person cockpit, then player look at target
			new StrafeRunCinematicCam[] {
				new SRCC(SRCC_CCT.StrafeVehFlyBy, new SRCC_CT(2000, 1000, 25, 25)),
				new SRCC(SRCC_CCT.StrafeVehFirstPersonLookAtPos, new SRCC_CT(5000, 1000, 25, 25)),
				new SRCC(SRCC_CCT.PlayerLookAtTarget, new SRCC_CT(701, 700, 25, 25)),
				new SRCC(SRCC_CCT.PlayerLookAtTargetZoomed, new SRCC_CT(int.MaxValue, 1500, 25, 25)),
			},

			// strafeVeh 45, then strafeVeh follow
			new StrafeRunCinematicCam[] {
				new SRCC(SRCC_CCT.StrafeVeh45offset, new SRCC_CT(1000, 1000, 25, 25)),
				new SRCC(SRCC_CCT.StrafeVehFirstPersonLookAtPos, new SRCC_CT(5000, 1000, 25, 25)),
				new SRCC(SRCC_CCT.FollowStrafeVeh, new SRCC_CT(int.MaxValue, 2500, 25, 25)),
			},

			// follow strafeVeh, then birds eye along veh LOS
			new StrafeRunCinematicCam[] {
				new SRCC(SRCC_CCT.FollowStrafeVeh, new SRCC_CT(1600, 1000, 25, 25)),
				new SRCC(SRCC_CCT.BirdsEyeTargetStrafeVehAngle, new SRCC_CT(2801, 3300, 1, 1)),
				new SRCC(SRCC_CCT.BirdsEyeTargetRandomAngle, new SRCC_CT(int.MaxValue, 7000, 1, 1))
			},
		};
		#endregion




		#region helperMethods
		/// <summary>
		/// Activate the next StrafeRunCinematicCam in _activeSequence queue, and interpolate from
		/// the currently active camera to the next camera.
		/// </summary>
		private void activateAndInterpToNextCam(){
			try {
				// activate the next camera in the queue
				_activeSrcc = _activeSequence.Dequeue();
				Camera nextCam = _activeSrcc.createCamera(_activeSrps);

				// interpolate from the currently active cam to nextCam
				_activeCam.InterpTo(nextCam, _activeSrcc.transition._transDuration,
					_activeSrcc.transition._easePosition, _activeSrcc.transition._easeRotation);

				// update properties
				_isActive = true;
				_activationTime = Game.GameTime;
				
				// destroy the previously active camera, and update _activeCam to nextCam
				_activeCam = nextCam;
			}
			catch (InvalidOperationException)
			{
				// if any exception are throw during the activation process, hard destroy
				GTA.UI.Notification.Show("~r~Error: No more cameras in the sequence.");
				_activationTime = int.MaxValue;		// make sure this method is not triggered by timing logic again
			}
			catch (Exception e)
			{
				GTA.UI.Notification.Show("~r~Error: something went wrong while activating next cam. Aborting.");
				GTA.UI.Notification.Show(e.Message);
				destructor();
			}

		}
		#endregion
	}





	public struct StrafeRunCinematicCam
	{
		#region transition
		public struct CameraTransition
		{
			// how long to render from this cam, before transitioning to the next cam
			public int _camDuration;		// use int.Maxvalue for the last cam

			// transition from the PREVIOUS cam to this cam
			public int _transDuration;
			public int _easePosition;
			public int _easeRotation;

			/// <summary>
			/// Define properties of the transition to and from this CineCam
			/// </summary>
			/// <param name="duration">how long to render from THIS cam, before transitioning to the next cam, in milliseconds</param>
			/// <param name="transDuration">interpolation duration from the PREVIOUS cam to THIS cam</param>
			/// <param name="easePos">interpolation position easing from the PREVIOUS cam to THIS cam</param>
			/// <param name="easeRot">interpolation rotation easing from the PREVIOUS cam to THIS cam</param>
			public CameraTransition (int duration, int transDuration, int easePos, int easeRot){
				_camDuration = duration;
				_transDuration = transDuration;
				_easePosition = easePos;
				_easeRotation = easeRot;
			}
		}
		public CameraTransition transition;
		#endregion


		#region cinematicCamType
		public enum cinematicCamType {
			FollowStrafeVeh,
			PlayerLookAtTarget,
			PlayerLookAtTargetZoomed,
			PlayerLookAtStrafeVeh,
			TargetLookAtStrafeVeh,
			StrafeVehFlyBy,
			StrafeVehFirstPersonLookAtPos,
			StrafeVeh45offset,			// look at strafe veh from 45 degrees CW offset
			BirdsEyeTargetRandomAngle,
			BirdsEyeTargetStrafeVehAngle,
		}
		public cinematicCamType _type;
		#endregion



		/// <summary>
		/// Create an instance of <c>StrafRunCinematicCam</c> - a wrapper for the in-game <c>Camera</c>.
		/// Params given to this constructor are used as instructions when creating and using the <c>Camera</c>
		/// in game.
		/// </summary>
		/// <param name="camTransitionTo">Instance of <c>CameraTransition</c> struct</param>
		public StrafeRunCinematicCam(cinematicCamType type, CameraTransition camTransitionTo)
		{
			// store settings
			_type = type;
			transition = camTransitionTo;
		}



		#region camConstructors
		/// <summary>
		/// Create and return a camera created based on Strafe Run properties and predefined settings.
		/// </summary>
		/// <param name="srps">Instance of <c>StrafeRun.StrafeRunPropertiesSummary</c></param>
		/// <returns><c>Camera</c> that can be activated</returns>
		public Camera createCamera(StrafeRun.StrafeRunPropertiesSummary srps)
		{
			int randVehIdx = StrafeRunCinematicCamController.rng.Next(0, srps.vehicles.Count);

			switch (_type)
			{
				case cinematicCamType.BirdsEyeTargetStrafeVehAngle:
					return createBirdsEyeTargetStrafeVehAngleCam(srps.targetPos, srps.vehicles[randVehIdx]);

				case cinematicCamType.BirdsEyeTargetRandomAngle:
					return createBirdsEyeTargetCam(srps.targetPos);

				case cinematicCamType.StrafeVeh45offset:
					return createStrafeVeh45offsetCam(srps.vehicles[randVehIdx]);

				case cinematicCamType.StrafeVehFirstPersonLookAtPos:
					return createStrafeVehFirstPersonCam(srps.vehicles[randVehIdx], srps.targetPos);

				case cinematicCamType.PlayerLookAtStrafeVeh:
					return createPlayerLookAtStrafeVehCam(srps.vehicles[randVehIdx]);

				case cinematicCamType.TargetLookAtStrafeVeh:
					return createTargetLookAtStrafeVehCam(srps.targetPos, srps.vehicles[randVehIdx]);

				case cinematicCamType.StrafeVehFlyBy:
					return createStrafeVehFlyByCam(srps.vehicles[randVehIdx]);

				case cinematicCamType.PlayerLookAtTarget:
					return createPlayerLookAtTargetCam(srps.targetPos);

				case cinematicCamType.PlayerLookAtTargetZoomed:
					return createPlayerLookAtTargetCam(srps.targetPos, 20f);

				default:
				case cinematicCamType.FollowStrafeVeh:
					return createFollowStrafeVehicleCam(srps.vehicles[randVehIdx], srps.targetPos);
			}
		}


		private Camera createBirdsEyeTargetStrafeVehAngleCam(Vector3 targetPos, Vehicle veh, float mult = 60f, float fov = 65f)
		{
			Vector3 offsetVector = (veh.Position - targetPos).Normalized * mult;	// offset from targetPos
			Camera cam = World.CreateCamera(targetPos + offsetVector, Vector3.Zero, fov);
			cam.PointAt(targetPos);
			return cam;
		}


		private Camera createBirdsEyeTargetCam(Vector3 targetPos, float radius = 50f, float height = 30f, float fov = 65f)
		{
			Vector3 camPos = targetPos.Around(radius);
			camPos.Z += height;
			Camera cam = World.CreateCamera(camPos, Vector3.Zero, fov);
			cam.PointAt(targetPos);
			return cam;
		}


		private Camera createStrafeVeh45offsetCam(Vehicle veh)
		{
			Camera cam = World.CreateCamera(Vector3.Zero, Vector3.Zero, 40f);
			cam.AttachTo(veh.Driver, new Vector3(20f, 20f, 0f));
			cam.PointAt(veh);
			return cam;
		}

		private Camera createStrafeVehFirstPersonCam(Vehicle veh, Vector3 targetPos)
		{
			Camera cam = World.CreateCamera(Vector3.Zero, Vector3.Zero, 60f);
			cam.AttachTo(veh.Driver, new Vector3(0f, 0.15f, 0.75f));
			cam.PointAt(targetPos);
			return cam;
		}

		private Camera createPlayerLookAtStrafeVehCam(Vehicle veh)
		{
			Camera cam = Helper.duplicateGameplayCam();
			cam.FieldOfView = 20f;
			cam.PointAt(veh);
			return cam;
		}

		private Camera createTargetLookAtStrafeVehCam(Vector3 targetPos, Vehicle veh)
		{
			Camera cam = World.CreateCamera(targetPos, Vector3.Zero, 70f);
			cam.PointAt(veh);
			return cam;
		}

		private Camera createStrafeVehFlyByCam(Vehicle veh)
		{
			Vector3 camPos = Helper.getVector3NearTarget(30f, veh.Position + veh.ForwardVector * 100);
			Camera cam = World.CreateCamera(camPos, Vector3.Zero, 45f);
			cam.PointAt(veh);
			return cam;
		}

		private Camera createFollowStrafeVehicleCam(Vehicle veh, Vector3 targetPos)
		{
			Camera cam = World.CreateCamera(Vector3.Zero, Vector3.Zero, 30f);
			cam.AttachTo(veh, new Vector3(1.5f, -30f, 5f));
			cam.PointAt(targetPos);
			return cam;
		}

		private Camera createPlayerLookAtTargetCam(Vector3 targetPos, float fov = 65f)
		{
			Camera cam = Helper.duplicateGameplayCam();
			cam.FieldOfView = fov;
			cam.PointAt(targetPos);
			return cam;
		}
		#endregion

	}
}
