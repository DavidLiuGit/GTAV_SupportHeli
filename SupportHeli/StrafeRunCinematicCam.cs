using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTA;
using GTA.Math;


namespace GFPS
{
	public class StrafeRunCinematicCamController
	{
		#region properties
		private Random rng = new Random();

		// 
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




		public void initializeCinematicCamSequence(List<Vehicle> vehicles, List<Ped> targets, Vector3 targetPos)
		{

		}


		#region sequences


		#endregion
	}





	public struct StrafeRunCinematicCamSequence
	{
		public struct CameraTransition
		{
			public int duration;
			public int easePosition;
			public int easeRotation;
		}

		public CameraTransition[] transitionArray;
	}
}
