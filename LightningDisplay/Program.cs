using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation.Media;
using System.IO.Ports;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Touch;
using Microsoft.SPOT.Net;
using System.Xml;
using System.IO;
using GHI.Glide;
using GHI.Glide.Display;
using GHI.Glide.UI;
using GHI.IO;
using GHI.Pins;
using GHI.Processor;
using GHI.Networking;
using GHI.IO.Storage;
using Microsoft.SPOT.IO;
using Microsoft.SPOT.Net.NetworkInformation;
using System.Net;
using System.Text;
using System.Net.Sockets;
using MFToolkit.IO;
using MFToolkit.Net.XBee;
using GHI.Utilities;

using GlideButton = GHI.Glide.UI.Button;
using GlideColors = GHI.Glide.Colors;
using System.Collections;

using IndianaJones.NETMF.Json;

using Axon.LEDDisplay;

namespace LightningDisplay
{
    public class Program
    {
        static Object displayLock = new object();

        static String SysVersion = "1.0.0";
        static Window CurrentWindow;
        static int LastWindow = -1;

        static Bitmap radio0;
        static Bitmap radio1;
        static Bitmap radio2;

        static Bitmap wifiState0;
        static Bitmap wifiState1;
        static Bitmap wifiState2;
        static Bitmap wifiState3;
        static Bitmap wifiState4;
        static Bitmap wifiState5;

        static Bitmap ledDigitsBitmap;
        static Bitmap ledDecimalBitmap;
        static Bitmap ledDigitsSMBitmap;
        static Bitmap ledDecimalSMBitmap;
        static Bitmap lightningOnBitmap;
        static Bitmap lightningOffBitmap;
        
        const int MAIN_WINDOW = 1;
        const int MENU_WINDOW = 2;
        const int WIFI_WINDOW = 3;
        const int NETKEY_WINDOW = 4;
        const int EMAIL_WINDOW = 5;
        const int ENVIRO_WINDOW = 6;
        //
        // Main display
        //
        static Image RadioLinkState;
        static Image DistanceImage;
        static Image EnergyImage;
        static Image LightningImage;
        static TextBlock StatusTextBlock;
        static Image WifiStateImage;

        static LEDdisplay distanceLED;
        static LEDdisplay energyLED;
        //
        // Enviro window
        //
        static Image TempInImage;
        static Image HumidityInImage;
        static Image TempOutImage;
        static Image HumidityOutImage;
        static Image AirQualImage;
        static Image BarometerImage;
        static Image TempIn2Image;
        static Image TempIn3Image;
        static Image TempKitchenImage;
        static Image WattageImage;
        static Image CurrentImage;
        static Image FridgeTempImage;

        static TextBlock dateTimeTextBlock;

        static LEDdisplay TempInLED;
        static LEDdisplay HumidityInLED;
        static LEDdisplay TempOutLED;
        static LEDdisplay HumidityOutLED;
        static LEDdisplay AirQualLED;
        static LEDdisplay BarometerLED;
        static LEDdisplay TempIn2LED;
        static LEDdisplay TempIn3LED;
        static LEDdisplay TempKitchenLED;
        static LEDdisplay WattageLED;
        static LEDdisplay CurrentLED;
        static LEDdisplay FridgeTempLED;
        //
        // Wifi selection
        //
        static DataGrid WifiList;
        static GlideButton wifiConnectButton;
        static GlideButton wifiCancelButton;
        // 
        // Wifi key
        //
        static TextBox WifiKeyText;
        static GlideButton keyOkButton;
        static GlideButton keyCancelButton;
        //
        // System Settings
        //
        static TextBox SSIDTextBox;
        //
        // Email
        //
        static TextBox SMTPServerTextBox;
        static TextBox SMTPPortTextBox;
        static TextBox SMTPUserTextBox;
        static TextBox SMTPPasswordTextBox;
        static TextBox SMTPEmailTextBox;
        //
        // Bitmaps for the display
        //
        struct _ClockTime
        {
            public int Day;
            public int Mon;
            public int Year;
            public int Hour;
            public int Min;
            public int Sec;
        };
        static _ClockTime ClockTime = new _ClockTime();

        struct _ConfigData
        {
            public String ServerIP;
            public int ServerPort;
            public String WifiID;
            public String WifiPassword;
            public double TimeZoneOffset;
            public String EmailUser;
            public String EmailPassword;
            public String EmailServer;
            public String EmailSender;
            public int EmailPort;
            public String EmailToAddress;
        };
        static SmtpClient mySmtp;

        static _ConfigData ConfigData = new _ConfigData();

        struct _LightningData
        {
            public bool NewReading;
            public bool NewScada;
            public int Distance;
            public int Energy;
            public bool FlashLightning;
            public DateTime LastStrikeTime;   // If zero, no strikes for this day
        };
        static _LightningData LightningData = new _LightningData();

        struct _Environment
        {
            public double TempOut;
            public double HumidityOut;
            public double Barometer;
            public double TempIn;
            public double HumidityIn;
            public double TempIn2;
            public double TempIn3;
            public double TempKitchen;
            public double FridgeTemp;
            public double Wattage;
            public double Current;
            public double AirQual;
        }
        static _Environment Environment = new _Environment();
        //
        // Xbee stuff
        //
        static String XbeeMessage;
        static String[] XbeeStripped;
        static ZNetRxResponse who;
        static AtCommandResponse responce;
        static ulong lightningNode = 0x13A200408D94BA;

        static bool XbeeRescanNodes = false;
        static bool XbeeSendNodeNames = false;

        static bool XbeeBusy = false;

        static CapTouchDriver CapDriver;

        static I2CDevice i2cDevice;
        
        static Boolean DrawingActive = false;           // Set when updating the main display
        static long msDisplayTimeout;
        static long msShowLightning;

        struct _Radio
        {
            public bool LinkActive;         // Set to true when Zigbee module detected
            public int RSSI;                // Radio link signal strength
            public int MyAddress;           // My radio address
            public int DestAddress;         // Desintation radio address
            public int RadioCount;          // Number of well detected by the system
        }
        static _Radio Radio = new _Radio();
        static XBee xbee;

        static WiFiRS9110 wifi;
        static WiFiRS9110.NetworkParameters[] scanResults;

        struct _Wifi
        {
            public int WifiSelection;
            public int WifiIndex;
            public bool WindowShowWiFi;
            public _WifiStates WifiState;
            public bool WifiScanStart;      // Set when we want to start a network scan
            public int WifiSignalStrength;  
        }
        static _Wifi wifiState = new _Wifi();

        static string[] SecurityModes = new string[4] { "Open", "WPA", "WPA2", "WEP" };

        enum _WifiStates
        {
            WIFI_STANDBY,                   // Standby, not connected
            WIFI_SCANNING,                  // Scanning for access point
            WIFI_CONNECTED,                 // Connected to Access point
        }

        static OutputPort buzzer = new OutputPort(GHI.Pins.G120.P2_21, false);
        static bool MakeSound = false;
        static bool HaltSound = false;
        static long msHaltSoundTime;
        //
        // Voltage, Current, Watts, Fridge U, Fridge L, Front Room Temp,
        // Bedroom Temp, Kitchen Temp, Dust Ratio
        //
        static int[] deviceList = new int[] { 24, 22, 23, 21, 20, 28, 29, 32, 30, 34 };
        static long msRequestStatusTime;

