using System;

namespace Pug.Cereal
{
	public interface IGrain
	{
		string Identifier { get; }
		string Resource { get; }
		String Subject { get; }
		DateTime Timestamp { get; }
		bool Equals(IGrain other);
	}
}