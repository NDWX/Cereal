using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Threading;

using System.Security;

namespace Pug.Cereal.Hosting.Tcp
{
    public class Listener
    {
		IPEndPoint endPoint;
		TcpListener listener;
		byte[] protectedKey;

		public Listener(IPEndPoint endPoint, byte[] protectedKey)
		{
			this.endPoint = endPoint;
			this.protectedKey = protectedKey;
		}

		public void Start()
		{
			listener = new TcpListener(endPoint);

			listener.Start();

			listener.BeginAcceptTcpClient(new AsyncCallback(acceptConnection), listener);
		}

		void acceptConnection(IAsyncResult result)
		{
			listener.BeginAcceptTcpClient(new AsyncCallback(acceptConnection), listener);

			TcpClient client = null;
			
			try
			{
				client = listener.EndAcceptTcpClient(result);

				NetworkStream networkStream = client.GetStream();

				byte[] requestData = new byte[8192];

				int dataReceived = networkStream.Read(requestData, 0, client.Available <= requestData.Length? client.Available: requestData.Length);


			}
			catch(Exception)
			{
				//todo: log warning
			}
		}
    }

	public class RequestInfo
	{
		string _string;

		public RequestInfo(IPEndPoint source, string version, string subject, string resource, int timeout, int duration)
		{
			this.Source = source;
			this.Version = version;
			this.Subject = subject;
			this.Resource = resource;
			this.Timeout = timeout;
			this.Duration = duration;
		}

		public IPEndPoint Source
		{
			get;
			protected set;
		}

		public string Version
		{
			get;
			protected set;
		}

		public string Subject
		{
			get;
			protected set;
		}

		public string Resource
		{
			get;
			protected set;
		}

		public int Timeout
		{
			get;
			protected set;
		}

		public int Duration
		{
			get;
			protected set;
		}

		public override string ToString()
		{
			return base.ToString();
		}
	}

	public interface IRequestParser
	{
		RequestInfo Parse(byte[] data);
	}

	public interface IRequestAuthenticator
	{
		bool Authenticate(byte[] payload, UInt64 context, byte[] signature);
	}

	public class DefaultRequestParser : IRequestParser
	{
		IRequestAuthenticator authenticator;

		public DefaultRequestParser(IRequestAuthenticator authenticator)
		{
			this.authenticator = authenticator;
		}

		public RequestInfo Parse(byte[] data)
		{
			ushort contextMask = 65520;
			ushort dataLengthMask = 4095;

			int dataLength = BitConverter.ToUInt16(new byte[] { data[2], data[3] }, 0) & dataLengthMask;
			int signatureStart = dataLength + 4;
			int signatureLength = data.Length - signatureStart;

			byte[] payLoad = new byte[dataLength];
			byte[] signature = new byte[signatureLength];

			byte version = data[0];

			UInt64 context = BitConverter.ToUInt64(new byte[] { data[1], data[2] }, 0) & contextMask;

			Array.Copy(data, 4, payLoad, 0, dataLength);

			Array.Copy(data, signatureStart, signature, 0, signatureLength);

			if (!authenticator.Authenticate(data, context, signature))
				throw new InvalidSignature();
			
			
			return null;
		}
	}

	public class InvalidSignature : Exception {

	}
}
