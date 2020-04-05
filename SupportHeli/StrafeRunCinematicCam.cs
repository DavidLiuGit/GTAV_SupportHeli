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
		private bool _isActive;
		private Queue<StrafeRunCinematicCam> _activeSequence;
		private StrafeRunCinematicCam _activeSrcc;
		private Camera _activeCam;
		private int _activationTime;

		// active strafe run properties
		private StrafeRun.StrafeRunPropertiesSummary _activeSrps;
		#endregion




		#region constructorDestructor
		public StrafeRunCinematicCamController()
		{

		}


		public void destructor()
		{
			World.RenderingCamera = null;
			World.DestroyAllCameras();
			_activeSequence = null;
			_activeCam = null;
			_isActive = false;
		}
		#endregion




		#region publicMethods
		public void initializeCinematicCamSequence(StrafeRun.StrafeRunPropertiesSummary srps)
		{
			// randomly select a sequence from available sequences
			StrafeRunCinematicCam[] selectedSequence = _sequences[rng.Next(0, _sequences.Length)];
			_activeSequence = new Queue<SRCC>(selectedSequence);
			_isActive = true;

			// initialize an duplicated GameplayCamera, and render from it
			Camera gpCam = Helper.duplicateGameplayCam();
			World.RenderingCamera = gpCam;

			// mark the next cam in the sequence as active, and interpolate to it
			_activeSrcc = _activeSequence.Dequeue();
			_activeCam = _activeSrcc.createCamera(srps);
			_activationTime = Game.GameTime;
		}



		/// <summary>
		/// Call this method on each tick to control the active StrafeRun
		/// </summary>
		/// <returns></returns>
		public bool onTick()
		{
			try {
				StrafeRunCinematicCam nextCam = _activeSequence.Peek();
				return true;
			}
			catch
			{
				return false;
			}
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
		public CameraTransition transitionTo;
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
			transitionTo = camTransitionTo;
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
