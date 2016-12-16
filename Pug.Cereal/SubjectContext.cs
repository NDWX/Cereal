using System.Collections.Generic;
using System.Collections.Specialized;

namespace Pug.Cereal
{
	public class SubjectContext //: ISubjectContext
	{
		object waitSync = new object();

		string waitingFor = string.Empty;
		SortedSet<string> heldResources;

		public SubjectContext(string subject)
		{
			this.Subject = subject;
			this.heldResources = new SortedSet<string>();
		}
		
		public string Subject
		{
			get;
			protected set;
		}

		public IReadOnlyCollection<string> HeldResources
		{
			get
			{
				return (IReadOnlyCollection<string>)this.heldResources;
			}
		}

		public bool IsWaitingForResource
		{
			get
			{
				return !string.IsNullOrEmpty(waitingFor);
			}
		}

		public bool IsWaitingFor(IEnumerable<string> resources)
		{
			if (string.IsNullOrEmpty(waitingFor))
				return false;

			IEnumerator<string> resourceEnumerator = resources.GetEnumerator();

			while(resourceEnumerator.MoveNext())
			{
				if (waitingFor == resourceEnumerator.Current)
					return true;
			}

			return false;
		}

		void HandleWaitEvent(string resource, bool? succeeded)
		{
			lock(waitSync)
			{
				if( succeeded == null )
				{
					if( !string.IsNullOrEmpty(this.waitingFor) )
					{
						throw new System.Exception("Multiple wait");
					}
					else 
					{
						this.waitingFor = resource;
					}
				}
				else 
				{
					heldResources.Add(resource);

					this.waitingFor = string.Empty;
				}
			}
		}

		public void WaitingFor(string resource)
		{
			HandleWaitEvent(resource, null);
		}

		public void WaitTimeout()
		{
			HandleWaitEvent(this.waitingFor, false);
		}

		public void WaitSuccessful()
		{
			HandleWaitEvent(this.waitingFor, true);
		}

		public void Released(string resource)
		{
			heldResources.Remove(resource);
		}
	}
}
