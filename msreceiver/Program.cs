using System;
using System.Collections.Generic;
using System.Diagnostics;
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
				var buffer = new byte[2048];
				while (true)
				{
					try
					{						
						var byteReceviver = socket.Receive(buffer);
						Console.WriteLine($"-- START TRAMISSION {DateTime.Now} --");
						Console.WriteLine($"Log buffer: {Encoding.UTF8.GetString(buffer, 0, buffer.Length)}");
						Console.WriteLine("-- END Trasmission -- \r\n");						
					}
					catch (Exception ex) { Console.WriteLine(ex.ToString()); }					
				}
			}
		}
	}
}
