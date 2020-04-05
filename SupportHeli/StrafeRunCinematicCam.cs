using System;
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
		private Random rng = new Random();

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
				Game.Player.IsInvincible = _initialInvincibilityState;
		}
		#endregion




		#region publicMethods
		public void initializeCinematicCamSequence(StrafeRun.StrafeRunPropertiesSummary srps)
		{
			_activeSrps = srps;

			// randomly select a sequence from available sequences
			StrafeRunCinematicCam[] selectedSequence = _sequences[rng.Next(0, _sequences.Length)];
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
				_initialInvincibilityState = Game.Player.IsInvincible;
				Game.Player.IsInvincible = true;
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
			// seq0: follow strafe vehicle to the end
			new StrafeRunCinematicCam[] {
				new SRCC(SRCC_CCT.followStrafeVehicle, new SRCC_CT(int.MaxValue, 1250, 25, 25)),
			},

			// seq1: look at strike target from player's perspective
			new StrafeRunCinematicCam[] {
				new SRCC(SRCC_CCT.playerLookAtTarget, new SRCC_CT(int.MaxValue, 500, 25, 25)),
			}
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
			catch (InvalidOperationException e)
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
			followStrafeVehicle,
			playerLookAtTarget,
			playerLookAtStrafeVehicle,
			targetLookAtStrafeVehicle
		}
		public cinematicCamType _type;
		#endregion



		/// <summary>
		/// Create 
		/// </summary>
		/// <param name="camTransitionTo"></param>
		public StrafeRunCinematicCam(cinematicCamType type, CameraTransition camTransitionTo)
		{
			// store settings
			_type = type;
			transition = camTransitionTo;
		}



		#region camConstructors

		public Camera createCamera(StrafeRun.StrafeRunPropertiesSummary srps)
		{
			switch (_type)
			{
				default:
				case cinematicCamType.followStrafeVehicle:
					return createFollowStrafeVehicleCam(srps.vehicles[0], srps.targetPos);
			}
		}



		private Camera createFollowStrafeVehicleCam(Vehicle veh, Vector3 targetPos)
		{
			Camera cam = World.CreateCamera(Vector3.Zero, Vector3.Zero, 30f);
			cam.AttachTo(veh, new Vector3(1.5f, -30f, 5f));
			cam.PointAt(targetPos);
			return cam;
		}
		#endregion

	}
}
