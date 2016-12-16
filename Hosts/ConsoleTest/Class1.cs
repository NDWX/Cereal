using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CapnProto;

namespace ConsoleTest
{
	public struct RequestInfo
	{
		public string Subject;
		public int WaitTimeout;
	}

    public class Class1
    {
		public static int Main(string[] args)
		{
			RequestInfo.Create();

			return 1;
		}
    }
}
