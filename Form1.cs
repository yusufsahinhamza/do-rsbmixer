using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace RSBMixer
{
    public partial class Form1 : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        private LowLevelKeyboardHook KeyboardHook = new LowLevelKeyboardHook();
        private Keys StartStopKey = Keys.A;
        private Keys MainLaserKey = Keys.D1;
        private Keys RSBKey = Keys.D2;

        private bool Active = false;

        public Form1()
        {
            InitializeComponent();

            if (!File.Exists("settings.xml"))
            {
                using (var fs = File.Create("settings.xml"))
                {
                    var xml = new XElement("Settings",
                        new XElement("StartStopKey", Keys.A.ToString()),
                        new XElement("MainLaserKey", Keys.D1.ToString()),
                        new XElement("RSBKey", Keys.D2.ToString())
                    );

                    var info = new UTF8Encoding(true).GetBytes(xml.ToString());
                    fs.Write(info, 0, info.Length);
                }
            }
            else
            {
                var xml = XDocument.Load("settings.xml");
                StartStopKey = (Keys)Enum.Parse(typeof(Keys), xml.Descendants().SingleOrDefault(x => x.Name.LocalName == "StartStopKey").Value, true);
                MainLaserKey = (Keys)Enum.Parse(typeof(Keys), xml.Descendants().SingleOrDefault(x => x.Name.LocalName == "MainLaserKey").Value, true);
                RSBKey = (Keys)Enum.Parse(typeof(Keys), xml.Descendants().SingleOrDefault(x => x.Name.LocalName == "RSBKey").Value, true);
                button2.Text = StartStopKey.ToString();
                button3.Text = MainLaserKey.ToString();
                button4.Text = RSBKey.ToString();
            }

            Task.Factory.StartNew(Tick);
            KeyboardHook.OnKeyPressed += KeydownEventHandler;
        }

        private void KeydownEventHandler(object sender, Keys key)
        {
            if (key == StartStopKey && !button2.Focused && !button3.Focused && !button4.Focused)
                button1.PerformClick();

            var settingsChanged = false;

            if (button3.Focused && key != RSBKey && key != StartStopKey)
            {
                button3.Text = key.ToString();
                MainLaserKey = key;
                settingsChanged = true;
            }
            else if (button4.Focused && key != MainLaserKey && key != StartStopKey)
            {
                button4.Text = key.ToString();
                RSBKey = key;
                settingsChanged = true;
            }
            else if (button2.Focused && key != RSBKey && key != MainLaserKey)
            {
                button2.Text = key.ToString();
                StartStopKey = key;
                settingsChanged = true;
            }

            if (settingsChanged)
            {
                var xml = new XElement("Settings",
                    new XElement("StartStopKey", StartStopKey),
                    new XElement("MainLaserKey", MainLaserKey),
                    new XElement("RSBKey", RSBKey)
                );

                xml.Save("settings.xml");
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            PressKey(MainLaserKey);

            Active = Active ? false : true;
            button1.Text = Active ? "Stop" : "Start";
            label4.Text = Active ? "Active" : "Passive";

            button2.Enabled = Active ? false : true;
            button3.Enabled = Active ? false : true;
            button4.Enabled = Active ? false : true;
            label1.Enabled = Active ? false : true;
            label2.Enabled = Active ? false : true;
            label3.Enabled = Active ? false : true;
        }

        private const int KEYEVENTF_EXTENDEDKEY = 1;
        private const int KEYEVENTF_KEYUP = 2;

        private void PressKey(Keys vKey)
        {
            keybd_event((byte)vKey, 0, KEYEVENTF_EXTENDEDKEY, 0);
            keybd_event((byte)vKey, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
        }

        private async void Tick()
        {
            while (true)
            {
                if (Active)
                {
                    PressKey(RSBKey);
                    await Task.Delay(150);
                    PressKey(MainLaserKey);
                    await Task.Delay(3000);
                }
            }
        }
    }
}
