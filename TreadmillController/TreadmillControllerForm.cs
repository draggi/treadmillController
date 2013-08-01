using System;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TreadmillController
{
    public partial class TreadmillControllerForm : Form
    {
         //KeyboardHook hook = new KeyboardHook();

          SerialPort port = new SerialPort("COM5", 115200, Parity.None, 8, StopBits.One);
          string SpeedToRecover = "";
          // from wtsapi32.h
          private const int NotifyForThisSession = 0;

          // from winuser.h
          private const int SessionChangeMessage = 0x02B1;
          private const int SessionLockParam = 0x7;
          private const int SessionUnlockParam = 0x8;

          [DllImport("wtsapi32.dll")]
          private static extern bool WTSRegisterSessionNotification(IntPtr hWnd, int dwFlags);

          [DllImport("wtsapi32.dll")]
          private static extern bool WTSUnRegisterSessionNotification(IntPtr hWnd);

          // flag to indicate if we've registered for notifications or not
          private bool registered = false;

          /// <summary>
          /// Is this form receiving lock / unlock notifications
          /// </summary>
          protected bool ReceivingLockNotifications
          {
              get { return registered; }
          }

          //For timer tick
          private int _ticks;
        public TreadmillControllerForm()
        {
            InitializeComponent();
              // For detecting hotkey

            // Set up the TrackBar.            
            trackBar1.Scroll += new EventHandler(trackBar1_Scroll);
            trackBar1.Minimum = 8;
            trackBar1.Maximum = 30;
            trackBar1.TickFrequency = 2;
            trackBar1.LargeChange = 3;
            trackBar1.SmallChange = 1;
            
        }

          private void TreadmillControllerForm_Load(object sender, EventArgs e)
        {
            MphInfo.Text = "0";
            KmInfo.Text = "0";
            port.DtrEnable = false;
            port.ReadBufferSize = 100;
        }

        private void Form1_Closing(object sender, EventArgs e)
        {      
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            // Display the trackbar value in the text box.
            //label3.Text = "" + (trackBar1.Value.ToString());
            SendSpeedToTreadmill((trackBar1.Value*0.2).ToString());
        }
        private string ReturnRatioForSelectedMphSpeed(string mph)
        {
            string RatioForThisSpeed="";
            double calc = (-0.0620747 * Convert.ToDouble(mph) + 0.92);
            RatioForThisSpeed = calc.ToString();
            RatioForThisSpeed = RatioForThisSpeed.Replace(",", ".");                       
            //label3.Text = RatioForThisSpeed;
            if (calc >= 0.82) RatioForThisSpeed = "1";
            return RatioForThisSpeed;
        }

        private void SendSpeedToTreadmill(string value)
        {                        
            port.Open();
            string read;
            port.Write("\n");
            //label3.Text=ReturnRatioForSelectedMphSpeed(value).ToString();
            port.Write(ReturnRatioForSelectedMphSpeed(value));
            port.Write("\n");
            //start timer:
            timer1.Start();
            read = port.ReadLine();
            MphInfo.Text = ReturnTrueSpeed(value,"mph");
            KmInfo.Text = ReturnTrueSpeed(value, "km");
            //label3.Text = read;
            port.Close();
        }

        private void SendStopToTreadMill()
        {
            port.Open();
            port.Write("\n");
            port.Write("1");
            
            //Stop the timer:
            timer1.Stop();

            trackBar1.Value = 8;
            MphInfo.Text = "0";
            KmInfo.Text = "0";
            port.Write("\n");
            port.Close();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {

        }

        public string ReturnTrueSpeed(string value, string type)
        {
            string ret = "";
            double c=Convert.ToDouble(value);
            if (type == "mph" & (Convert.ToDouble(value)>1.6))
            {
                ret = (c - 1).ToString();
            }
            else if (Convert.ToDouble(value) > 1.6 )
            {
                ret = ((c - 1) * 1.6).ToString();
            }
            else
            {
                ret = "0";
            }
            return ret;
        }
        
        private void Stop_Click(object sender, EventArgs e)
        {            
            SendStopToTreadMill();
        }

        protected virtual void OnSessionLock()
        {
            //AddLog("Session locked");
            SpeedToRecover = trackBar1.Value.ToString();
            SendStopToTreadMill();
        }


        protected override void WndProc(ref Message m)
        {
            // check for session change notifications
            if (m.Msg == SessionChangeMessage)
            {
                if (m.WParam.ToInt32() == SessionLockParam)
                    OnSessionLock();
                else if (m.WParam.ToInt32() == SessionUnlockParam)
                    OnSessionUnlock();
            }

            base.WndProc(ref m);
            return;
        }

        /// <summary>
        /// The windows session has been unlocked
        /// </summary>
        protected virtual void OnSessionUnlock()
        {
            trackBar1.Value=Convert.ToInt16(SpeedToRecover);
            SendSpeedToTreadmill((Convert.ToInt16(SpeedToRecover) * 0.2).ToString());        
        }

        /// <summary>
        /// Register for event notifications
        /// </summary>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // WtsRegisterSessionNotification requires Windows XP or higher
            bool haveXp = Environment.OSVersion.Platform == PlatformID.Win32NT &&
                                (Environment.OSVersion.Version.Major > 5 ||
                                    (Environment.OSVersion.Version.Major == 5 &&
                                     Environment.OSVersion.Version.Minor >= 1));
            if (haveXp)
                registered = WTSRegisterSessionNotification(Handle, NotifyForThisSession);            
            return;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            _ticks++;
            this.Text = _ticks.ToString();
        }


    }
}
