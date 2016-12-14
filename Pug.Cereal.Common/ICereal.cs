using System;

namespace Pug.Cereal
{
	public interface ICereal
	{
		bool HasDeadlockDetection
		{
			get;
		}

		IGrain Lock(string subject, string resource, int timeout = 0);

		void Release(IGrain grain);
	}
}
