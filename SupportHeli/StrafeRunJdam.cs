﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using GTA;
using GTA.Math;



namespace GFPS
{
	public struct StrafeRunJdam
	{
		public bool hasExploded;
		private Vector3 _explosionPosition;
		private int _explosionTime;

		public StrafeRunJdam(Vector3 targetPos, float targetRadius)
		{
			hasExploded = false;

			// determine the explosion position using Gaussian random var
			_explosionPosition = targetPos.Around(Helper.randomNormal(0f, targetRadius / 2.8f));

			// determine the explosion time (as milliseconds after start of strafe run)
			_explosionTime = (int)Helper.randomNormal(6000f, 1250f);
		}


		/// <summary>
		/// handle JDAM explosion logic
		/// </summary>
		/// <param name="timeElapsed">milliseconds since strafe run started</param>
		/// <returns><c>true</c> if JDAM has already exploded</returns>
		public bool onTick(int timeElapsed){
			// if JDAM has already exploded, return true immediately
			if (hasExploded) return true;

			// if it is time to explode, then explode
			else if (timeElapsed > _explosionTime){
				explode();
				hasExploded = true;
			}

			return hasExploded;
		}


		/// <summary>
		/// Generate an explosion at the pre-determined explosion position;
		/// </summary>
		private void explode()
		{
			World.AddExplosion(_explosionPosition, ExplosionType.Plane, 12f, 1f);
		}
	}
}
