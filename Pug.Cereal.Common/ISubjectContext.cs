using System.Collections.Generic;

namespace Pug.Cereal
{
	public interface ISubjectContext
	{
		bool IsWaitingFor(ICollection<string> resources);

		void RegisterLock(string resource);

		IEnumerable<string> HeldResources
		{
			get;
		}
	}

}
