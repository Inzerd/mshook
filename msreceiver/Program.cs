using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace msreceiver
{
	internal class Program
	{
		private static readonly int port = 8080;
		static void Main(string[] args)
		{
			using(var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)) 
			{ 
				socket.Bind(new IPEndPoint(IPAddress.Any, port));
				var buffer = new byte[1024];
				while (true)
				{
					try
					{
						var byteReceviver = socket.Receive(buffer);
						var data = Encoding.UTF8.GetString(buffer, 0, byteReceviver);						
						Console.WriteLine(data);
					}
					catch (Exception ex) { Console.WriteLine(ex.ToString()); }					
				}
			}
		}
	}
}
