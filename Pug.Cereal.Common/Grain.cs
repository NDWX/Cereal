using System;
using System.Text;

namespace Pug.Cereal
{
	public struct Grain : IEquatable<Grain>
	{
		public Grain(string identifier, string subject, string resource)
		{
			this.Timestamp = DateTime.Now;
			this.Identifier = identifier;
			this.Subject = subject;
			this.Resource = resource;
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

		//static Grain empty = new Grain(string.Empty, string.Empty, string.Empty, 0);

		public static Grain Empty
		{
			get
			{
				return new Grain(string.Empty, string.Empty, string.Empty);
			}
		}

	}
}
