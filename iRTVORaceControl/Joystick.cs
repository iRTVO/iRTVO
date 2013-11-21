using Microsoft.DirectX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRTVORaceControl
{
    public class Joystick
    {
        private Device joystickDevice;
        private JoystickState state;

        //Properties
        public Boolean JoystickActiv = false;
        public String Name;
        public Int32 Axes;
        public Int32 Buttons;
        public Int32 Views;
        public Int32 X;
        public Int32 Y;
        public Int32 Z;
        public Int32 Rz;
        public Int32 Push;
        public Dictionary<Int32, Boolean> ButtonList;

        /*
                public Int32 Speed
                {
                    get
                    {
                        return Time.Interval;
                    }
                    set
                    {
                        Time.Interval = value;
                    }
                }

                */
        public Joystick(Guid id)
        {
            desiredId = id;
            FindDevice();

            /*
            Form = f;



            //Define Timer Ticks
            Time.Interval = 50;
            Time.Tick += new EventHandler(Tick);

           

            //If wanted Start finder
            if (ActivateFinder)
            {
                this.ActivateFinder();
            }
             * */
        }

        /*public Joystick(System.Windows.Forms.Form f, Boolean ActivateFinder)
        {
            Form = f;



            //Define Timer Ticks
            Time.Interval = 50;
            Time.Tick += new EventHandler(Tick);


            FindDevice();

            //If wanted Start finder
            if (ActivateFinder)
            {
                this.ActivateFinder();
            }
        }*/

        //activate Finder

        public void ActivateFinder()
        {
            /*
            Finder.Interval = 100;
            Finder.Tick += new EventHandler(Finder_Tick);
            Finder.Enabled = true;
             * */
        }

        public void DisableFinder()
        {
            // Finder.Enabled = false;
        }

        void Finder_Tick(object sender, EventArgs e)//Finder methode
        {
            if (!JoystickActiv)
                FindDevice();

        }

        public Dictionary<Guid, string> Joysticks = new Dictionary<Guid, string>();
        //private bool byId = false;
        private Guid desiredId = Guid.Empty;
        private Guid id = Guid.Empty;

        public static Dictionary<Guid, string> GetAllJoysticks()
        {
            Dictionary<Guid, string> localJoysticks = new Dictionary<Guid, string>();
            DeviceList gameControllerList = Manager.GetDevices(DeviceClass.GameControl, EnumDevicesFlags.AttachedOnly);


            if (gameControllerList.Count > 0)//is there at least one device
            {

                for (Int32 i = 1; i <= gameControllerList.Count; i++)//run trough devices
                {
                    gameControllerList.MoveNext();//choose next device
                    DeviceInstance deviceInstance = (DeviceInstance)gameControllerList.Current;//create deviceinstance
                    localJoysticks.Add(deviceInstance.ProductGuid, deviceInstance.ProductName);
                }
            }
            return localJoysticks;
        }

        public void FindDevices()
        {
            Joysticks = new Dictionary<Guid, string>();
            DeviceList gameControllerList = Manager.GetDevices(DeviceClass.GameControl, EnumDevicesFlags.AttachedOnly);


            if (gameControllerList.Count > 0)//is there at least one device
            {

                for (Int32 i = 1; i <= gameControllerList.Count; i++)//run trough devices
                {
                    gameControllerList.MoveNext();//choose next device
                    DeviceInstance deviceInstance = (DeviceInstance)gameControllerList.Current;//create deviceinstance
                    Joysticks.Add(deviceInstance.ProductGuid, deviceInstance.ProductName);
                }
            }
        }

        public void FindDevice()
        {
            //Joysticks = new Dictionary<Guid, string>();
            JoystickActiv = false;


            // find all connected devices


            DeviceList gameControllerList = Manager.GetDevices(DeviceClass.GameControl, EnumDevicesFlags.AttachedOnly);


            if (gameControllerList.Count > 0)//is there at least one device
            {

                for (Int32 i = 1; i <= gameControllerList.Count; i++)//run trough devices
                {
                    gameControllerList.MoveNext();//choose next device
                    DeviceInstance deviceInstance = (DeviceInstance)gameControllerList.Current;//create deviceinstance
                    //Joysticks.Add(deviceInstance.InstanceGuid, deviceInstance.ProductName);
                    if ((deviceInstance.DeviceType == DeviceType.Joystick) || (deviceInstance.DeviceType == DeviceType.Gamepad)) //is the selected device a joystick
                    {
                        if (!desiredId.Equals(Guid.Empty))
                        {
                            if (deviceInstance.ProductGuid.Equals(desiredId))
                            {
                                joystickDevice = new Device(deviceInstance.InstanceGuid);//create joystick device
                                this.id = deviceInstance.ProductGuid;
                                i = gameControllerList.Count + 1;//leave loop
                            }
                        }
                        else
                        {
                            joystickDevice = new Device(deviceInstance.InstanceGuid);//create joystick device
                            this.id = deviceInstance.ProductGuid;
                            i = gameControllerList.Count + 1;//leave loop
                        }
                    }
                }

                if (joystickDevice == null)//no joystick found
                {
                    if (DeviceChanged != null)
                    {
                        DeviceChanged.Invoke(false, this);//Message. no joystick found
                    }
                }
                else //Wenn Joystick vorhanden
                {
                    // HMMMMMM joystickDevice.SetCooperativeLevel(Form, CooperativeLevelFlags.Background | CooperativeLevelFlags.NonExclusive); //define interaction

                    //joystickDevice.SetDataFormat(DeviceDataFormat.Joystick);//define that device is a joystick

                    joystickDevice.Acquire();//make free

                    //Referenz auch "Eigenschaften" in einer variable speichern
                    DeviceCaps cps = joystickDevice.Caps;
                    //Axis Number
                    Axes = cps.NumberAxes;
                    //Number of Buttons
                    Buttons = cps.NumberButtons;
                    //Views Number
                    Views = cps.NumberPointOfViews;

                    //Joystickname
                    Name = joystickDevice.DeviceInformation.ProductName;

                    //Timer start
                    // Time.Enabled = true;

                    JoystickActiv = true;

                    if (DeviceChanged != null)
                    {
                        DeviceChanged.Invoke(true, this);
                    }
                }
            }
            else
            {
                if (DeviceChanged != null)
                {
                    DeviceChanged.Invoke(false, this);
                }
            }
        }

        //Description
        public override String ToString()
        {
            if (!JoystickActiv)
            {
                return "No joystick connected!";
            }
            else
            {
                //Poll();                //Nicht immer sinnvoll und
                //OrderData();     //kann zu Problemen führen
                String Data;
                Data = "Name: " + Name + Environment.NewLine;
                Data += "Axis: " + Axes + Environment.NewLine;
                Data += "Buttons: " + Buttons + Environment.NewLine;
                Data += "Views: " + Views;
                if (X != 0 || Y != 0 || Rz != 0 || Z != 0)
                {
                    Data += Environment.NewLine;
                    Data += "X-Axis: " + X.ToString() + Environment.NewLine;
                    Data += "Y-Axis: " + Y.ToString() + Environment.NewLine;
                    Data += "Z-Axis: " + Z.ToString() + Environment.NewLine;
                    Data += "Rz-Axis: " + Rz.ToString();
                }
                return Data;
            }
        }

        private void Poll()
        {
            try
            {
                //Pollen
                joystickDevice.Poll();

                //Status refresh
                state = joystickDevice.CurrentJoystickState;
            }
            catch
            {
                //lost joystick connection
                JoystickActiv = false;
                if (DeviceChanged != null)
                {
                    DeviceChanged.Invoke(false, this);
                }
            }
        }

        private void OrderData()
        {
            //Buttons
            byte[] bButtons = state.GetButtons();
            Dictionary<Int32, Boolean> LButtons = new Dictionary<int, bool>();
            for (Int32 i = 0; i < Buttons; i++)
            {
                if (bButtons[i] >= 128)
                    LButtons.Add(i, true);
                else
                    LButtons.Add(i, false);
            }




            if (ButtonList != null && (ButtonUp != null || ButtonDown != null))
            {
                foreach (KeyValuePair<Int32, Boolean> B in LButtons)
                {
                    if (B.Value != ButtonList[B.Key])
                    {
                        if (B.Value == true)
                        {
                            if (ButtonDown != null)
                            {
                                ButtonDown.Invoke(B.Key, this);
                            }
                        }
                        else
                        {
                            if (ButtonUp != null)
                            {
                                ButtonUp.Invoke(B.Key, this);
                            }
                        }
                    }
                }
            }

            ButtonList = LButtons;

            //Axis
            X = state.X;
            Y = state.Y;
            Z = state.Z;
            Rz = state.Rz;

            if (AxeChanged != null)
            {
                AxeChanged.Invoke(X, Y, Z, Rz, this);
            }

            //Shift
            try
            {
                Int32 P = state.GetSlider()[0];
                if (PushChange != null && P != Push)
                {
                    PushChange.Invoke(P, this);
                }
                Push = P;
            }
            catch { }

            //Get Data
            if (GetData != null)
            {
                GetData.Invoke(this);
            }
        }

        //Events
        public event OnButtonDown ButtonDown;
        public event OnButtonUp ButtonUp;
        public event OnAxeChanged AxeChanged;
        public event OnGetData GetData;
        public event OnPushChange PushChange;
        public event OnDeviceChanged DeviceChanged;

        public void Tick(object sender, EventArgs e)
        {
            Poll();

            OrderData();

        }
    }

    public delegate void OnDeviceChanged(Boolean Found, Joystick joystick);
    public delegate void OnGetData(Joystick joystick);
    public delegate void OnButtonDown(Int32 ButtonNumber, Joystick joystick);
    public delegate void OnButtonUp(Int32 ButtonNumber, Joystick joystick);
    public delegate void OnAxeChanged(Int32 X, Int32 Y, Int32 Z, Int32 Rz, Joystick joystick);
    public delegate void OnPushChange(Int32 Push, Joystick joystick);
}