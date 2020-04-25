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
	class Helper
	{
		static Random rng = new Random();

		/// <summary>
		/// Generate an offset Vector3. Offset by specified _height, and at a random point in a circle, defined by <c>haloRadius</c>
		/// </summary>
		/// <param name="_height">_height, in meters</param>
		/// <param name="haloRadius">_radius of circle to pick a point from</param>
		/// <returns></returns>
		public static Vector3 getOffsetVector3(float height, float haloRadius = 0.0f)
		{
			float x = 0.0f, y = 0.0f;

			// randomly generate the magnitude of x & y
			if (haloRadius > 0.0f)
			{
				double x2 = rng.NextDouble() * haloRadius * haloRadius;			// 0.0 < x^2 < haloRadius^2
				double y2 = Math.Pow(haloRadius, 2.0f) - x2;						// y^2 = haloRadius^2 - x^2
				x = (float)Math.Sqrt(x2);
				y = (float)Math.Sqrt(y2);
			}

			// randomly determine the signs of x & y, based on quadrant
			int quadrant = rng.Next(4);
			switch (quadrant)
			{
				case 0: return new Vector3(x, y, height);
				case 1: return new Vector3(x, -y, height);
				case 2: return new Vector3(-x, y, height);
				case 3: default: return new Vector3(-x, -y, height);		// add default so all code-paths have return value
			}
			
		}



		/// <summary>
		/// Given a target position and a _radius, return a random coordinate on the edge of the circle.
		/// </summary>
		/// <param name="_radius">Radius of the circle, in meters</param>
		/// <param name="playerPos"><c>Vector3</c> representing player's position</param>
		/// <returns></returns>
		public static Vector3 getVector3NearTarget(float radius, Vector3 targetPos)
		{
			return targetPos + getOffsetVector3(0.0f, radius);
		}



		public static void makeRelationshipGroupHate(RelationshipGroup rg, uint[] hateGroupHashes)
		{
			foreach (uint group in hateGroupHashes)
				rg.SetRelationshipBetweenGroups((RelationshipGroup)group, Relationship.Hate, false);
		}
		public readonly static uint[] defaultHateGroups = new uint[] {
			0xA49E591C,		// cops
			0xF50B51B7,		// rent-a-cops
			0x4325F88A, 0x11DE95FC, 0x8DC30DC3,		// gangs
			0x90C7DA60, 0x11A9A7E3, 0x45897C40, 0xC26D562A, 0x7972FFBD, 0x783E3868, 0x936E7EFB, 0x6A3B9F86, 0xB3598E9C,	// ambient gangs
			0x7EA26372,		// prisoners
			0x8296713E,		// dealers
			0xE3D976F3,		// army
		};



		public static void allyRelationshipGroups(int rg1Hash, int rg2Hash)
		{
			//void SET_RELATIONSHIP_BETWEEN_GROUPS(int relationship, Hash group1, Hash group2)
			Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 0, rg1Hash, rg2Hash);
			Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 0, rg2Hash, rg1Hash);
		}




		#region trigonometry
		/// <summary>
		/// Given a normalized direction Vector3, return a set of Euler angles, describing Rot[ZXY] (i.e. pitch, roll, yaw).
		/// All angles are in degrees. Roll will be set to 0.0
		/// </summary>
		/// <returns><c>Vector3</c> whose x, y, z angles represent pitch, roll, and yaw angles respectively.</returns>
		public static Vector3 getEulerAngles(Vector3 normDirectionVector)
		{
			// calculate angles
			float yaw = (float)(Math.Atan2(normDirectionVector.Y, normDirectionVector.X) * 180 / Math.PI) - 90f;
			float pitch = (float)(Math.Asin(normDirectionVector.Z) * (180 / Math.PI));

			// build the vector & add the offset angle (forwardAngle), if given
			return new Vector3(pitch, 0f, yaw);		// applied in order: pitch, yaw, roll
		}



		/// <summary>
		/// Rotate a 3D vector about the Z-axis (yaw) by a specified angle.
		/// </summary>
		/// <param name="input">Original vector</param>
		/// <param name="angle">Angle, in degrees, to rotate about the Z-axis</param>
		/// <returns></returns>
		public static Vector3 rotateVectorZAxis(Vector3 input, float angleDegrees)
		{
			// convert angle to radians
			float angleRad = (float) (angleDegrees / 180f * Math.PI);

			// x' = x cos θ − y sin θ
			input.X = (float)(input.X * Math.Cos(angleRad) - input.Y * Math.Sin(angleRad));

			// y' = x sin θ + y cos θ
			input.Y = (float)(input.X * Math.Sin(angleRad) + input.Y * Math.Cos(angleRad));

			return input;
		}
		#endregion




		#region raycasting
		/// <summary>
		/// Determine whether a ray can be cast from source to target without hitting the map
		/// </summary>
		/// <param name="source">Position of source</param>
		/// <param name="target">Position of target</param>
		/// <returns></returns>
		public static bool evaluateRaycast(Vector3 source, Vector3 target)
		{
			return !World.Raycast(source, target, IntersectFlags.Map).DidHit;
		}



		/// <summary>
		/// Compute how many rays can hit their target without colliding into the map.
		/// </summary>
		/// <param name="source">Position of the ray source</param>
		/// <param name="targetList">List of positions to target in Raycast</param>
		/// <returns></returns>
		public static int evaluateRaycastHits(Vector3 source, List<Vector3> targetList)
		{
			int collisionCount = 0;

			// iterate over targets in targetList
			foreach (Vector3 target in targetList)
			{
				// raycast from source to target
				RaycastResult rr = World.Raycast(source, target, IntersectFlags.Map);

				// if the ray collided with the map before reaching the target, increment collisionCount
				if (rr.DidHit) collisionCount++;
			}

			// return the number of rays that successfully reached their target
			return targetList.Count - collisionCount;
		}
		#endregion




		#region camera
		/// <summary>
		/// Create a custom camera that imitates the Gameplay Camera
		/// </summary>
		/// <returns></returns>
		public static Camera duplicateGameplayCam()
		{
			Camera cam = World.CreateCamera(GameplayCamera.Position, GameplayCamera.Rotation, GameplayCamera.FieldOfView);
			return cam;
		}
		#endregion
	}
}
