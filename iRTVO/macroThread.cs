/*
 * macroThread.cs
 * 
 * Macro class:
 * 
 * Class that sends keystrokes to the sim.
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

// additional
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace iRTVO
{
    public class Macro
    {

        // macro
        const UInt32 WM_KEYDOWN = 0x0100;
        const UInt32 WM_KEYUP = 0x0101;

        const int keyShift = 0x10;
        const int scancodeShift = 0x36;

        const int keyLive = 0x61;
        const int scancodeLive = 0x4f;

        const int keyPlay = 0x65;
        const int scancodePlay = 0x4c;

        const int keyRewind = 0x25;
        const int scancodeRewind = 0x4b;

        const int delay = 70;

        Process[] processes;

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

        private void sendKey(int key, int scancode, int repeat, bool shift) {
            processes = Process.GetProcessesByName("iRacingSim");
            foreach (Process proc in processes)
            {
                if (shift)
                    PostMessage(proc.MainWindowHandle, WM_KEYDOWN, keyShift, 1 | scancodeShift << 16);

                for (int i = 0; i < repeat; i++)
                {
                    PostMessage(proc.MainWindowHandle, WM_KEYDOWN, key, 1 | scancode << 16);
                    PostMessage(proc.MainWindowHandle, WM_KEYUP, key, 1 | scancode << 16 | 1 << 30 | 1 << 24);
                    Thread.Sleep(delay);
                }

                if (shift)
                    PostMessage(proc.MainWindowHandle, WM_KEYUP, keyShift, 1 | scancodeShift << 16);
            }
        }

        public void rewind(object input)
        {
            SharedData.replayInProgress = true;
            SharedData.replayReady.Reset();
            SharedData.replayReady.WaitOne();
            int length = Int32.Parse(input.ToString());
            sendKey(keyRewind, scancodeRewind, 6, true);
            Thread.Sleep((int)((1000 * length) / 16) - delay*3);
            sendKey(keyPlay, scancodePlay, 2, false);
            SharedData.replayInProgress = false;
        }

        public void play()
        {
            sendKey(keyPlay, scancodePlay, 1, false);
        }

        public void live()
        {
            SharedData.replayInProgress = true;
            SharedData.replayReady.Reset();
            SharedData.replayReady.WaitOne();
            sendKey(keyPlay, scancodePlay, 1, false);
            Thread.Sleep(Properties.Settings.Default.ReplayMinLength);
            sendKey(keyLive, scancodeLive, 1, false);
            SharedData.replayInProgress = false;
        }
	}
}
