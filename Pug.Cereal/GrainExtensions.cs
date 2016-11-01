using System;

namespace Pug.Cereal
{
	public static class GrainExtensions
	{
		public static bool HasExpired(this Grain grain, int minimumPeriod)
		{
			double heldPeriod = DateTime.Now.Subtract(grain.Timestamp).TotalMilliseconds;

			if (heldPeriod < grain.Duration)
				return false;

			return heldPeriod - grain.Duration >= minimumPeriod;
		}

		public static bool IsEmpty(this Grain grain)
		{
			return grain.Equals(Grain.Empty);
		}
	}
}
