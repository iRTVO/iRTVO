using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
// additional
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using NLog;
using System.Diagnostics;
using NLog.Config;
using System.IO;
using NLog.Targets;
using System.Reflection;
using iRTVO.Data;

namespace iRTVO
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Logger logger = null;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        internal static extern bool IsDebuggerPresent();


        public static bool ErrorOccoured { get; set; }
        public static string LastError { get; set; } 
        public static bool SuppressErrors { get; private set; }
        public static bool ShowBorders { get;  set; }
        
        // debug
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Log the Exception and silently ignore it if no debugger is attached or explicitly rethrow is requested (-debug parameter)
            logger.Fatal(e.Exception.Message);
            logger.Fatal(e.Exception.ToString());
            ErrorOccoured = true;
            LastError = e.Exception.Message;

            if (e.Exception is OutOfMemoryException)
                throw e.Exception;

            if (!IsDebuggerPresent())
            {
                if (SuppressErrors)
                {
                    e.Handled = true;
                    return;
                }
            }
            
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            ConfigureLogging();
            if ( e.Args.Contains("-debug") )
                SuppressErrors = false;
            else
                SuppressErrors = true;
            ShowBorders = false;
            if (e.Args.Contains("-borders"))
                ShowBorders = true;

            logger.Info("iRTVO Build {0} startup", Assembly.GetEntryAssembly().GetName().Version);
            // Only bind if no debugger is attached or not in debug mode
            // else we wil not be able to step into problematic code
            if (!IsDebuggerPresent() && SuppressErrors)
            {
                DispatcherUnhandledException += Application_DispatcherUnhandledException;
            }

            try
            {
                // make sure our vital configuration is ok, else shutdown
                SharedData.settings = new Settings(Directory.GetCurrentDirectory() + "\\options.ini");                
            }
            catch (Exception ex)
            {
                logger.Fatal("Error while initializing: {0}", ex.ToString());
                MessageBox.Show("Error while initializing", "Fatal");
                this.Shutdown();
                return;
            }
            

        }

       

        private void ConfigureLogging()
        {
            // If there is no logging configuration file , use some default loggin for errors and stuff
            // Make sure the debug-Configuration is not part of the default distribution
            if ( !File.Exists(Path.Combine(Environment.CurrentDirectory,"nlog.config")))
            {
                LoggingConfiguration config = new LoggingConfiguration();
                FileTarget fileTarget = new FileTarget();
                config.AddTarget("file", fileTarget);
                fileTarget.FileName = "${basedir}/irtvo.log";                
                LoggingRule rule = new LoggingRule("*", LogLevel.Warn, fileTarget);
                config.LoggingRules.Add(rule);                
                LogManager.Configuration = config;
            }
            LogManager.EnableLogging();
            logger = LogManager.GetCurrentClassLogger();
        }
    }
}

// overlay click through
public static class WindowsServices
{
    const int WS_EX_TRANSPARENT = 0x00000020;
    const int GWL_EXSTYLE = (-20);

    [DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    public static void SetWindowExTransparent(IntPtr hwnd)
    {
        var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
    }
}