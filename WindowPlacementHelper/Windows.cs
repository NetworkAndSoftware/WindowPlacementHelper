using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

namespace WindowPlacementHelper
{
  public class Windows
  {
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumedWindow lpEnumFunc, ArrayList lParam);

    public static IEnumerable<Window> EnumWindows()
    {
      var windowHandles = new ArrayList();
      EnumedWindow callBackPtr = GetWindowHandle;
      EnumWindows(callBackPtr, windowHandles);

      return windowHandles.Cast<IntPtr>().Select(i => new Window(i));
    }

    private static bool GetWindowHandle(IntPtr windowHandle, ArrayList handles)
    {
      handles.Add(windowHandle);
      return true;
    }

    private delegate bool EnumedWindow(IntPtr handleWindow, ArrayList handles);

    [StructLayout(LayoutKind.Sequential)]
// ReSharper disable once InconsistentNaming
    private struct RECT
    {
      public int Left, Top, Right, Bottom;

      public RECT(int left, int top, int right, int bottom)
      {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
      }

      public RECT(Rectangle r) : this(r.Left, r.Top, r.Right, r.Bottom)
      {
      }

      public int X
      {
        get { return Left; }
        set
        {
          Right -= (Left - value);
          Left = value;
        }
      }

      public int Y
      {
        get { return Top; }
        set
        {
          Bottom -= (Top - value);
          Top = value;
        }
      }

      public int Height
      {
        get { return Bottom - Top; }
        set { Bottom = value + Top; }
      }

      public int Width
      {
        get { return Right - Left; }
        set { Right = value + Left; }
      }

      public Point Location
      {
        get { return new Point(Left, Top); }
        set
        {
          X = value.X;
          Y = value.Y;
        }
      }

      public Size Size
      {
        get { return new Size(Width, Height); }
        set
        {
          Width = value.Width;
          Height = value.Height;
        }
      }

      public static implicit operator Rectangle(RECT r)
      {
        return new Rectangle(r.Left, r.Top, r.Width, r.Height);
      }

      public static implicit operator RECT(Rectangle r)
      {
        return new RECT(r);
      }

      public static bool operator ==(RECT r1, RECT r2)
      {
        return r1.Equals(r2);
      }

      public static bool operator !=(RECT r1, RECT r2)
      {
        return !r1.Equals(r2);
      }

      public bool Equals(RECT r)
      {
        return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
      }

      public override bool Equals(object obj)
      {
        if (obj is RECT)
          return Equals((RECT) obj);
        if (obj is Rectangle)
          return Equals(new RECT((Rectangle) obj));
        return false;
      }

      public override int GetHashCode()
      {
        return ((Rectangle) this).GetHashCode();
      }

      public override string ToString()
      {
        return string.Format(CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right,
          Bottom);
      }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
      public int X;
      public int Y;

      private POINT(int x, int y)
      {
        this.X = x;
        this.Y = y;
      }

      public POINT(System.Drawing.Point pt) : this(pt.X, pt.Y) { }

      public static implicit operator System.Drawing.Point(POINT p)
      {
        return new System.Drawing.Point(p.X, p.Y);
      }

      public static implicit operator POINT(System.Drawing.Point p)
      {
        return new POINT(p.X, p.Y);
      }
    }
    public class Window
    {
      private readonly IntPtr hWnd;

      internal Window(IntPtr hwnd)
      {
        hWnd = hwnd;
      }

      [DllImport("user32.dll", SetLastError = false)]
      static extern IntPtr GetDesktopWindow();

      public static Window GetDesktop()
      {
        return new Window(GetDesktopWindow());
      }

      public IntPtr Handle
      {
        get
        {
          return hWnd;
        }
      }

