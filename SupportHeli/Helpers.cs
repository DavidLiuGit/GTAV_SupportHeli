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
				Random rand = new Random();
				double x2 = rand.NextDouble() * haloRadius * haloRadius;			// 0.0 < x^2 < haloRadius^2
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
		public static Vector3 getVector3NearPlayer(float radius, Vector3 targetPos)
		{
			return targetPos + getOffsetVector3(0.0f, radius);
		}

	}
}
