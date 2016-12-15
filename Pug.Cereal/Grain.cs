using System;
using System.Threading;

namespace Pug.Cereal
{
	public class Grain : IEquatable<Grain>, IGrain
	{
#if DETECT_DEADLOCK
		public Grain(string identifier, string subject, SubjectContext subjectContext, string resource, EventWaitHandle lockHandle)
		{
			this.Timestamp = DateTime.Now;
			this.Identifier = identifier;
			this.Subject = subject;
			this.SubjectContext = subjectContext;
			this.Resource = resource;
			this.WaitHandle = lockHandle;
		}
#else
		public Grain(string identifier, string subject, string resource, EventWaitHandle lockHandle)
		{
			this.Timestamp = DateTime.Now;
			this.Identifier = identifier;
			this.Subject = subject;
			this.Resource = resource;
			this.WaitHandle = lockHandle;
		}
#endif

		public string Identifier
		{
			get;
			private set;
		}

		public string Subject
		{
			get;
			private set;
		}
#if DETECT_DEADLOCK
		public SubjectContext SubjectContext
		{
			get;
			private set;
		}
#endif
		public string Resource
		{
			get;
			private set;
		}

		internal EventWaitHandle WaitHandle
		{
			get;
			private set;
		}

		public DateTime Timestamp
		{
			get;
			private set;
		}

		public override string ToString()
		{
			return "Grain:" + Identifier;
		}

		public override int GetHashCode()
		{
			return Identifier.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return obj is Grain && Equals((Grain)obj);
		}

		public bool Equals(Grain other)
		{
			return Identifier == other.Identifier && Subject == other.Subject && Timestamp == other.Timestamp;
		}

		public bool Equals(IGrain other)
		{
			return other is Grain && Equals((Grain)other);
		}

		//static Grain empty = new Grain(string.Empty, string.Empty, string.Empty, 0);

#if DETECT_DEADLOCK
		static Grain empty = new Grain(string.Empty, string.Empty, null, string.Empty, null);
#else
		static Grain empty = new Grain(string.Empty, string.Empty, string.Empty, null);
#endif

		public static Grain Empty
		{
			get
			{
				return empty;
			}
		}

	}
}
