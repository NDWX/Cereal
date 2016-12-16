using System;

namespace Pug.Cereal
{
	public static class GrainExtensions
	{
		public static bool IsEmpty(this Grain grain)
		{
			return grain.Equals(Grain.Empty);
		}
	}
}
