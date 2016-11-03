using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pug.Cereal
{
	internal class GrainComplex
	{
		Resource resource;
		object releaseSync = new object();		

		public GrainComplex(Grain grain, Resource resource)
		{
			this.Grain = grain;
			this.resource = resource;
		}

		public Grain Grain
		{
			get;
			protected set;
		}

		public event EventHandler<string> Returned;

		public void Return()
		{
			lock (releaseSync)
			{
				resource.Release(Grain);

				if (Returned != null)
					Returned(this, Grain.Identifier);
			}
		}
	}
}
