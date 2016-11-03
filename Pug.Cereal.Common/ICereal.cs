using System;

namespace Pug.Cereal
{
	public interface ICereal
	{
		Grain Lock(string subject, string resource, int timeout = 0);

		void Release(Grain grain);
	}
}
