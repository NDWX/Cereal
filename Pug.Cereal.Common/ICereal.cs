using System;

namespace Pug.Cereal
{
	public interface ICereal : IDisposable
	{
		bool HasDeadlockDetection
		{
			get;
		}

		IGrain Lock(string subject, string resource, int timeout = -1);

		void Release(IGrain grain);
	}
}
