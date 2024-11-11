using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.Json;
using vJoyInterfaceWrap;
using vJoyInterface;

namespace ETS2SoftwareEyeDetector
{
    class Program
    {


        // Declaring one joystick (Device id 1) and a position structure. 
        static public vJoy joystick;
        static public vJoy.JoystickState iReport;
        static public uint id = 1;


        static void Main(string[] args)
        {
            // Create one joystick object and a position structure.
            joystick = new vJoy();
            iReport = new vJoy.JoystickState();


            // Device ID can only be in the range 1-16
            if (args.Length > 0 && !String.IsNullOrEmpty(args[0]))
                id = Convert.ToUInt32(args[0]);
            if (id <= 0 || id > 16)
            {
                Console.WriteLine("Illegal device ID {0}\nExit!", id);
                return;
            }

            // Get the driver attributes (Vendor ID, Product ID, Version Number)
            if (!joystick.vJoyEnabled())
            {
                Console.WriteLine("vJoy driver not enabled: Failed Getting vJoy attributes.\n");
                return;
            }
            else
                Console.WriteLine("Vendor: {0}\nProduct :{1}\nVersion Number:{2}\n", joystick.GetvJoyManufacturerString(), joystick.GetvJoyProductString(), joystick.GetvJoySerialNumberString());

            // Get the state of the requested device
            VjdStat status = joystick.GetVJDStatus(id);
            switch (status)
            {
                case VjdStat.VJD_STAT_OWN:
                    Console.WriteLine("vJoy Device {0} is already owned by this feeder\n", id);
                    break;
                case VjdStat.VJD_STAT_FREE:
                    Console.WriteLine("vJoy Device {0} is free\n", id);
                    break;
                case VjdStat.VJD_STAT_BUSY:
                    Console.WriteLine("vJoy Device {0} is already owned by another feeder\nCannot continue\n", id);
                    return;
                case VjdStat.VJD_STAT_MISS:
                    Console.WriteLine("vJoy Device {0} is not installed or disabled\nCannot continue\n", id);
                    return;
                default:
                    Console.WriteLine("vJoy Device {0} general error\nCannot continue\n", id);
                    return;
            };

            // Check which axes are supported
            bool AxisX = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_X);
            bool AxisY = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Y);
            bool AxisZ = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Z);
            bool AxisRX = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RX);
            bool AxisRZ = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RZ);
            // Get the number of buttons and POV Hat switchessupported by this vJoy device
            int nButtons = joystick.GetVJDButtonNumber(id);
            int ContPovNumber = joystick.GetVJDContPovNumber(id);
            int DiscPovNumber = joystick.GetVJDDiscPovNumber(id);

            // Print results
            Console.WriteLine("\nvJoy Device {0} capabilities:\n", id);
            Console.WriteLine("Numner of buttons\t\t{0}\n", nButtons);
            Console.WriteLine("Numner of Continuous POVs\t{0}\n", ContPovNumber);
            Console.WriteLine("Numner of Descrete POVs\t\t{0}\n", DiscPovNumber);
            Console.WriteLine("Axis X\t\t{0}\n", AxisX ? "Yes" : "No");
            Console.WriteLine("Axis Y\t\t{0}\n", AxisX ? "Yes" : "No");
            Console.WriteLine("Axis Z\t\t{0}\n", AxisX ? "Yes" : "No");
            Console.WriteLine("Axis Rx\t\t{0}\n", AxisRX ? "Yes" : "No");
            Console.WriteLine("Axis Rz\t\t{0}\n", AxisRZ ? "Yes" : "No");

            // Test if DLL matches the driver
            UInt32 DllVer = 0, DrvVer = 0;
            bool match = joystick.DriverMatch(ref DllVer, ref DrvVer);
            if (match)
                Console.WriteLine("Version of Driver Matches DLL Version ({0:X})\n", DllVer);
            else
                Console.WriteLine("Version of Driver ({0:X}) does NOT match DLL Version ({1:X})\n", DrvVer, DllVer);


            // Acquire the target
            if ((status == VjdStat.VJD_STAT_OWN) || ((status == VjdStat.VJD_STAT_FREE) && (!joystick.AcquireVJD(id))))
            {
                Console.WriteLine("Failed to acquire vJoy device number {0}.\n", id);
                return;
            }
            else
                Console.WriteLine("Acquired: vJoy device number {0}.\n", id);

            Console.WriteLine("\npress enter to stat feeding");

            int X = 1;
            long maxval = 0;

            joystick.GetVJDAxisMax(id, HID_USAGES.HID_USAGE_X, ref maxval);

            bool res;
            // Reset this device to default values
            joystick.ResetVJD(id);

            // Feed the device in endless loop
            float angle = 1f;
            float angleAddition;

            while (true)
            {
                if (angle != 0 && X != 0)
                {
                    try
                    {
                        if (!File.Exists("data.json.lock"))
                        {
                            string jsonData = File.ReadAllText("data.json");
                            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData);
                            var angleTemp = data["angle"];
                            angle = float.Parse($"{angleTemp}");
                            Console.WriteLine("Received angle: " + angle);
                        }
                        else { Console.WriteLine("File is locked"); }


                        if (angle < 0)
                        {
                            angleAddition = -angle + 1;
                        }
                        else
                        {
                            angleAddition = 0;
                        }

                        if (angle > X)
                        {
                            X += Convert.ToInt32(5 * (0.06 * (angle + angleAddition / X + angleAddition)));
                        }
                        else
                        {
                            X -= Convert.ToInt32(5 * 0.06 * (X + angleAddition / angle + angleAddition));
                        }
                        res = joystick.SetAxis(Convert.ToInt32(Map(angle, -20, 20, 0, maxval)), id, HID_USAGES.HID_USAGE_X);
                    }
                    catch (Exception exception)
                    {
                        X = Convert.ToInt32(Map(angle, -20, 20, 0, maxval));
                        Console.WriteLine(exception);
                    }
                }
            }
        } // Main
        static float Map(float value, float minInput, float maxInput, float minOutput, float maxOutput)
        {
            return (value - minInput) / (maxInput - minInput) * (maxOutput - minOutput) + minOutput;
        }
    } // class Program
} // namespace FeederDemoCS