        //*******************************************************************
        /// <summary>
        /// Main
        /// </summary>
        /// 
        public static void Main()
        {
            long msTime;

            InitSystem();
            InitHardware();

            // OVERCLOCK G120 EXTERNAL MEMORY CONTROLLER
            Register EMCCLKSEL = new Register(0x400FC100);
            EMCCLKSEL.ClearBits(1 << 0); // OVERDRIVE

            Glide.FitToScreen = true;

            i2cDevice = new I2CDevice(new I2CDevice.Configuration(0x38, 400));

            CapDriver = new CapTouchDriver(i2cDevice);
            CapDriver.SetBacklightTime(0);
            CapDriver.ResetBacklight();

            LoadSettings();
            //
            // Set NETMF time from RTC. Must do here before the timers are initialised
            //
            Utility.SetLocalTime(RealTimeClock.GetDateTime());

            SetActiveWin(ENVIRO_WINDOW);
            //
            // Create a thread to handle the Zigbee Radio
            //
            Thread xbeeHandler = new Thread(xbeeRadioHandler);
            Debug.Print("XBEE thread = " + xbeeHandler.ManagedThreadId.ToString());
            xbeeHandler.Start();

            Thread wifiThread = new Thread(wifiHandler);
            wifiThread.Priority = ThreadPriority.Normal;
            Debug.Print("WIFI THREADID = " + wifiThread.ManagedThreadId.ToString());
            wifiThread.Start();

            Thread soundThread = new Thread(SoundHandler);
            soundThread.Start();

            msShowLightning = UtilsClass.msTime();

            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;

            LightningData.NewScada = true;              // This will reset it to ZERO on startup

            msRequestStatusTime = UtilsClass.msTime();

            while (true)
            {
                msTime = UtilsClass.msTime();

                if (wifiState.WindowShowWiFi)             // WiFi Selection window?
                {
                    wifiState.WindowShowWiFi = false;
                    SetActiveWin(WIFI_WINDOW);
                }
                try                                 // Might fail if the graphics are not ready yet
                {
                    lock (displayLock)
                    {
                        UpdateDisplay();
                    }
                }
                catch (Exception e)
                {
                    Debug.Print("Update Display Error: " + e.Message);
                    DrawingActive = false;
                }
                if (msDisplayTimeout == 0)
                {
                    SetDisplayTimeout(10 * 60);     // 10 mins
                }
                if(LightningData.NewReading)        // If we detect any lightning, switch on the backlight
                {
                    MakeSound = true;

                    LightningData.NewReading = false;
                    LightningData.FlashLightning = true;

                    SetActiveWin(MAIN_WINDOW);      // Show lightning window

                    CapDriver.SwithcBacklightOn();
                }
                if (Glide.MainWindow != null)
                {
                    if (Glide.MainWindow.Name != "EnviroWindow")
                    {
                        //
                        // We must be on another screen so check the time has not expired.
                        // Don't do this check on the calwindow
                        //
                        if (msTime > msDisplayTimeout)
                        {
                            //
                            // Switch back to the main display
                            //
                            SetActiveWin(ENVIRO_WINDOW);
                        }
                    }
                }
                if (msDisplayTimeout == 0)
                {
                    SetDisplayTimeout(10 * 60);
                }
                Thread.Sleep(250);
            }
        }

        static void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
        {
            
        }

        static void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            
        }

        //*******************************************************************
        //
        // Close the current window and open a new one
        //
        //*******************************************************************

        static void SetActiveWin(int OpenWho)
        {
            int index;
            LastWindow = OpenWho;

            CapDriver.ResetBacklight();
            SetDisplayTimeout(10 * 60);

            lock (displayLock)
            {
                if (Glide.MainWindow != null)
                {
                    Glide.MainWindow.IgnoreEvents();

                    index = Glide.MainWindow.NumChildren - 1;
                    while (Glide.MainWindow.NumChildren > 0)
                    {
                        Glide.MainWindow.RemoveChildAt(index--);
                    }
                    Glide.MainWindow.Dispose();
                }
                Glide.MainWindow = null;        // Disable it before we update

                switch (OpenWho)
                {
                    case MAIN_WINDOW:
                        InitBitmaps();
                        InitMainWin();
                        SetDisplayTimeout(30);  // Set display for 30 seconds just enough time to show the lightning
                        break;
                    case MENU_WINDOW:
                        InitMenuWin();
                        break;
                    case NETKEY_WINDOW:
                        InitNetKeyWin();
                        break;
                    case WIFI_WINDOW:
                        InitWifiWin();
                        break;
                    case EMAIL_WINDOW:
                        InitSettingsWin();
                        break;
                    case ENVIRO_WINDOW:
                        InitBitmaps();
                        InitEnviroWin();
                        break;
                    default:
                        InitBitmaps();
                        InitMainWin();
                        break;
                }
                Glide.MainWindow = CurrentWindow;
                //
                // Now we need to update any radio buttons as mainwindow is now present
                //
                if (OpenWho == MENU_WINDOW)
                {
                }
            }
        }

        //*******************************************************************
        //
        // Init the bitmaps for the system
        //
        //*******************************************************************

        static void InitBitmaps()
        {
            //
            // Create all the bitmaps
            //
            radio0 = new Bitmap(Resources.GetBytes(Resources.BinaryResources.LEDred), Bitmap.BitmapImageType.Jpeg);
            radio1 = new Bitmap(Resources.GetBytes(Resources.BinaryResources.LEDorange), Bitmap.BitmapImageType.Jpeg);
            radio2 = new Bitmap(Resources.GetBytes(Resources.BinaryResources.LEDgreen), Bitmap.BitmapImageType.Jpeg);

            ledDigitsBitmap = new Bitmap(Resources.GetBytes(Resources.BinaryResources.LEDDigits), Bitmap.BitmapImageType.Jpeg);
            ledDecimalBitmap = new Bitmap(Resources.GetBytes(Resources.BinaryResources.LEDdecimal), Bitmap.BitmapImageType.Jpeg);

            ledDigitsSMBitmap = new Bitmap(Resources.GetBytes(Resources.BinaryResources.LEDDigitsGreen_sm), Bitmap.BitmapImageType.Jpeg);
            ledDecimalSMBitmap = new Bitmap(Resources.GetBytes(Resources.BinaryResources.LEDdecimalGreen_sm), Bitmap.BitmapImageType.Jpeg);

            lightningOnBitmap = new Bitmap(Resources.GetBytes(Resources.BinaryResources.LightningOn), Bitmap.BitmapImageType.Jpeg);
            lightningOffBitmap = new Bitmap(Resources.GetBytes(Resources.BinaryResources.LightningOff), Bitmap.BitmapImageType.Jpeg);

            wifiState0 = new Bitmap(Resources.GetBytes(Resources.BinaryResources.wifi0), Bitmap.BitmapImageType.Jpeg);
            wifiState1 = new Bitmap(Resources.GetBytes(Resources.BinaryResources.wifi1), Bitmap.BitmapImageType.Jpeg);
            wifiState2 = new Bitmap(Resources.GetBytes(Resources.BinaryResources.wifi2), Bitmap.BitmapImageType.Jpeg);
            wifiState3 = new Bitmap(Resources.GetBytes(Resources.BinaryResources.wifi3), Bitmap.BitmapImageType.Jpeg);
            wifiState4 = new Bitmap(Resources.GetBytes(Resources.BinaryResources.wifi4), Bitmap.BitmapImageType.Jpeg);
            wifiState5 = new Bitmap(Resources.GetBytes(Resources.BinaryResources.wifi5), Bitmap.BitmapImageType.Jpeg);
        }

