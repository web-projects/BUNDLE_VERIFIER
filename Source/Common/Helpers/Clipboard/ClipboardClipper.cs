using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Helpers.Clipboard
{
    public static class ClipboardClipper
    {
        #region --- CLIPBOARD COPY DEFINITIONS ---
        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll")]
        private static extern uint GetLastError();

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalFree(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        // In .NET Framework there was a special case for a few function names and CopyMemory happened to be one of them.
        // The special case was removed in .NET Core: "CopyMemory" becomes "RtlMoveMemory"
        //[DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr data);

        // ReSharper disable InconsistentNaming
        const uint CF_TEXT = 1;
        const uint CF_UNICODETEXT = 13;
        // ReSharper restore InconsistentNaming

        #endregion --- CLIPBOARD COPY DEFINITIONS ---

        #region --- CLIPBOARD COPY IMPLEMENTATION ---
        [STAThread]
        public static Result PushStringToClipboard(string message)
        {
            var isAscii = (message != null && (message == Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(message))));
            if (isAscii)
            {
                return PushUnicodeStringToClipboard(message);
            }
            else
            {
                return PushAnsiStringToClipboard(message);
            }
        }

        [STAThread]
        public static Result PushUnicodeStringToClipboard(string message)
        {
            return __PushStringToClipboard(message, CF_UNICODETEXT);
        }

        [STAThread]
        public static Result PushAnsiStringToClipboard(string message)
        {
            return __PushStringToClipboard(message, CF_TEXT);
        }

        [STAThread]
        private static Result __PushStringToClipboard(string message, uint format)
        {
            OpenClipboard(IntPtr.Zero);
            //IntPtr ptr = Marshal.StringToHGlobalUni(source);
            //SetClipboardData(13, ptr);
            //CloseClipboard();
            //Marshal.FreeHGlobal(ptr);
            try
            {
                try
                {
                    if (message is null)
                    {
                        return new Result { ResultCode = ResultCode.ErrorInvalidArgs };
                    }

                    if (!OpenClipboard(IntPtr.Zero))
                    {
                        return new Result { ResultCode = ResultCode.ErrorOpenClipboard, LastError = GetLastError() };
                    }

                    try
                    {
                        uint sizeOfChar;
                        switch (format)
                        {
                            case CF_TEXT:
                            sizeOfChar = 1;
                            break;
                            case CF_UNICODETEXT:
                            sizeOfChar = 2;
                            break;
                            default:
                            throw new Exception("Not Reachable");
                        }

                        var characters = (uint)message.Length;
                        uint bytes = (characters + 1) * sizeOfChar;

                        // ReSharper disable once InconsistentNaming
                        const int GMEM_MOVABLE = 0x0002;
                        // ReSharper disable once InconsistentNaming
                        const int GMEM_ZEROINIT = 0x0040;
                        // ReSharper disable once InconsistentNaming
                        const int GHND = GMEM_MOVABLE | GMEM_ZEROINIT;

                        // IMPORTANT: SetClipboardData requires memory that was acquired with GlobalAlloc using GMEM_MOVABLE.
                        IntPtr hGlobal = NewMethod(bytes, GHND);
                        if (hGlobal == IntPtr.Zero)
                        {
                            return new Result { ResultCode = ResultCode.ErrorGlobalAlloc, LastError = GetLastError() };
                        }

                        try
                        {
                            // IMPORTANT: Marshal.StringToHGlobalUni allocates using LocalAlloc with LMEM_FIXED.
                            //            Note that LMEM_FIXED implies that LocalLock / LocalUnlock is not required.
                            IntPtr source;
                            switch (format)
                            {
                                case CF_TEXT:
                                source = Marshal.StringToHGlobalAnsi(message);
                                break;
                                case CF_UNICODETEXT:
                                source = Marshal.StringToHGlobalUni(message);
                                break;
                                default:
                                throw new Exception("Not Reachable");
                            }

                            try
                            {
                                IntPtr target = NewMethod1(hGlobal);

                                if (target == IntPtr.Zero)
                                {
                                    return new Result { ResultCode = ResultCode.ErrorGlobalLock, LastError = GetLastError() };
                                }

                                try
                                {
                                    CopyMemory(target, source, bytes);
                                }
                                finally
                                {
                                    var ignore = GlobalUnlock(target);
                                }

                                if (SetClipboardData(format, hGlobal).ToInt64() != 0)
                                {
                                    // IMPORTANT: SetClipboardData takes ownership of hGlobal upon success.
                                    hGlobal = IntPtr.Zero;
                                }
                                else
                                {
                                    return new Result { ResultCode = ResultCode.ErrorSetClipboardData, LastError = GetLastError() };
                                }
                            }
                            finally
                            {
                                // Marshal.StringToHGlobalUni actually allocates with LocalAlloc, thus we should theorhetically use LocalFree to free the memory...
                                // ... but Marshal.FreeHGlobal actully uses a corresponding version of LocalFree internally, so this works, even though it doesn't
                                //  behave exactly as expected.
                                Marshal.FreeHGlobal(source);
                            }
                        }
                        catch (OutOfMemoryException)
                        {
                            return new Result { ResultCode = ResultCode.ErrorOutOfMemoryException, LastError = GetLastError() };
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            return new Result { ResultCode = ResultCode.ErrorArgumentOutOfRangeException, LastError = GetLastError() };
                        }
                        finally
                        {
                            if (hGlobal != IntPtr.Zero)
                            {
                                var ignore = GlobalFree(hGlobal);
                            }
                        }
                    }
                    finally
                    {
                        CloseClipboard();
                    }
                    return new Result { ResultCode = ResultCode.Success };
                }
                catch (Exception)
                {
                    return new Result { ResultCode = ResultCode.ErrorException, LastError = GetLastError() };
                }
            }
            catch (Exception)
            {
                return new Result { ResultCode = ResultCode.ErrorGetLastError };
            }
        }

        private static IntPtr NewMethod1(IntPtr hGlobal)
        {
            return GlobalLock(hGlobal);
        }

        private static IntPtr NewMethod(uint bytes, uint GHND)
        {
            return GlobalAlloc(GHND, (UIntPtr)bytes);
        }
        #endregion --- CLIPBOARD COPY IMPLEMENTATION ---
    }
}