      [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
      private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

      [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
      private static extern int GetWindowTextLength(IntPtr hWnd);

      public int GetWindowTextLength()
      {
        return GetWindowTextLength(hWnd);
      }


      private string _text;

      public string Text
      {
        get
        {
          if (null == _text)
          {
            // Allocate correct string length first
            var length = GetWindowTextLength(hWnd);
            var sb = new StringBuilder(length + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            _text = sb.ToString();
          }

          return _text;
        }
      }

      #region window info

      public Info GetInfo()
      {
        return new Info(hWnd);
      }
      
      public class Info
      {
        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWINFO
        {
          public uint cbSize;
          public RECT rcWindow;
          public RECT rcClient;
          public uint dwStyle;
          public uint dwExStyle;
          public uint dwWindowStatus;
          public uint cxWindowBorders;
          public uint cyWindowBorders;
          public ushort atomWindowType;
          public ushort wCreatorVersion;

          static public WINDOWINFO CreateOne()
          {
            return new WINDOWINFO { cbSize = (UInt32)(Marshal.SizeOf(typeof(WINDOWINFO))) };
          }
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);

        private WINDOWINFO? windowinfo;

        internal Info(IntPtr hWnd)
        {
          var w = WINDOWINFO.CreateOne();
          if (GetWindowInfo(hWnd, ref w))
            windowinfo = w;
          else
            throw new Exception();
        }

        public Rectangle ClientRectangle
        {
          get
          {
            return windowinfo.Value.rcClient;
          }
        }

        public Rectangle WindowRectangle
        {
          get
          {
            return windowinfo.Value.rcWindow;
          }
        }

        public StyleType Style
        {
          get
          {
            return new StyleType(windowinfo.Value.dwStyle);
          }
        }

        public class StyleType
        {
          UInt32 dwStyle;

          internal StyleType(UInt32 dwstyle)
          {
            dwStyle = dwstyle;
          }

          private bool GetBit(long bit)
          {
            return Convert.ToBoolean(dwStyle & bit);
          }

          private void SetBit(long bit, bool value)
          {
            if (value)
              dwStyle |= (UInt32)bit;
            else
            {
              dwStyle &= ~(UInt32)bit;
            }
          }

          // The window has a thin-line border.
          public bool Border
          {
            get
            {
              return GetBit(0x00800000);
            }
            set
            {
              SetBit(0x00800000, value);
            }
          }

          // The window is a child window. A window with this style cannot have a menu bar. This style cannot be used with the WS_POPUP style.        
          public bool ChildWindow
          {
            get
            {
              return GetBit(0x00400000);
            }
            set
            {
              SetBit(0x00400000, value);
            }
          }

          // The window has a title bar (includes the WS_BORDER style).
          public bool Caption
          {
            get
            {
              return Border && ChildWindow;
            }
            set
            {
              Border = ChildWindow = value;
            }
          }

          // Excludes the area occupied by child windows when drawing occurs within the parent window. This style is used when creating the parent window.
          public bool ClipChildren
          {
            get
            {
              return GetBit(0x00200000);
            }
            set
            {
              SetBit(0x00200000, value);
            }
          }

          // Clips child windows relative to each other; that is, when a particular child window receives a WM_PAINT message, the WS_CLIPSIBLINGS style clips all other overlapping child windows out of the region of the child window to be updated. If WS_CLIPSIBLINGS is not specified and child windows overlap, it is possible, when drawing within the client area of a child window, to draw within the client area of a neighboring child window. 
          public bool ClipSiblings
          {
            get
            {
              return GetBit(0x04000000);
            }
            set
            {
              SetBit(0x04000000, value);
            }
          }


          // The window is initially disabled. A disabled window cannot receive input from the user. To change this after a window has been created, use the EnableWindow function.
          public bool Disabled
          {
            get
            {
              return GetBit(0x08000000L);
            }
            set
            {
              SetBit(0x08000000L, value);
            }
          }

          // The window has a border of a style typically used with dialog boxes. A window with this style cannot have a title bar.
          public bool DialogFrame
          {
            get
            {
              return GetBit(0x00400000L);
            }
            set
            {
              SetBit(0x00400000L, value);
            }
          }

          // The window is the first control of a group of controls. The group consists of this first control and all controls defined after it, up to the next control with the WS_GROUP style. The first control in each group usually has the WS_TABSTOP style so that the user can move from group to group. The user can subsequently change the keyboard focus from one control in the group to the next control in the group by using the direction keys.
          // You can turn this style on and off to change dialog box navigation. To change this style after a window has been created, use the SetWindowLong function.
          public bool Group
          {
            get
            {
              return GetBit(0x00020000L);
            }
            set
            {
              SetBit(0x00020000L, value);
            }
          }

          public bool Visible
          {
            get
            {
              return GetBit(0x10000000L);
            }
            set
            {
              SetBit(0x10000000L, value);
            }
          }


          // The window is initially minimized. Same as the WS_MINIMIZE style.
          public bool Minimized
          {
            get
            {
              return GetBit(0x20000000L);
            }
            set
            {
              SetBit(0x20000000L, value);
            }
          }

          // The window is an overlapped window. An overlapped window has a title bar and a border. Same as the WS_TILED style.
          public bool Overlapped
          {
            get
            {
              return 0 == dwStyle;
            }
          }

          // The window is an overlapped window. Same as the WS_TILEDWINDOW style.
          public bool OverlappedWindow
          {
            get
            {
              return Caption && SysMenu && ThickFrame && MinimizeBox && MaximizeBox;
            }
            set
            {
              Caption = SysMenu = ThickFrame = MinimizeBox = MaximizeBox = value;
            }
          }

          // The window has a maximize button. Cannot be combined with the WS_EX_CONTEXTHELP style. The WS_SYSMENU style must also be specified.
          public bool MaximizeBox
          {
            get
            {
              return GetBit(0x20000000L);
            }
            set
            {
              SetBit(0x20000000L, value);
            }
          }

          // The window has a minimize button. Cannot be combined with the WS_EX_CONTEXTHELP style. The WS_SYSMENU style must also be specified.
          public bool MinimizeBox
          {
            get
            {
              return GetBit(0x00020000L);
            }
            set
            {
              SetBit(0x00020000L, value);
            }
          }

          // The window has a sizing border. Same as the WS_SIZEBOX style.
          public bool ThickFrame
          {
            get
            {
              return GetBit(0x00040000L);
            }
            set
            {
              SetBit(0x00040000L, value);
            }
          }

          // The window has a window menu on its title bar. The WS_CAPTION style must also be specified.
          public bool SysMenu
          {
            get
            {
              return GetBit(0x20000000L);
            }
            set
            {
              SetBit(0x20000000L, value);
            }
          }
          /*

          WS_HSCROLL
          0x00100000L
          The window has a horizontal scroll bar.

          WS_MAXIMIZE
          0x01000000L
          The window is initially maximized.


          WS_MINIMIZE
          0x20000000L
          The window is initially minimized. Same as the WS_ICONIC style.


          WS_POPUP
          0x80000000L
          The windows is a pop-up window. This style cannot be used with the WS_CHILD style.

          WS_POPUPWINDOW
          (WS_POPUP | WS_BORDER | WS_SYSMENU)
          The window is a pop-up window. The WS_CAPTION and WS_POPUPWINDOW styles must be combined to make the window menu visible.

          WS_SIZEBOX
          0x00040000L
          The window has a sizing border. Same as the WS_THICKFRAME style.

          WS_TABSTOP
          0x00010000L
          The window is a control that can receive the keyboard focus when the user presses the TAB key. Pressing the TAB key changes the keyboard focus to the next control with the WS_TABSTOP style.
            You can turn this style on and off to change dialog box navigation. To change this style after a window has been created, use the SetWindowLong function. For user-created windows and modeless dialogs to work with tab stops, alter the message loop to call the IsDialogMessage function.

          WS_THICKFRAME
          0x00040000L
          The window has a sizing border. Same as the WS_SIZEBOX style.


          WS_VSCROLL
          0x00200000L
           * */
        }


      }

      public Placement GetPlacement()
      {
        return new Placement(hWnd);
      }

      public class Placement
      {
        public enum ShowCommandEnum
        {
          /// <summary>
          /// Hides the window and activates another window.
          /// </summary>
          Hide = 0,

          /// <summary>
          /// Activates and displays a window. If the window is minimized or 
          /// maximized, the system restores it to its original size and position.
          /// An application should specify this flag when displaying the window 
          /// for the first time.
          /// </summary>
          Normal = 1,

          /// <summary>
          /// Activates the window and displays it as a minimized window.
          /// </summary>
          ShowMinimized = 2,

          /// <summary>
          /// Maximizes the specified window.
          /// </summary>
          Maximize = 3, // is this the right value?

          /// <summary>
          /// Activates the window and displays it as a maximized window.
          /// </summary>       
          ShowMaximized = 3,

          /// <summary>
          /// Displays a window in its most recent size and position. This value 
          /// is similar to <see cref="Win32.ShowWindowCommand.Normal"/>, except 
          /// the window is not activated.
          /// </summary>
          ShowNoActivate = 4,

          /// <summary>
          /// Activates the window and displays it in its current size and position. 
          /// </summary>
          Show = 5,

          /// <summary>
          /// Minimizes the specified window and activates the next top-level 
          /// window in the Z order.
          /// </summary>
          Minimize = 6,

          /// <summary>
          /// Displays the window as a minimized window. This value is similar to
          /// <see cref="Win32.ShowWindowCommand.ShowMinimized"/>, except the 
          /// window is not activated.
          /// </summary>
          ShowMinNoActive = 7,

          /// <summary>
          /// Displays the window in its current size and position. This value is 
          /// similar to <see cref="Win32.ShowWindowCommand.Show"/>, except the 
          /// window is not activated.
          /// </summary>
          ShowNA = 8,

          /// <summary>
          /// Activates and displays the window. If the window is minimized or 
          /// maximized, the system restores it to its original size and position. 
          /// An application should specify this flag when restoring a minimized window.
          /// </summary>
          Restore = 9,

          /// <summary>
          /// Sets the show state based on the SW_* value specified in the 
          /// STARTUPINFO structure passed to the CreateProcess function by the 
          /// program that started the application.
          /// </summary>
          ShowDefault = 10,

          /// <summary>
          ///  <b>Windows 2000/XP:</b> Minimizes a window, even if the thread 
          /// that owns the window is not responding. This flag should only be 
          /// used when minimizing windows from a different thread.
          /// </summary>
          ForceMinimize = 11
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
// ReSharper disable once InconsistentNaming
        private struct WINDOWPLACEMENT
        {
          /// <summary>
          /// The length of the structure, in bytes. Before calling the GetWindowPlacement or SetWindowPlacement functions, set this member to sizeof(WINDOWPLACEMENT).
          /// <para>
          /// GetWindowPlacement and SetWindowPlacement fail if this member is not set correctly.
          /// </para>
          /// </summary>
          public int Length;

          /// <summary>
          /// Specifies flags that control the position of the minimized window and the method by which the window is restored.
          /// </summary>
          public int Flags;

          /// <summary>
          /// The current show state of the window.
          /// </summary>
          public ShowCommandEnum ShowCmd;

          /// <summary>
          /// The coordinates of the window's upper-left corner when the window is minimized.
          /// </summary>
          public POINT MinPosition;

          /// <summary>
          /// The coordinates of the window's upper-left corner when the window is maximized.
          /// </summary>
          public POINT MaxPosition;

          /// <summary>
          /// The window's coordinates when the window is in the restored position.
          /// </summary>
          public RECT NormalPosition;

          /// <summary>
          /// Gets the default (empty) value.
          /// </summary>
          public static WINDOWPLACEMENT CreateOne()
          {
            return new WINDOWPLACEMENT()
            {
              Length = Marshal.SizeOf(typeof (WINDOWPLACEMENT))
            };
          }
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

        private WINDOWPLACEMENT? _windowplacement;

        internal Placement(IntPtr hWnd)
        {
          var w = WINDOWPLACEMENT.CreateOne();
          if (GetWindowPlacement(hWnd, ref w))
            _windowplacement = w;
          else
            throw new Exception();
        }

        public ShowCommandEnum ShowCommand
        {
          get
          {
// ReSharper disable once PossibleInvalidOperationException
            return _windowplacement.Value.ShowCmd;
          }
          set
          {
            var placement = _windowplacement.Value;
            placement.ShowCmd = value;
            _windowplacement = placement;
          }
        }
      }
      [DllImport("user32.dll")]
      [return: MarshalAs(UnmanagedType.Bool)]
      static extern bool IsWindowVisible(IntPtr hWnd);

      public bool Visible
      {
        get
        {
          return IsWindowVisible(hWnd);
        }
      }

      [DllImport("user32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);


      public Rectangle Position
      {
        set
        {
// ReSharper disable InconsistentNaming
          const int SWP_ASYNCWINDOWPOS = 0x4000;
          const int SWP_DEFERERASE = 0x2000;
          const int SWP_DRAWFRAME = 0x0020;
          const int SWP_FRAMECHANGED = 0x0020;
          const int SWP_HIDEWINDOW = 0x0080;
          const int SWP_NOACTIVATE = 0x0010;
          const int SWP_NOCOPYBITS = 0x0100;
          const int SWP_NOMOVE = 0x0002;
          const int SWP_NOOWNERZORDER = 0x0200;
          const int SWP_NOREDRAW = 0x0008;
          const int SWP_NOREPOSITION = 0x0200;
          const int SWP_NOSENDCHANGING = 0x0400;
          const int SWP_NOSIZE = 0x0001;
          const int SWP_NOZORDER = 0x0004;
          const int SWP_SHOWWINDOW = 0x0040;

          const int HWND_TOP = 0;
          const int HWND_BOTTOM = 1;
          const int HWND_TOPMOST = -1;
          const int HWND_NOTOPMOST = -2;
// ReSharper restore InconsistentNaming

          SetWindowPos(hWnd, IntPtr.Zero, value.X, value.Y, value.Width, value.Height,
            SWP_ASYNCWINDOWPOS | SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_NOZORDER);
        }
      }

      #endregion
    }
  }
}