using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTA;
using GTA.Math;

namespace GFPS
{
	class Helper
	{
		static Random rng = new Random();

		/// <summary>
		/// Generate an offset Vector3. Offset by specified height, and at a random point in a circle, defined by <c>haloRadius</c>
		/// </summary>
		/// <param name="height">height, in meters</param>
		/// <param name="haloRadius">radius of circle to pick a point from</param>
		/// <returns></returns>
		public static Vector3 getOffsetVector3(float height, float haloRadius = 0.0f)
		{
			float x = 0.0f, y = 0.0f;

			// if haloRadius is > 0.0, then randomly generate x & y within the radius
			if (haloRadius > 0.0f)
			{
				double x2 = rng.NextDouble() * haloRadius * haloRadius;			// 0.0 < x^2 < haloRadius^2
				double y2 = Math.Pow(haloRadius, 2.0f) - x2;						// y^2 = haloRadius^2 - x^2
				x = (float)Math.Sqrt(x2);
				y = (float)Math.Sqrt(y2);
			}

			return new Vector3(x, y, height);
		}



		/// <summary>
		/// Given a target position and a radius, return a random coordinate on the edge of the circle.
		/// </summary>
		/// <param name="radius">Radius of the circle, in meters</param>
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
		};
	}
}
