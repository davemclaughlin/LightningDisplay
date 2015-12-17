using System;
using Microsoft.SPOT;
using JDI.Storage;
using JDI.NETMF.Storage;

namespace LightningDisplay
{
    public class UtilsClass
    {
        //
        // Init file stuff. We pass this to each class that needs to store
        // data in the ini file
        //
        public static JDI.Storage.IniSettings ini;

        public static float EngUnitsFloat(float Raw, float MinEngineering,
                             float MaxEngineering, float MinCalibrated,
                             float MaxCalibrated)
        {
            float SlopeCoeff = 0, OffsetCoeff = 0;

            try
            {
                SlopeCoeff = (MaxEngineering - MinEngineering) / (MaxCalibrated - MinCalibrated);
                OffsetCoeff = MaxEngineering - (SlopeCoeff * MaxCalibrated);
            }
            catch(Exception)
            {
                return (0);
            }
            return ((Raw * SlopeCoeff) + OffsetCoeff);
        }

        public static double dEngUnitsFloat(double Raw, double MinEngineering,
                             double MaxEngineering, double MinCalibrated,
                             double MaxCalibrated)
        {
            double SlopeCoeff = 0, OffsetCoeff = 0;

            try
            {
                SlopeCoeff = (MaxEngineering - MinEngineering) / (MaxCalibrated - MinCalibrated);
                OffsetCoeff = MaxEngineering - (SlopeCoeff * MaxCalibrated);
            }
            catch (Exception)
            {
                return (0);
            }
            return ((Raw * SlopeCoeff) + OffsetCoeff);
        }

        public static long msTime()
        {
            long msTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            return msTime;
        }

        public static bool mountINI()
        {
            try
            {
                //
                // Init the ini file and load into memory
                //
                if (JDI.NETMF.Storage.SDCard.MountSD() == true)
                {
                    ini = new IniSettings(JDI.NETMF.Storage.SDCard.RootDirectory + "\\SETTINGS.INI");

                    ini.LoadSettings();
                }
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        public static bool saveINI()
        {
            if (JDI.NETMF.Storage.SDCard.IsMounted)
            {
                if (ini.IsModified)
                {
                    ini.SaveSettings();
                    // 
                    // Unmount the card and remount it
                    //
                    JDI.NETMF.Storage.SDCard.UnMountSD();

                    System.Threading.Thread.Sleep(200);

                    JDI.NETMF.Storage.SDCard.MountSD();
                }
            }
            return true;
        }
    }
}
