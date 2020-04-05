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

		// 
		private bool _isActive;
		private Queue<StrafeRunCinematicCam> _activeSequence;
		private Camera _activeCam;
		#endregion




		#region constructorDestructor
		public StrafeRunCinematicCamController()
		{

		}


		public void destructor()
		{
			World.RenderingCamera = null;
			World.DestroyAllCameras();
		}
		#endregion




		#region publicMethods
		public void initializeCinematicCamSequence(List<Vehicle> vehicles, List<Ped> targets, Vector3 targetPos)
		{
			// randomly select a sequence from available sequences
			StrafeRunCinematicCam[] selectedSequence = _sequences[rng.Next(0, _sequences.Length)];
			_activeSequence = new Queue<SRCC>(selectedSequence);

		}



		/// <summary>
		/// Call this method on each tick to control the active StrafeRun
		/// </summary>
		/// <returns></returns>
		public bool onTick()
		{
			return true;
		}
		#endregion



		#region sequences
		private StrafeRunCinematicCam[][] _sequences = {
			// seq0: follow strafe vehicle to the end
			new StrafeRunCinematicCam[] {
				new SRCC(SRCC_CCT.followStrafeVehicle, new SRCC_CT(0, 1250, 25, 25)),
			},

			// seq1: look at strike target from player's perspective
			new StrafeRunCinematicCam[] {
				new SRCC(SRCC_CCT.playerLookAtTarget, new SRCC_CT(0, 500, 25, 25)),
			}
		};
		#endregion
	}





	public struct StrafeRunCinematicCam
	{
		#region transition
		public struct CameraTransition
		{
			public int _duration;			// duration of the previous cam before transitioning to this cam
			public int _transDuration;		// duration of the transition from the previous cam to this cam
			public int _easePosition;
			public int _easeRotation;

			public CameraTransition (int duration, int transDuration, int easePos, int easeRot){
				_duration = duration;
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

		public Camera createCamera(List<Vehicle> vehicles, List<Ped> targets, Vector3 targetPos)
		{
			switch (_type)
			{
				default:
				case cinematicCamType.followStrafeVehicle:
					return createFollowStrafeVehicleCam(vehicles[0], targetPos);
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