        //*******************************************************************
        //
        // Initialises the LCD based on what hardware we are running on
        //
        //*******************************************************************

        private static void InitHardware()
        {
            //
            // Because we might reflash, we have reprogramme the Display Driver
            // as this will be wiped out when we reflash the firmware.
            //
            Display.Width = 800;
            Display.Height = 480;
            Display.HorizontalSyncPulseWidth = 1;
            Display.HorizontalBackPorch = 88;
            Display.HorizontalFrontPorch = 40;
            Display.VerticalSyncPulseWidth = 3;
            Display.VerticalBackPorch = 32;
            Display.VerticalFrontPorch = 13;
            Display.PixelClockRateKHz = 25000;
            Display.OutputEnableIsFixed = true;
            Display.OutputEnablePolarity = true;
            Display.HorizontalSyncPolarity = false;
            Display.VerticalSyncPolarity = false;
            Display.PixelPolarity = true;
            Display.Type = Display.DisplayType.Lcd;

            if (Display.Save())      // Reboot required?
            {
                PowerState.RebootDevice(false);
            }
        }

        //*******************************************************************
        //
        // Save settings to the INI file
        //
        //*******************************************************************

        static void SaveSettings()
        {
            if (UtilsClass.ini == null)
                return;

            UtilsClass.ini.DeleteSection("CONFIG");

            UtilsClass.ini.SetSettingValue("CONFIG", "WIFIID", ConfigData.WifiID);
            UtilsClass.ini.SetSettingValue("CONFIG", "WIFIPW", ConfigData.WifiPassword);
            UtilsClass.ini.SetSettingValue("CONFIG", "SERVERIP", ConfigData.ServerIP);
            UtilsClass.ini.SetSettingValue("CONFIG", "SERVERPORT", ConfigData.ServerPort.ToString());

            UtilsClass.ini.SetSettingValue("EMAIL", "SERVER", ConfigData.EmailServer);
            UtilsClass.ini.SetSettingValue("EMAIL", "PORT", ConfigData.EmailPort.ToString());
            UtilsClass.ini.SetSettingValue("EMAIL", "USER", ConfigData.EmailUser);
            UtilsClass.ini.SetSettingValue("EMAIL", "PASSWORD", ConfigData.EmailPassword);
            UtilsClass.ini.SetSettingValue("EMAIL", "ADDRESS", ConfigData.EmailToAddress);

            UtilsClass.saveINI();
        }

        //*******************************************************************
        //
        // Load settings from the INI file
        //
        //*******************************************************************

        static void LoadSettings()
        {
            if (UtilsClass.ini == null)
            {
                ConfigData.WifiID = "";
                ConfigData.WifiPassword = "";
                ConfigData.ServerIP = "";
                ConfigData.ServerPort = 5000;
                return;
            }
            ConfigData.WifiID = UtilsClass.ini.GetSettingValue("CONFIG", "WIFIID", "");
            ConfigData.WifiPassword = UtilsClass.ini.GetSettingValue("CONFIG", "WIFIPW", "");
            ConfigData.ServerIP = UtilsClass.ini.GetSettingValue("CONFIG", "SERVERIP", "");
            ConfigData.ServerPort = Int16.Parse(UtilsClass.ini.GetSettingValue("CONFIG", "SERVERPORT", "5000"));

            ConfigData.EmailServer = UtilsClass.ini.GetSettingValue("EMAIL", "SERVER", "");
            ConfigData.EmailPort = Int16.Parse(UtilsClass.ini.GetSettingValue("EMAIL", "PORT", "25"));
            ConfigData.EmailUser = UtilsClass.ini.GetSettingValue("EMAIL", "USER", "");
            ConfigData.EmailPassword = UtilsClass.ini.GetSettingValue("EMAIL", "PASSWORD", "");
            ConfigData.EmailToAddress = UtilsClass.ini.GetSettingValue("EMAIL", "ADDRESS", "");

            ConfigData.EmailSender = "environment@embeddedcomputer.co.uk";

            ConfigData.TimeZoneOffset = 7;
        }

        //*******************************************************************
        //
        // Zigbee Radio handler thread
        //
        //*******************************************************************

        private static void xbeeRadioHandler()
        {
            long msTime;
            XBeeResponse reply;

            xbee = new XBee("COM1", 9600, ApiType.Enabled);
            xbee.FrameReceived += new FrameReceivedEventHandler(xbee_FrameReceived);
            xbee.LogEvent += xbee_LogEvent;
            xbee.ModemStatusChanged += xbee_ModemStatusChanged;
            xbee.Open();
            //
            // Try a node discovery
            //
            AtCommand cmd = new NodeDiscoverCommand();

            try
            {
                reply = xbee.Execute(cmd, 10000);     // Check for presence
            }
            catch (Exception error)
            {
                Debug.Print("XBEE : " + error.Message);
                Radio.LinkActive = false;
            }
            msTime = UtilsClass.msTime();

            if(lightningNode > 0)
            {
                sendXbee("LIGHTNING,SETCOUNT5", lightningNode);
            }
            while (true)
            {
                msTime = UtilsClass.msTime();

                if (XbeeRescanNodes)
                {
                    XbeeRescanNodes = false;

                    //
                    // Try a node discovery
                    //
                    cmd = new NodeDiscoverCommand();

                    CheckXbeeBusy();
                    try
                    {
                        reply = xbee.Execute(cmd, 10000);     // Check for presence
                    }
                    catch (Exception)
                    {
                        Radio.LinkActive = false;
                        return;
                    }
                }
                if (XbeeSendNodeNames)          // Send all the nodes their identifiers
                {
                    XbeeSendNodeNames = false;
                }
                Thread.Sleep(100);
            }
        }

        static void sendXbee(String packet, ulong address)
        {
            XBeeResponse reply;

            XBeeRequest req = new ZNetTxRequest(new XBeeAddress64(address), new XBeeAddress16(0xFFFE), Encoding.UTF8.GetBytes(packet));
            try
            {
                reply = xbee.Execute(req, 2000);
            }
            catch (Exception)
            {
            }

        }

        static void xbee_ModemStatusChanged(object sender, ModemStatusChangedEventArgs e)
        {
            if(e.Status == ModemStatusType.Disassociated)
            {

            }
        }

        static void xbee_LogEvent(object sender, LogEventArgs e)
        {
        }

        //*******************************************************************
        //
        // Checks to see if Xbee receiver is busy and waits
        //
        //*******************************************************************

        static void CheckXbeeBusy()
        {
            while (XbeeBusy)
            {
                Thread.Sleep(10);
            }
        }

        //*******************************************************************
        //
        // Zigbee reception
        //
        //*******************************************************************

