using System;
using System.Text;
using System.Threading;

using Pug.Cereal;

namespace Pug.Cereal
{
	public struct Grain : IEquatable<Grain>, IGrain
	{
		public Grain(string identifier, string subject, string resource, EventWaitHandle lockHandle)
		{
			this.Timestamp = DateTime.Now;
			this.Identifier = identifier;
			this.Subject = subject;
			this.Resource = resource;
			this.WaitHandle = lockHandle;
		}

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

		static Grain empty = new Grain(string.Empty, string.Empty, string.Empty, null);

		public static Grain Empty
		{
			get
			{
				return empty;
			}
		}

	}
}
