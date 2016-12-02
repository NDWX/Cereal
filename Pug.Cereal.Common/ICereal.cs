using System;

namespace Pug.Cereal
{
	public interface ICereal
	{
		IGrain Lock(string subject, string resource, int timeout = 0);

		void Release(IGrain grain);
	}
}
