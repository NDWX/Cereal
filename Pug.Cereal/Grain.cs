using System;

namespace Pug.Cereal
{
	public class Grain : IEquatable<Grain>
	{
		public Grain(string identifier, string subject, string resource, TimeSpan duration)
		{
			this.Timestamp = DateTime.Now;
			this.Identifier = identifier;
			this.Subject = subject;
			this.Resource = resource;
			this.Duration = duration;
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

		public TimeSpan Duration
		{
			get;
			private set;
		}

		public bool Equals(Grain other)
		{
			return Identifier == other.Identifier && Subject == other.Subject && Timestamp == other.Timestamp && Duration == other.Duration;
		}
	}
}
