using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Woof.SystemEx {

    /// <summary>
    /// Global keyboard and mouse events access.
    /// </summary>
    /// <remarks>
    /// An incoming part of the Woof Toolkit.
    /// </remarks>
    public sealed class GlobalInput : IDisposable {

        #region Events

        /// <summary>
        /// Occurs whenever any key is pressed, system-wide.
        /// </summary>
        public event EventHandler<GlobalKeyEventArgs> KeyDown;

        /// <summary>
        /// Occurs whenever any key is released, system-wide.
        /// </summary>
        public event EventHandler<GlobalKeyEventArgs> KeyUp;

        #endregion

        #region Constructor

        /// <summary>
        /// Hooks a managed keyboard event to a low level keyboard event.
        /// </summary>
        public GlobalInput() {
            KeyboardHookId = NativeMethods.SetWindowsHookEx(
                WH_KEYBOARD_LL,
                KeyboardHookInstance = KeyboardLowLevelHandler,
                NativeMethods.GetModuleHandle(MainModuleName), 0
            );
            GC.KeepAlive(KeyboardHookInstance);
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Sends left click event at current cursor position.
        /// </summary>
        public void LeftClick() => NativeMethods.mouse_event(MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);

        /// <summary>
        /// Sends left click event at specified position.
        /// </summary>
        /// <param name="position">Absolute cursor position.</param>
        public void LeftClick(Point position) {
            Cursor.Position = position;
            NativeMethods.mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
        }

        /// <summary>
        /// Pastes some text via system clipboard using Ctrl+V shortcut on the foreground window.
        /// </summary>
        /// <param name="text">Text to paste.</param>
        public void Paste(string text) {
            var mainWin = NativeMethods.GetForegroundWindow();
            if (mainWin != IntPtr.Zero) {
                var itsThread = NativeMethods.GetWindowThreadProcessId(mainWin, out uint _);
                var threadInfo = new GUITHREADINFO();
                threadInfo.cbSize = Marshal.SizeOf(threadInfo);
                NativeMethods.GetGUIThreadInfo(itsThread, ref threadInfo);
                var target = threadInfo.hwndFocus;
                if (target != IntPtr.Zero) {
                    var clipboardTextBackup = Clipboard.ContainsText() ? Clipboard.GetText() : null;
                    Clipboard.SetText(text);
                    NativeMethods.PostMessage(target, WM_KEYDOWN, (int)Keys.LControlKey, 1);
                    NativeMethods.PostMessage(target, WM_KEYDOWN, (int)Keys.V, 1);
                    Thread.Sleep(16); // wait 1 frame for the paste command to be processed.
                    if (clipboardTextBackup != null) Clipboard.SetText(clipboardTextBackup);
                }
            }
        }

        /// <summary>
        /// Removes native hook.
        /// </summary>
        public void Dispose() {
            IsDisposing = true;
            NativeMethods.UnhookWindowsHookEx(KeyboardHookId);
        }

        #endregion

        #region Destructor

        /// <summary>
        /// Disposes the hook on destruction.
        /// </summary>
        ~GlobalInput() {
            if (!IsDisposing) Dispose();
        }

        #endregion

        #region Internal handler

        /// <summary>
        /// Low level keyboard event handler. Invokes managed events.
        /// </summary>
        /// <param name="nCode">A code the hook procedure uses to determine how to process the message. If nCode is less than zero, the hook procedure must pass the message to the CallNextHookEx function without further processing and should return the value returned by CallNextHookEx.</param>
        /// <param name="wParam">The identifier of the keyboard message. This parameter can be one of the following messages: WM_KEYDOWN, WM_KEYUP, WM_SYSKEYDOWN, or WM_SYSKEYUP.</param>
        /// <param name="lParam">A pointer to a KBDLLHOOKSTRUCT structure.</param>
        /// <returns>Pointer to a low level keyboard event handler.</returns>
        private IntPtr KeyboardLowLevelHandler(int nCode, IntPtr wParam, IntPtr lParam) {
            var msg = (int)wParam;
            if (nCode >= 0 && (msg == WM_KEYDOWN || msg == WM_KEYUP)) {
                var key = Marshal.ReadInt32(lParam);
                var keyEventArgs = new GlobalKeyEventArgs(key, Modifiers);
                switch (msg) {
                    case WM_KEYDOWN:
                        KeyDown?.Invoke(this, keyEventArgs);
                        switch ((Keys)key) {
                            case Keys.LControlKey:
                            case Keys.RControlKey:
                                Modifiers |= Keys.Control;
                                break;
                            case Keys.LMenu:
                            case Keys.RMenu:
                                Modifiers |= Keys.Alt;
                                break;
                            case Keys.LShiftKey:
                            case Keys.RShiftKey:
                                Modifiers |= Keys.Shift;
                                break;
                            case Keys.LWin:
                                Modifiers |= Keys.LWin;
                                break;
                            case Keys.RWin:
                                Modifiers |= Keys.RWin;
                                break;
                        }
                        break;
                    case WM_KEYUP:
                        KeyUp?.Invoke(this, keyEventArgs);
                        switch ((Keys)key) {
                            case Keys.LControlKey:
                            case Keys.RControlKey:
                                Modifiers &= ~Keys.Control;
                                break;
                            case Keys.LMenu:
                            case Keys.RMenu:
                                Modifiers &= ~Keys.Alt;
                                break;
                            case Keys.LShiftKey:
                            case Keys.RShiftKey:
                                Modifiers &= ~Keys.Shift;
                                break;
                            case Keys.LWin:
                                Modifiers &= ~Keys.LWin;
                                break;
                            case Keys.RWin:
                                Modifiers &= ~Keys.RWin;
                                break;
                        }
                        break;
                }
                if (keyEventArgs.IsHandled) return (IntPtr)1; // non zero value causes the event to be "eaten".
            }
            return NativeMethods.CallNextHookEx(KeyboardHookId, nCode, wParam, lParam);
        }

        #endregion

        #region WinAPI

        /// <summary>
        /// WinAPI native methods.
        /// </summary>
        class NativeMethods {

            /// <summary>
            /// Retrieves a module handle for the specified module. The module must have been loaded by the calling process.
            /// </summary>
            /// <param name="lpModuleName">
            /// The name of the loaded module (either a .dll or .exe file).
            /// If the file name extension is omitted, the default library extension .dll is appended.
            /// The file name string can include a trailing point character (.) to indicate that the module name has no extension.
            /// The string does not have to specify a path. When specifying a path, be sure to use backslashes (\), not forward slashes (/).
            /// The name is compared (case independently) to the names of modules currently mapped into the address space of the calling process.
            /// If this parameter is NULL, GetModuleHandle returns a handle to the file used to create the calling process (.exe file).</param>
            /// <returns>If the function succeeds, the return value is a handle to the specified module.</returns>
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern IntPtr GetModuleHandle(string lpModuleName);

            /// <summary>
            /// Installs an application-defined hook procedure into a hook chain. You would install a hook procedure to monitor the system for certain types of events. These events are associated either with a specific thread or with all threads in the same desktop as the calling thread.
            /// </summary>
            /// <param name="idHook">The type of hook procedure to be installed.</param>
            /// <param name="lpfn">A pointer to the hook procedure. If the dwThreadId parameter is zero or specifies the identifier of a thread created by a different process, the lpfn parameter must point to a hook procedure in a DLL. Otherwise, lpfn can point to a hook procedure in the code associated with the current process.</param>
            /// <param name="hMod">A handle to the DLL containing the hook procedure pointed to by the lpfn parameter. The hMod parameter must be set to NULL if the dwThreadId parameter specifies a thread created by the current process and if the hook procedure is within the code associated with the current process.</param>
            /// <param name="dwThreadId">The identifier of the thread with which the hook procedure is to be associated. For desktop apps, if this parameter is zero, the hook procedure is associated with all existing threads running in the same desktop as the calling thread.</param>
            /// <returns>If the function succeeds, the return value is the handle to the hook procedure. </returns>
            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr SetWindowsHookEx(int idHook, KeyboardLowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

            /// <summary>
            /// Removes a hook procedure installed in a hook chain by the SetWindowsHookEx function. 
            /// </summary>
            /// <param name="hhk">A handle to the hook to be removed. This parameter is a hook handle obtained by a previous call to SetWindowsHookEx.</param>
            /// <returns>True if succeeded.</returns>
            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool UnhookWindowsHookEx(IntPtr hhk);

            /// <summary>
            /// Passes the hook information to the next hook procedure in the current hook chain. A hook procedure can call this function either before or after processing the hook information.
            /// </summary>
            /// <param name="hhk">This parameter is ignored.</param>
            /// <param name="nCode">The hook code passed to the current hook procedure.The next hook procedure uses this code to determine how to process the hook information.</param>
            /// <param name="wParam">The wParam value passed to the current hook procedure. The meaning of this parameter depends on the type of hook associated with the current hook chain.</param>
            /// <param name="lParam">The lParam value passed to the current hook procedure. The meaning of this parameter depends on the type of hook associated with the current hook chain.</param>
            /// <returns>This value is returned by the next hook procedure in the chain. The current hook procedure must also return this value. The meaning of the return value depends on the hook type.</returns>
            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

            /// <summary>
            /// The mouse_event function synthesizes mouse motion and button clicks.
            /// </summary>
            /// <param name="dwFlags">Controls various aspects of mouse motion and button clicking.</param>
            /// <param name="dx">The mouse's absolute position along the x-axis or its amount of motion since the last mouse event was generated, depending on the setting of MOUSEEVENTF_ABSOLUTE. Absolute data is specified as the mouse's actual x-coordinate; relative data is specified as the number of minimal distances moved.</param>
            /// <param name="dy">The mouse's absolute position along the y-axis or its amount of motion since the last mouse event was generated, depending on the setting of MOUSEEVENTF_ABSOLUTE. Absolute data is specified as the mouse's actual y-coordinate; relative data is specified as the number of minimal distances moved.</param>
            /// <param name="dwData">
            /// If dwFlags contains MOUSEEVENTF_WHEEL, then dwData specifies the amount of wheel movement. A positive value indicates that the wheel was rotated forward, away from the user; a negative value indicates that the wheel was rotated backward, toward the user. One wheel click is defined as WHEEL_DELTA, which is 120.
            /// If dwFlags contains MOUSEEVENTF_HWHEEL, then dwData specifies the amount of wheel movement.A positive value indicates that the wheel was tilted to the right; a negative value indicates that the wheel was tilted to the left.
            /// If dwFlags contains MOUSEEVENTF_XDOWN or MOUSEEVENTF_XUP, then dwData specifies which X buttons were pressed or released. This value may be any combination of the following flags.
            /// If dwFlags is not MOUSEEVENTF_WHEEL, MOUSEEVENTF_XDOWN, or MOUSEEVENTF_XUP, then dwData should be zero.
            /// </param>
            /// <param name="dwExtraInfo">An additional value associated with the mouse event. An application calls GetMessageExtraInfo to obtain this extra information.</param>
            [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, IntPtr dwExtraInfo);

            /// <summary>
            /// Retrieves a handle to the foreground window (the window with which the user is currently working).
            /// The system assigns a slightly higher priority to the thread that creates the foreground window than it does to other threads.
            /// </summary>
            /// <returns>
            /// The return value is a handle to the foreground window.
            /// The foreground window can be NULL in certain circumstances, such as when a window is losing activation.
            /// </returns>
            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();

            /// <summary>
            /// Retrieves information about the active window or a specified GUI thread.
            /// </summary>
            /// <param name="idThread">The identifier for the thread for which information is to be retrieved. To retrieve this value, use the GetWindowThreadProcessId function. If this parameter is NULL, the function returns information for the foreground thread.</param>
            /// <param name="lpgui">A pointer to a GUITHREADINFO structure that receives information describing the thread.</param>
            /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero.To get extended error information, call GetLastError.</returns>
            [DllImport("user32.dll")]
            public static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

            /// <summary>
            /// Retrieves the identifier of the thread that created the specified window and, optionally, the identifier of the process that created the window.
            /// </summary>
            /// <param name="hWnd">A handle to the window.</param>
            /// <param name="lpdwProcessId">Process identifier.</param>
            /// <returns>Identifier of the thread that created the window.</returns>
            [DllImport("user32.dll", SetLastError = true)]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

            /// <summary>
            /// Places (posts) a message in the message queue associated with the thread that created the specified window and returns without waiting for the thread to process the message.
            /// </summary>
            /// <param name="hWnd">A handle to the window whose window procedure is to receive the message.</param>
            /// <param name="msg">The message to be posted.</param>
            /// <param name="wParam">Additional message-specific information.</param>
            /// <param name="lParam">Additional message-specific information.</param>
            /// <returns>If the function succeeds, the return value is nonzero.</returns>
            [DllImport("user32.Dll")]
            public static extern Int32 PostMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        }

        /// <summary>
        /// Contains information about a GUI thread.
        /// </summary>
        struct GUITHREADINFO {
            /// <summary>
            /// The size of this structure, in bytes. The caller must set this member to sizeof(GUITHREADINFO).
            /// </summary>
            public int cbSize;
            /// <summary>
            /// The thread state. This member can be one or more of the following values.
            /// </summary>
            public int flags;
            /// <summary>
            /// A handle to the active window within the thread.
            /// </summary>
            public IntPtr hwndActive;
            /// <summary>
            /// A handle to the window that has the keyboard focus.
            /// </summary>
            public IntPtr hwndFocus;
            /// <summary>
            /// A handle to the window that has captured the mouse.
            /// </summary>
            public IntPtr hwndCapture;
            /// <summary>
            /// A handle to the window that owns any active menus.
            /// </summary>
            public IntPtr hwndMenuOwner;
            /// <summary>
            /// A handle to the window in a move or size loop.
            /// </summary>
            public IntPtr hwndMoveSize;
            /// <summary>
            /// A handle to the window that is displaying the caret.
            /// </summary>
            public IntPtr hwndCaret;
            /// <summary>
            /// The caret's bounding rectangle, in client coordinates, relative to the window specified by the hwndCaret member.
            /// </summary>
            public System.Drawing.Rectangle rcCaret;
        }

        /// <summary>
        /// Delegate for low level keyboard event handler.
        /// </summary>
        /// <param name="nCode">A code the hook procedure uses to determine how to process the message. If nCode is less than zero, the hook procedure must pass the message to the CallNextHookEx function without further processing and should return the value returned by CallNextHookEx.</param>
        /// <param name="wParam">The identifier of the keyboard message. This parameter can be one of the following messages: WM_KEYDOWN, WM_KEYUP, WM_SYSKEYDOWN, or WM_SYSKEYUP.</param>
        /// <param name="lParam">A pointer to a KBDLLHOOKSTRUCT structure.</param>
        /// <returns>Pointer to a low level keyboard event handler.</returns>
        delegate IntPtr KeyboardLowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        #endregion

        #region WinAPI Constants

        /// <summary>
        /// Installs a hook procedure that monitors low-level keyboard input events.
        /// </summary>
        private const int WH_KEYBOARD_LL = 13;

        /// <summary>
        /// Posted to the window with the keyboard focus when a nonsystem key is pressed. A nonsystem key is a key that is pressed when the ALT key is not pressed.
        /// </summary>
        private const int WM_KEYDOWN = 0x0100;

        /// <summary>
        /// Posted to the window with the keyboard focus when a nonsystem key is released. A nonsystem key is a key that is pressed when the ALT key is not pressed, or a keyboard key that is pressed when a window has the keyboard focus.
        /// </summary>
        private const int WM_KEYUP = 0x00101;

        /// <summary>
        /// Sets the text of a window.
        /// </summary>
        const int WM_SETTEXT = 0x000C;

        /// <summary>
        /// The dx and dy parameters contain normalized absolute coordinates. If not set, those parameters contain relative data: the change in position since the last reported position. This flag can be set, or not set, regardless of what kind of mouse or mouse-like device, if any, is connected to the system.
        /// </summary>
        private const int MOUSEEVENTF_ABSOLUTE = 0x8000;

        /// <summary>
        /// Movement occurred.
        /// </summary>
        private const int MOUSEEVENTF_MOVE = 0x0001;

        /// <summary>
        /// The left button is down.
        /// </summary>
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;

        /// <summary>
        /// The left button is up.
        /// </summary>
        private const int MOUSEEVENTF_LEFTUP = 0x0004;

        /// <summary>
        /// The right button is down.
        /// </summary>
        private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;

        /// <summary>
        /// The right button is up.
        /// </summary>
        private const int MOUSEEVENTF_RIGHTUP = 0x0010;

        #endregion

        #region State


        /// <summary>
        /// Current modifier keys.
        /// </summary>
        private Keys Modifiers = Keys.None;

        /// <summary>
        /// Instance of the keyboard hook procedure to be kept alive during the application lifetime.
        /// </summary>
        private readonly KeyboardLowLevelProc KeyboardHookInstance;

        /// <summary>
        /// Hook identifier.
        /// </summary>
        private IntPtr KeyboardHookId = IntPtr.Zero;

        /// <summary>
        /// Gets the main module name of the current process.
        /// </summary>
        private string MainModuleName {
            get {
                using (var process = Process.GetCurrentProcess())
                using (var mainModule = process.MainModule) return mainModule.ModuleName;
            }
        }

        /// <summary>
        /// True if the disposal process was started.
        /// </summary>
        private bool IsDisposing;

        #endregion

    }

    /// <summary>
    /// Global keyboard event arguments, based on WinForms <see cref="KeyEventArgs"/>.
    /// </summary>
    public sealed class GlobalKeyEventArgs : EventArgs {

        /// <summary>
        /// Gets or sets a value indicating whether the keyboard event is already handled and should not be passed further.
        /// </summary>
        public bool IsHandled { get; set; }

        /// <summary>
        /// Gets the key code from <see cref="Keys"/> enumeration.
        /// </summary>
        public Keys KeyCode { get; }

        /// <summary>
        /// Gets the key code from <see cref="Keys"/> enumeration combined with modifier keys.
        /// </summary>
        public Keys KeyData { get; }

        /// <summary>
        /// Gets the numeric value of the key.
        /// </summary>
        public int KeyValue { get; }

        /// <summary>
        /// Gets the modifier keys.
        /// </summary>
        public Keys Modifiers { get; }

        /// <summary>
        /// Creates new <see cref="GlobalKeyEventArgs"/> from integer value.
        /// </summary>
        /// <param name="value">Keyboard code value.</param>
        public GlobalKeyEventArgs(int value, Keys modifiers = Keys.None) {
            KeyCode = (Keys)value;
            KeyData = KeyCode | modifiers;
            KeyValue = value;
            Modifiers = modifiers;
        }

    }

}