using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;

namespace WindowPlacementHelper
{
  /// <summary>
  ///   Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();
      
      var timer = new Timer(Callback, null, 0, 30000);
      SystemEvents.DisplaySettingsChanged += SystemEventsOnDisplaySettingsChanged;

      //TaskbarIcon.Icon = SystemIcons.Application;

    }

    private void Callback(object state)
    {
      SaveWindows();
    }

    private void SystemEventsOnDisplaySettingsChanged(object sender, EventArgs eventArgs)
    {
      RestoreWindows();
    }

    private static void Describe(Windows.Window w, Windows.Window.Info info, Windows.Window.Placement placement)
    {
      Trace.WriteLine(String.Format("{3:x8} : Text: '{0}', Rect: {1}, ShowCommand: {2}", w.Text,
        info.WindowRectangle, placement.ShowCommand, (int) w.Handle));
    }

    private static bool IsInteresting(Windows.Window w, Windows.Window.Placement placement)
    {
      if (!w.Visible)
        return false;

      switch (placement.ShowCommand)
      {
        //case Windows.Window.Placement.ShowCommandEnum.ShowMaximized:
        case Windows.Window.Placement.ShowCommandEnum.ShowMinimized:
          return false;
      }

      return true;
    }

    private readonly Dictionary<Rectangle, Dictionary<Windows.Window, Rectangle>> _positions = new Dictionary<Rectangle, Dictionary<Windows.Window, Rectangle>>();
    private AboutWindow _about;

    private void SaveWindows()
    {
      lock (_positions)
      {
        var positions = new Dictionary<Windows.Window, Rectangle>();

        var desktoprectangle = Windows.Window.GetDesktop().GetInfo().WindowRectangle;
        _positions[desktoprectangle] = positions;

        //Trace.WriteLine(String.Format("Saving for {0}", desktoprectangle));

        foreach (Windows.Window w in Windows.EnumWindows())
        {
          Windows.Window.Placement placement = w.GetPlacement();
          Windows.Window.Info info = w.GetInfo();

          if (!IsInteresting(w, placement))
            continue;

          positions[w] = (info.WindowRectangle);
        }

        //foreach (var w in positions)
        //{
        //  if (w.Key.Text == "MainWindow")
        //    Describe(w.Key, w.Key.GetInfo(), w.Key.GetPlacement());
        //}
      }
    }

    private void RestoreWindows()
    {
      lock (_positions)
      {
        var desktoprectangle = Windows.Window.GetDesktop().GetInfo().WindowRectangle;

        if (!_positions.ContainsKey(desktoprectangle))
          return;

        var positions = _positions[desktoprectangle];

        Trace.WriteLine(String.Format("Restoring for {0}", desktoprectangle));
        //foreach (var w in positions)
        //{ if (w.Key.Text == "MainWindow")
        //    Trace.WriteLine(w.Value);
        //}

        foreach (var w in positions)
          try
          {
            w.Key.Position = w.Value;
          }
          catch (Exception)
          {
          }
      }
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
      if (null == _about)
      {
        _about = new AboutWindow();

        _about.Show();
        _about.Closed += (o, args) => _about = null;
      }
      else
      { _about.WindowState = WindowState.Normal;
        _about.Activate();
        _about.Focus();
      }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
      Close();
    }
  }
}