        static void xbee_FrameReceived(object sender, FrameReceivedEventArgs e)
        {
            XbeeBusy = true;

            NodeDiscover nd = null;

            //            Debug.Print("received a packet: " + e.Response);

            if (e.Response.ApiID == XBeeApiType.AtCommandResponse)
            {
                AtCommandResponse response = e.Response as AtCommandResponse;

                if (response.Command == "ND")
                {
                    try
                    {
                        nd = NodeDiscover.Parse((e.Response as AtCommandResponse));

                        if(nd.NodeIdentifier == "LIGHTNING1")
                        {
                            lightningNode = nd.SerialNumber.Value;
                        }
                    }
                    catch
                    {
                        nd = null;
                    }
                    if (nd != null && nd.ShortAddress != null)
                    {
                        Debug.Print(nd.SerialNumber.Value.ToString());

                        Radio.RSSI = nd.SignalStrength;
                    }
                }
            }
            else
            {
                if (e.Response.ApiID == XBeeApiType.ZNetRxPacket)
                {
                    try
                    {
                        who = e.Response as ZNetRxResponse;
                        ulong from = who.SerialNumber.Value;

                        XbeeMessage = new String(System.Text.Encoding.UTF8.GetChars(who.Value));

                        XbeeStripped = XbeeMessage.Split(',');

                        if (XbeeStripped[0].Equals("LIGHTNING"))        // Lightning?
                        {
                            if (XbeeStripped.Length > 2)                // Data?
                            {
                                LightningData.Distance = Int32.Parse(XbeeStripped[1]);
                                LightningData.Energy = Int32.Parse(XbeeStripped[2]);
                                // 
                                // Record the time of this strike
                                //
                                LightningData.LastStrikeTime = DateTime.Now;

                                LightningData.NewReading = true;
                                LightningData.FlashLightning = true;
                                LightningData.NewScada = true;
                            }
                        }
                        Radio.LinkActive = true;
                    }
                    catch
                    {
                        Debug.Print("XBEE message decode error");
                    }
                }
            }
            XbeeBusy = false;
        }

        //*******************************************************************
        //
        // Sets the display timeout to return to the main screen if a
        // user goes to a menu and leaves it there doing nothing.
        // Fixed time of 10 mins is allowed
        //
        //*******************************************************************

        static void SetDisplayTimeout(long timeout)
        {
            long msTime = UtilsClass.msTime();

            msDisplayTimeout = msTime + timeout;
        }

        //*******************************************************************
        //
        // Updates the main screen with new data
        //
        //*******************************************************************

        static void UpdateDisplay()
        {
            if (Glide.MainWindow == null)
            {
                return;
            }
            if (Glide.MainWindow.Name == "MainWindow")
            {
                DrawingActive = true;

                if (Radio.LinkActive)
                {
                    RadioLinkState.Bitmap = radio2;
                    RadioLinkState.Invalidate();
                }
                else
                {
                    RadioLinkState.Bitmap = radio0;
                    RadioLinkState.Invalidate();
                }
                if (wifiState.WifiSignalStrength == 0)
                {
                    WifiStateImage.Bitmap = wifiState0;
                }
                else if (wifiState.WifiSignalStrength >= 80)
                {
                    WifiStateImage.Bitmap = wifiState5;
                }
                else if (wifiState.WifiSignalStrength >= 60)
                {
                    WifiStateImage.Bitmap = wifiState4;
                }
                else if (wifiState.WifiSignalStrength >= 40)
                {
                    WifiStateImage.Bitmap = wifiState3;
                }
                else if (wifiState.WifiSignalStrength >= 30)
                {
                    WifiStateImage.Bitmap = wifiState2;
                }
                else if (wifiState.WifiSignalStrength >= 20)
                {
                    WifiStateImage.Bitmap = wifiState1;
                }
                WifiStateImage.Invalidate();

                distanceLED.DrawLED(LightningData.Distance);
                energyLED.DrawLED(LightningData.Energy);

                if (LightningData.FlashLightning)
                {
                    LightningData.FlashLightning = false;
                    msShowLightning = UtilsClass.msTime() + 2000;

                    LightningImage.Bitmap = lightningOnBitmap;
                    LightningImage.Invalidate();
                }
                else
                {
                    if (UtilsClass.msTime() > msShowLightning)
                    {
                        LightningImage.Bitmap = lightningOffBitmap;
                        LightningImage.Invalidate();
                    }
                }
                StatusTextBlock.Text = GetLastStrikeTime();
                StatusTextBlock.Invalidate();

                DrawingActive = false;
            }
            if (Glide.MainWindow.Name == "EnviroWindow")
            {
                DrawingActive = true;

                if (Radio.LinkActive)
                {
                    RadioLinkState.Bitmap = radio2;
                    RadioLinkState.Invalidate();
                }
                else
                {
                    RadioLinkState.Bitmap = radio0;
                    RadioLinkState.Invalidate();
                }
                if (wifiState.WifiSignalStrength == 0)
                {
                    WifiStateImage.Bitmap = wifiState0;
                }
                else if (wifiState.WifiSignalStrength >= 80)
                {
                    WifiStateImage.Bitmap = wifiState5;
                }
                else if (wifiState.WifiSignalStrength >= 60)
                {
                    WifiStateImage.Bitmap = wifiState4;
                }
                else if (wifiState.WifiSignalStrength >= 40)
                {
                    WifiStateImage.Bitmap = wifiState3;
                }
                else if (wifiState.WifiSignalStrength >= 30)
                {
                    WifiStateImage.Bitmap = wifiState2;
                }
                else if (wifiState.WifiSignalStrength >= 20)
                {
                    WifiStateImage.Bitmap = wifiState1;
                }
                WifiStateImage.Invalidate();
                TempInLED.DrawLED(Environment.TempIn);
                HumidityInLED.DrawLED(Environment.HumidityIn);
                TempOutLED.DrawLED(Environment.TempOut);
                HumidityOutLED.DrawLED(Environment.HumidityOut);
                AirQualLED.DrawLED(Environment.AirQual);
                BarometerLED.DrawLED(Environment.Barometer);
                TempIn2LED.DrawLED(Environment.TempIn2);
                TempIn3LED.DrawLED(Environment.TempIn3);
                TempKitchenLED.DrawLED(Environment.TempKitchen);

                WattageLED.DrawLED(Environment.Wattage);
                CurrentLED.DrawLED(Environment.Current);

                FridgeTempLED.DrawLED(Environment.FridgeTemp);

                dateTimeTextBlock.Text = GetTimeDate();
                dateTimeTextBlock.Invalidate();

                DrawingActive = false;
            }
        }

        //*******************************************************************
        //
        // Creates the main window
        //
        //*******************************************************************

