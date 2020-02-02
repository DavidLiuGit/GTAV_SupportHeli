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
	class CustomNativeApi
	{
		public static void TaskSeekCoverFromPlayer(Ped ped, int duration = 0)
		{
			Function.Call(Hash.TASK_SEEK_COVER_FROM_PED, ped.Handle, Game.Player.Character.Handle, duration, true);
		}

		public static void TaskExitCover(Ped ped, int p1, Vector3 pos)
		{
			Function.Call(Hash.TASK_EXIT_COVER, ped, p1, pos.X, pos.Y, pos.Z);
		}
	}
}