        static void InitMainWin()
        {
            CurrentWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.MainWindow));

            CurrentWindow.TapEvent += CurrentWindow_TapEvent;

            DistanceImage = (Image)CurrentWindow.GetChildByName("DistanceImage");
            DistanceImage.Bitmap = new Bitmap(480, 140);
            EnergyImage = (Image)CurrentWindow.GetChildByName("EnergyImage");
            EnergyImage.Bitmap = new Bitmap(480, 140);
            LightningImage = (Image)CurrentWindow.GetChildByName("LightningImage");
            LightningImage.Bitmap = lightningOffBitmap;
            //
            // Create tap events for the menu window so more screen is available
            //
            LightningImage.TapEvent += CurrentWindow_TapEvent;
            DistanceImage.TapEvent += CurrentWindow_TapEvent;
            EnergyImage.TapEvent += CurrentWindow_TapEvent;

            StatusTextBlock = (TextBlock)CurrentWindow.GetChildByName("StatusTextBlock");
            StatusTextBlock.Text = "No lighting today";

            RadioLinkState = (Image)CurrentWindow.GetChildByName("RadioImage");
            RadioLinkState.Visible = Radio.LinkActive;
            RadioLinkState.Bitmap = radio0;

            WifiStateImage = (Image)CurrentWindow.GetChildByName("WifiStateImage");
            WifiStateImage.Bitmap = wifiState0;

            distanceLED = new LEDdisplay(DistanceImage, ledDigitsBitmap, ledDecimalBitmap, 6, 0);
            energyLED = new LEDdisplay(EnergyImage, ledDigitsBitmap, ledDecimalBitmap, 6, 0);
        }

        //*******************************************************************
        /// <summary>
        /// Show the menu window if the main screen is touched
        /// </summary>
        /// <param name="sender"></param>
        ///
        static void CurrentWindow_TapEvent(object sender)
        {
            SetActiveWin(MENU_WINDOW);
        }

        //*******************************************************************
        /// <summary>
        /// Initialises the environment window
        /// </summary>
        /// 
        static void InitEnviroWin()
        {
            CurrentWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.EnviroWindow));

            CurrentWindow.TapEvent += CurrentWindow_TapEvent;

            TempInImage = (Image)CurrentWindow.GetChildByName("TempInImage");
            TempInImage.Bitmap = new Bitmap(210, 7);

            RadioLinkState = (Image)CurrentWindow.GetChildByName("RadioImage");
            RadioLinkState.Visible = Radio.LinkActive;
            RadioLinkState.Bitmap = radio0;

            WifiStateImage = (Image)CurrentWindow.GetChildByName("WifiStateImage");
            WifiStateImage.Bitmap = wifiState0;

            TempOutImage = (Image)CurrentWindow.GetChildByName("TempOutImage");
            TempOutImage.Bitmap = new Bitmap(170, 70);
            HumidityOutImage = (Image)CurrentWindow.GetChildByName("HumidityOutImage");
            HumidityOutImage.Bitmap = new Bitmap(170, 70);
            TempInImage = (Image)CurrentWindow.GetChildByName("TempInImage");
            TempInImage.Bitmap = new Bitmap(170, 70);
            HumidityInImage = (Image)CurrentWindow.GetChildByName("HumidityInImage");
            HumidityInImage.Bitmap = new Bitmap(170, 70);
            TempIn2Image = (Image)CurrentWindow.GetChildByName("TempIn2Image");
            TempIn2Image.Bitmap = new Bitmap(170, 70);
            TempIn3Image = (Image)CurrentWindow.GetChildByName("TempIn3Image");
            TempIn3Image.Bitmap = new Bitmap(170, 70);
            TempKitchenImage = (Image)CurrentWindow.GetChildByName("TempKitchenImage");
            TempKitchenImage.Bitmap = new Bitmap(170, 70);
            AirQualImage = (Image)CurrentWindow.GetChildByName("AirQualImage");
            AirQualImage.Bitmap = new Bitmap(170, 70);
            BarometerImage = (Image)CurrentWindow.GetChildByName("BarometerImage");
            BarometerImage.Bitmap = new Bitmap(160, 70);
            WattageImage = (Image)CurrentWindow.GetChildByName("WattageImage");
            WattageImage.Bitmap = new Bitmap(160, 70);
            CurrentImage = (Image)CurrentWindow.GetChildByName("CurrentImage");
            CurrentImage.Bitmap = new Bitmap(170, 70);
            FridgeTempImage = (Image)CurrentWindow.GetChildByName("FridgeTempImage");
            FridgeTempImage.Bitmap = new Bitmap(170, 70);

            TempOutLED = new LEDdisplay(TempOutImage, ledDigitsSMBitmap, ledDecimalSMBitmap, 3, 1);
            HumidityOutLED = new LEDdisplay(HumidityOutImage, ledDigitsSMBitmap, ledDecimalSMBitmap, 3, 1);
            TempInLED = new LEDdisplay(TempInImage, ledDigitsSMBitmap, ledDecimalSMBitmap, 3, 1);
            HumidityInLED = new LEDdisplay(HumidityInImage, ledDigitsSMBitmap, ledDecimalSMBitmap, 3, 1);
            TempIn2LED = new LEDdisplay(TempIn2Image, ledDigitsSMBitmap, ledDecimalSMBitmap, 3, 1);
            TempIn3LED = new LEDdisplay(TempIn3Image, ledDigitsSMBitmap, ledDecimalSMBitmap, 3, 1);
            TempKitchenLED = new LEDdisplay(TempKitchenImage, ledDigitsSMBitmap, ledDecimalSMBitmap, 3, 1);
            
            AirQualLED = new LEDdisplay(AirQualImage, ledDigitsSMBitmap, ledDecimalSMBitmap, 3, 1);
            BarometerLED = new LEDdisplay(BarometerImage, ledDigitsSMBitmap, ledDecimalSMBitmap, 4, 0);

            WattageLED = new LEDdisplay(WattageImage, ledDigitsSMBitmap, ledDecimalSMBitmap, 4, 0);
            CurrentLED = new LEDdisplay(CurrentImage, ledDigitsSMBitmap, ledDecimalSMBitmap, 3, 1);

            FridgeTempLED = new LEDdisplay(FridgeTempImage, ledDigitsSMBitmap, ledDecimalSMBitmap, 3, 1);

            dateTimeTextBlock = (TextBlock)CurrentWindow.GetChildByName("DateTimeTextBlock");
        }

        //*******************************************************************
        /// <summary>
        /// Create the menu window
        /// </summary>
        /// 
        static void InitMenuWin()
        {
            CurrentWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.MenuWindow));

            GlideButton menuBackButton = (GlideButton)CurrentWindow.GetChildByName("CancelButton");
            menuBackButton.TapEvent += menuBackButton_TapEvent;

            GlideButton menuEmailButton = (GlideButton)CurrentWindow.GetChildByName("EmailButton");
            menuEmailButton.TapEvent += menuEmailButton_TapEvent;

            GlideButton menuZigbeeButton = (GlideButton)CurrentWindow.GetChildByName("ZigbeeButton");
            menuZigbeeButton.TapEvent += menuZigbeeButton_TapEvent;

            GlideButton menuRescanButton = (GlideButton)CurrentWindow.GetChildByName("RescanButton");
            menuRescanButton.TapEvent += menuRescanButton_TapEvent;

            SSIDTextBox = (TextBox)CurrentWindow.GetChildByName("SSIDTextBox");
            SSIDTextBox.Text = ConfigData.WifiID;
        }

        //*******************************************************************
        /// <summary>
        /// Start wifi scan and go back to the main screen
        /// </summary>
        /// <param name="sender"></param>
        /// 
        static void menuRescanButton_TapEvent(object sender)
        {
            wifiState.WifiScanStart = true;
            ConfigData.WifiID = "";         // Clear so we request a new one

            SetActiveWin(ENVIRO_WINDOW);
        }

        //*******************************************************************
        /// <summary>
        /// Show the zigbee window
        /// </summary>
        /// <param name="sender"></param>
        /// 
        static void menuZigbeeButton_TapEvent(object sender)
        {
            
        }

        //*******************************************************************
        /// <summary>
        /// Show the email settings window
        /// </summary>
        /// <param name="sender"></param>
        /// 
        static void menuEmailButton_TapEvent(object sender)
        {
            SetActiveWin(EMAIL_WINDOW);
        }

        //*******************************************************************
        /// <summary>
        /// Back button pressed
        /// </summary>
        /// <param name="sender"></param>
        /// 
        static void menuBackButton_TapEvent(object sender)
        {
            SetActiveWin(ENVIRO_WINDOW);
        }

        //*******************************************************************
        /// <summary>
        /// Create the settings window
        /// </summary>
        /// 
        static void InitSettingsWin()
        {
            CurrentWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.SettingsWindow));

            GlideButton settingsAcceptButton = (GlideButton)CurrentWindow.GetChildByName("AcceptButton");
            settingsAcceptButton.TapEvent += settingsAcceptButton_TapEvent;

            GlideButton settingsCancelButton = (GlideButton)CurrentWindow.GetChildByName("CancelButton");
            settingsCancelButton.TapEvent += settingsCancelButton_TapEvent;

            SMTPServerTextBox = (TextBox)CurrentWindow.GetChildByName("SMTPServerTextBox");
            SMTPServerTextBox.TapEvent += new OnTap(Glide.OpenKeyboard);

            SMTPPortTextBox = (TextBox)CurrentWindow.GetChildByName("SMTPPortTextBox");
            SMTPPortTextBox.TapEvent += new OnTap(Glide.OpenKeyboard);

            SMTPUserTextBox = (TextBox)CurrentWindow.GetChildByName("SMTPUserTextBox");
            SMTPUserTextBox.TapEvent += new OnTap(Glide.OpenKeyboard);

            SMTPPasswordTextBox = (TextBox)CurrentWindow.GetChildByName("SMTPPasswordTextBox");
            SMTPPasswordTextBox.TapEvent += new OnTap(Glide.OpenKeyboard);

            SMTPEmailTextBox = (TextBox)CurrentWindow.GetChildByName("SMTPEmailTextBox");
            SMTPEmailTextBox.TapEvent += new OnTap(Glide.OpenKeyboard);

            SMTPServerTextBox.Text = ConfigData.EmailServer;
            SMTPPortTextBox.Text = ConfigData.EmailPort.ToString();
            SMTPUserTextBox.Text = ConfigData.EmailUser;
            SMTPPasswordTextBox.Text = ConfigData.EmailPassword;
            SMTPEmailTextBox.Text = ConfigData.EmailToAddress;
        }

        static void settingsAcceptButton_TapEvent(object sender)
        {
            SetActiveWin(MENU_WINDOW);
        }

        //*******************************************************************
        /// <summary>
        /// Handles the cancel/exit button and returns to the menu window
        /// </summary>
        /// <param name="sender"></param>
        /// 
        static void settingsCancelButton_TapEvent(object sender)
        {
            ConfigData.EmailServer = SMTPServerTextBox.Text;
            try 
            {
                ConfigData.EmailPort = Int16.Parse(SMTPPortTextBox.Text);
            }
            catch(Exception)
            {}
            ConfigData.EmailUser = SMTPUserTextBox.Text;
            ConfigData.EmailPassword = SMTPPasswordTextBox.Text;
            ConfigData.EmailToAddress = SMTPEmailTextBox.Text;
            ConfigData.EmailSender = "environment@embeddedcomputer.co.uk";

            SaveSettings();

            SetActiveWin(MENU_WINDOW);
        }

        //*******************************************************************
        /// <summary>
        /// Gets how long the last strike was as a message
        /// </summary>
        /// <returns></returns>
        /// 
        static String GetLastStrikeTime()
        {
            String msg = "Last strike ";

            if(LightningData.LastStrikeTime.Ticks == 0)
            {
                return ("No strikes recorded");
            }
            TimeSpan timeSpan = DateTime.Now - LightningData.LastStrikeTime;

            if(timeSpan.Days > 0)
            {
                msg += timeSpan.Days.ToString() + " days ago";
            }
            else if(timeSpan.Hours > 0)
            {
                msg += timeSpan.Hours.ToString() + " hour(s) ago";
            }
            else if (timeSpan.Minutes > 0)
            {
                msg += timeSpan.Minutes.ToString() + " min(s) ago";
            }
            else if (timeSpan.Seconds > 0)
            {
                msg += timeSpan.Seconds.ToString() + " secs ago";
            }
            else
            {
                msg = "Strike just now";
            }
            return (msg);
        }

        //*******************************************************************
        //
        // Handles a Wifi connection
        //
        //*******************************************************************

        static void wifiHandler()
        {
            DateTime UtcTime;
            long msTime;
            long msSendTime;
            double[] replyValues;

            Thread.Sleep(3000);     // Wait a little time before doing this

            wifi = new WiFiRS9110(SPI.SPI_module.SPI2, GHI.Pins.G120.P1_10, GHI.Pins.G120.P2_11, GHI.Pins.G120.P1_9, 4000);

            wifi.Open();
            wifi.NetworkInterface.EnableDhcp();
            wifi.EnableDynamicDns();

            StartWifiScan();

            if (wifi.LinkConnected)
            {
                wifiState.WifiState = _WifiStates.WIFI_CONNECTED;
                //
                // If get here, Wifi is now active so we get the time from a time server
                //
                try
                {
                    UtcTime = GetNetworkTime("0.fr.pool.ntp.org");

                    RealTimeClock.SetDateTime(UtcTime);     // Set the hardware clock
                    //
                    // Set the .NETMF clock
                    //
                    Microsoft.SPOT.Hardware.Utility.SetLocalTime(UtcTime + new TimeSpan((long)(TimeSpan.TicksPerHour * ConfigData.TimeZoneOffset)));
                }
                catch (Exception ex)
                {
                    Debug.Print("Fetch time from server failed: " + ex.Message);
                    //
                    // Set NETMF time from RTC instead. Must do here before the timers are initialised
                    //
                    Utility.SetLocalTime(RealTimeClock.GetDateTime());
                }
            }
            Thread.Sleep(2000);

            msSendTime = UtilsClass.msTime();  // Will cause the first update to go out now

            wifiState.WifiScanStart = false;      // If get here, we are already connected

            while (true)
            {
                if (wifiState.WifiScanStart)
                {
                    wifiState.WifiScanStart = false;

                    StartWifiScan();
                    if (wifi.LinkConnected)
                    {
                        wifiState.WifiState = _WifiStates.WIFI_CONNECTED;
                    }
                }
                try
                {
                    if (wifi.LinkConnected)
                    {
                        if (LightningData.NewScada)
                        {
                            LightningData.NewScada = false;

                            SendToSCADA();
                        }
                        if(UtilsClass.msTime() >= msRequestStatusTime)
                        {
                            msRequestStatusTime = UtilsClass.msTime() + 5000;

                            replyValues = RequestSCADA(deviceList);

                            if(replyValues != null)
                            {
                                ParseReplies(replyValues);
                            }
                        }
                    }
                    else
                    {
                        //
                        // No connection, so try to reconnect
                        //
                        wifiState.WifiState = _WifiStates.WIFI_STANDBY;       // Not connected anymore
                        //
                        // Let's sleep for a few Seconds and then rejoin it
                        //
                        Thread.Sleep(2000);
                        wifiState.WifiScanStart = true;
                    }
                }
                catch (Exception)
                {
                }
                Thread.Sleep(1000);
            }
        }

        //*******************************************************************
        /// <summary>
        /// Parses the values to update the data 
        /// 
        //  Voltage, Current, Watts, Fridge U, Fridge L, Front Room Temp,
        //  Bedroom Temp, Kitchen Temp, Dust Ratio, Barometer
        /// </summary>
        /// <param name="replies"></param>
        ///
        static void ParseReplies(double[] replies)
        {
            int count = replies.Length;

            if(count > 0)
            {
                Environment.Current = replies[1];
                Environment.Wattage = replies[2];
                Environment.FridgeTemp = replies[3];
                Environment.TempIn = replies[5];
                Environment.TempIn2 = replies[6];
                Environment.TempKitchen = replies[7];
                Environment.AirQual = replies[8];
                Environment.Barometer = replies[9];
            }
        }

        //*******************************************************************
        //
        // Starts a wifi scan. If SSID is blank, a list is shown for a new
        // connection to be selected
        //
        //*******************************************************************

        static void StartWifiScan()
        {
            wifiState.WifiSignalStrength = 0;                       // Reset for start of scan

            if (wifiState.WifiState == _WifiStates.WIFI_SCANNING)  // Already scanning?
            {
                return;
            }
            wifiState.WifiState = _WifiStates.WIFI_SCANNING;

            if (wifi.LinkConnected)
            {
                wifi.Disconnect();          // Disconnect if connected
            }
            try
            {
                scanResults = wifi.Scan();
            }
            catch (Exception er)
            {
                Debug.Print("WiFi SCAN failed to start");

                Thread.Sleep(2000);
                return;
            }

            if (scanResults == null)
            {
                Debug.Print("WiFi no scan results detected");

                Thread.Sleep(2000);         // Wait before trying again
                return;
            }
            //
            // Once we have a list, see if the one we last connected to is
            // in the list
            //
            int Selected = 0;
            String NetworkKey = "";

            if (ConfigData.WifiID != "")
            {
                int Index = 0;
                String SSID;
                //
                // Locate our SSID in the scan results
                //
                for (Index = 0; Index < scanResults.Length; Index++)
                {
                    SSID = scanResults[Index].Ssid;
                    if (SSID == ConfigData.WifiID)         // Found our network?
                    {
                        NetworkKey = ConfigData.WifiPassword;
                        Selected = Index;

                        WiFiRS9110.NetworkParameters netParams = new WiFiRS9110.NetworkParameters();

                        netParams = scanResults[Index];
                        netParams.Key = NetworkKey;

                        try
                        {
                            wifi.Join(netParams);

                            wifiState.WifiSignalStrength = scanResults[Index].Rssi;
                        }
                        catch (Exception err)
                        {
                            Debug.Print(err.Message);
                        }
                        break;
                    }
                }
            }
            else
            {
                //
                // Bring up a list for user selection
                //
                wifiState.WifiSelection = -1;   // Nothing selected yet
                wifiState.WindowShowWiFi = true;

                long msWaitTimer = UtilsClass.msTime() + 60000; // Max wait 1 minute

                while (wifiState.WifiSelection == -1 && UtilsClass.msTime() < msWaitTimer)
                {
                    Thread.Sleep(1000);
                }
                if (wifiState.WifiSelection >= 0)
                {
                    NetworkKey = ConfigData.WifiID;
                    Selected = wifiState.WifiSelection;


                    try
                    {
                        wifi.Join(ConfigData.WifiID, NetworkKey);

                        wifiState.WifiSignalStrength = scanResults[wifiState.WifiSelection].Rssi;
                    }
                    catch (Exception error)
                    {
                        Debug.Print(error.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the time from the internet
        /// </summary>
        /// <returns></returns>
        /// 
        static public DateTime GetNetworkTime()
        {
            return GetNetworkTime("time-a.nist.gov");
        }

        //*******************************************************************
        //
        // Gets the current DateTime from <paramref name="ntpServer"/>.
        // The hostname of the NTP server.
        // A DateTime containing the current time
        //
        //*******************************************************************

        static public DateTime GetNetworkTime(string ntpServer)
        {
            IPAddress[] address = Dns.GetHostEntry(ntpServer).AddressList;

            if (address == null || address.Length == 0)
                throw new ArgumentException("Could not resolve ip address from '" + ntpServer + "'.", "ntpServer");

            IPEndPoint ep = new IPEndPoint(address[0], 123);

            return GetNetworkTime(ep);
        }

        //*******************************************************************
        //
        // Gets the current DateTime from IPEndPoint.
        // <returns>A DateTime containing the current time.
        //
        //*******************************************************************

        static public DateTime GetNetworkTime(IPEndPoint ep)
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            s.Connect(ep);

            byte[] ntpData = new byte[48]; // RFC 2030 
            ntpData[0] = 0x1B;
            for (int i = 1; i < 48; i++)
                ntpData[i] = 0;

            lock (s)
            {
                s.Send(ntpData);
            }
            s.Receive(ntpData);

            byte offsetTransmitTime = 40;
            ulong intpart = 0;
            ulong fractpart = 0;

            for (int i = 0; i <= 3; i++)
                intpart = 256 * intpart + ntpData[offsetTransmitTime + i];

            for (int i = 4; i <= 7; i++)
                fractpart = 256 * fractpart + ntpData[offsetTransmitTime + i];

            ulong milliseconds = (intpart * 1000 + (fractpart * 1000) / 0x100000000L);
            s.Close();

            TimeSpan timeSpan = TimeSpan.FromTicks((long)milliseconds * TimeSpan.TicksPerMillisecond);

            DateTime dateTime = new DateTime(1900, 1, 1);
            dateTime += timeSpan;

            TimeSpan offsetAmount = TimeZone.CurrentTimeZone.GetUtcOffset(dateTime);
            DateTime networkDateTime = (dateTime + offsetAmount);

            return networkDateTime;
        }

        //*******************************************************************
        //
        // Init the Wifi selection window
        //
        //*******************************************************************

        static void InitWifiWin()
        {
            CurrentWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.wifiWindow));

            WifiList = (DataGrid)CurrentWindow.GetChildByName("wifiList");
            WifiList.TapCellEvent += new OnTapCell(WifiList_TapCellEvent);

            wifiConnectButton = (GlideButton)CurrentWindow.GetChildByName("connectButton");
            wifiConnectButton.TapEvent += new OnTap(wifiConnectButton_TapEvent);
            wifiConnectButton.Enabled = false;      // Until something selected

            wifiCancelButton = (GlideButton)CurrentWindow.GetChildByName("cancelButton");
            wifiCancelButton.TapEvent += new OnTap(wifiCancelButton_TapEvent);
            //
            // Populate the box now
            //
            DataGridColumn SSID = new DataGridColumn("SSID", 250);
            DataGridColumn Signal = new DataGridColumn("Signal", 100);
            DataGridColumn SecType = new DataGridColumn("Security", 200);

            WifiList.AddColumn(SSID);
            WifiList.AddColumn(Signal);
            WifiList.AddColumn(SecType);

            DataGridItem WifiData;
            Object[] GridData;

            if (scanResults != null)
            {
                for (int Index = 0; Index < scanResults.Length; Index++)
                {
                    GridData = new Object[3];

                    GridData[0] = scanResults[Index].Ssid;
                    GridData[1] = scanResults[Index].Rssi;
                    GridData[2] = SecurityModes[(int)scanResults[Index].SecurityMode];

                    WifiData = new DataGridItem(GridData);
                    WifiList.AddItem(WifiData);
                }
            }
            wifiState.WifiIndex = -1;
        }

        //*******************************************************************
        //
        // Cancel the wifi selection
        //
        //*******************************************************************

        static void wifiCancelButton_TapEvent(object sender)
        {
            SetActiveWin(ENVIRO_WINDOW);
        }

        //*******************************************************************
        //
        // Got a selection, so we request the key
        //
        //*******************************************************************

        static void wifiConnectButton_TapEvent(object sender)
        {
            if (wifiState.WifiIndex >= 0)          // Must be a selection first
            {
                SetActiveWin(NETKEY_WINDOW);
            }
        }

        //*******************************************************************
        //
        // Get the selection the user clicked on
        //
        //*******************************************************************

        static void WifiList_TapCellEvent(object sender, TapCellEventArgs args)
        {
            wifiState.WifiIndex = WifiList.SelectedIndex;

            wifiConnectButton.Enabled = true;
            wifiConnectButton.Invalidate();
        }

        //*******************************************************************
        //
        // Init the key input window
        //
        //*******************************************************************

        static void InitNetKeyWin()
        {
            CurrentWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.NetKeyWindow));

            WifiKeyText = (TextBox)CurrentWindow.GetChildByName("wifiKeyText");
            WifiKeyText.TapEvent += new OnTap(Glide.OpenKeyboard);

            keyOkButton = (GlideButton)CurrentWindow.GetChildByName("okButton");
            keyOkButton.TapEvent += new OnTap(keyOkButton_TapEvent);

            keyCancelButton = (GlideButton)CurrentWindow.GetChildByName("cancelButton");
            keyCancelButton.TapEvent += new OnTap(keyCancelButton_TapEvent);
            //
            // Use existing key from configuration as it may be valid
            //
            WifiKeyText.Text = ConfigData.WifiPassword;
        }

        //*******************************************************************
        //
        // Cancel and back to list
        //
        //*******************************************************************

        static void keyCancelButton_TapEvent(object sender)
        {
            //
            // Back to wifi list
            //
            SetActiveWin(WIFI_WINDOW);
        }

        //*******************************************************************
        //
        // Key is input, so continue
        //
        //*******************************************************************

        static void keyOkButton_TapEvent(object sender)
        {
            //
            // Get the key
            //
            ConfigData.WifiPassword = WifiKeyText.Text;
            ConfigData.WifiID = scanResults[wifiState.WifiIndex].Ssid;

            SaveSettings();
            //
            // After we go back to the main screen, the system will try to connect
            // to the SSID we chose
            //
            SetActiveWin(ENVIRO_WINDOW);

            wifiState.WifiSelection = wifiState.WifiIndex;      // This will trigger the connection
        }

        //*******************************************************************
        //
        // Send data to Homeseer
        //
        //*******************************************************************

        static void SendToSCADA()
        {
            byte[] result = new byte[32768];
            int read = 0;
            String request = "http://192.168.1.125:8050/JSON?request=controldevicebyvalue&ref=44&value=" + LightningData.Distance.ToString();

            using(var req = HttpWebRequest.Create(request) as HttpWebRequest)
            {
                using(var res = req.GetResponse() as HttpWebResponse)
                {
                    using (var stream = res.GetResponseStream())
                    {
                        int offset = 0;
                        do
                        {
                            read = stream.Read(result, offset, result.Length - offset);

                            offset += read;

                            Thread.Sleep(20);
                        }
                        while (read != 0);
                        String reply = new String(System.Text.Encoding.UTF8.GetChars(result));
                    }
                }
            }
        }

        static double[] RequestSCADA(int[] devices)
        {
            byte[] result = new byte[32768];
            int read = 0;
            String request = "http://192.168.1.125:8050/JSON?request=getstatus&ref=" + devices[0].ToString();
            String reply = "";
            double[] value;

            for (int index = 1; index < devices.Length; index++)
            {
                request += "&getstatus&ref=" + devices[index].ToString();
            }
            using (var req = HttpWebRequest.Create(request) as HttpWebRequest)
            {
                using (var res = req.GetResponse() as HttpWebResponse)
                {
                    using (var stream = res.GetResponseStream())
                    {
                        int offset = 0;
                        do
                        {
                            read = stream.Read(result, offset, result.Length - offset);

                            offset += read;

                            Thread.Sleep(20);
                        }
                        while (read != 0);
                        reply = new String(System.Text.Encoding.UTF8.GetChars(result));
                    }
                }
            }
            //
            // Now parse the reply to find the status?
            //
            value = new double[devices.Length];

            if(reply.IndexOf("HomeSeer Devices") >= 0)
            {
                Hashtable parsed = (Hashtable) JsonParser.JsonDecode(reply);

                ICollection collection = (ICollection) parsed["Devices"];

                foreach (Hashtable k in collection)
                {
                    int index = 0;
                    double keyValue = 0;
                    double keyValueDouble = 0;
                    int keyValueInt = 0;
                    ulong keyValueUlong = 0;
                    bool found = false;
                    String keyValueStr;
                    int ptr;
                    ulong refValue = 0;

                    ICollection keys = (ICollection)k.Keys;
                    ICollection valueCol = (ICollection)k.Values;
                    object[] values = new object[valueCol.Count];

                    valueCol.CopyTo(values, 0);

                    foreach(String header in keys)
                    {
                        if (header.Equals("ref"))
                        {
                            refValue = (ulong) values[index];

                            found = true;
                        }
                        if(found && header.Equals("value"))
                        {
                            if(values[index].GetType() == keyValueDouble.GetType())
                            {
                                keyValueDouble = (double)values[index];

                                keyValue = keyValueDouble;
                            }
                            else if (values[index].GetType() == keyValueInt.GetType())
                            {
                                keyValueInt = (int)values[index];

                                keyValue = (double)keyValueInt;
                            }
                            else if (values[index].GetType() == keyValueUlong.GetType())
                            {
                                keyValueUlong = (ulong)values[index];

                                keyValue = (double)keyValueUlong;
                            }
                            // 
                            // Find this device and save the value
                            //
                            ptr = 0;        
                            while(ptr < deviceList.Length)
                            {
                                if(deviceList[ptr] == (int) refValue)
                                {
                                    value[ptr] = (double) keyValue;

                                    break;
                                }
                                ptr++;
                            }
                            found = false;
                        }
                        index++;
                    }
                }
            }
            return value;
        }

        //*******************************************************************
        //
        // Sound a little peep to aler the user. After doing so, wait 1 min
        // before doing a second one
        //
        //*******************************************************************

        static void SoundHandler()
        {
            uint[] time = new uint[] {50, 80};

            while(true)
            {
                if(MakeSound && ! HaltSound)
                {
                    MakeSound = false;
                    HaltSound = true;       // Don't trigger again for at least 1 minute
                    msHaltSoundTime = UtilsClass.msTime() + 60000;

                    buzzer.Write(true);

                    Thread.Sleep(100);

                    buzzer.Write(false);
                }
                if(HaltSound)
                {
                    if(MakeSound)
                    {
                        MakeSound = false;
                    }
                    if(UtilsClass.msTime() >= msHaltSoundTime)
                    {
                        HaltSound = false;
                    }
                }
                Thread.Sleep(100);
            }
        }

        //*******************************************************************
        //
        // Init the system for us to use it
        //
        //*******************************************************************

        static void InitSystem()
        {
            //
            // Mount the SD card with the INI file on it
            //
            if (!UtilsClass.mountINI())
            {
                Thread.Sleep(1000);
            }
            mySmtp = new SmtpClient(ConfigData.EmailServer, ConfigData.EmailPort);
        }

        //*******************************************************************
        //
        // Sends an email via SMTP
        //
        //*******************************************************************

        static void SendEmail(String address, String subject, String body)
        {
            mySmtp.Send(ConfigData.EmailSender, address, subject, body, true, ConfigData.EmailUser, ConfigData.EmailPassword);
        }

        //*******************************************************************
        /// <summary>
        /// Returns the date and time as a string
        /// </summary>
        /// <returns>Time and Date String</returns>
        /// 
        static string GetTimeDate()
        {
            string sRet = "";
            DateTime t = DateTime.Now;
            sRet = t.ToString("dd MMM yyyy HH:mm:ss");
            return sRet;
        }

    }
}
