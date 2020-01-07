using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using EOSDigital.API;
using EOSDigital.SDK;

namespace ConsoleExample
{
    class Program
    {
        static CanonAPI APIHandler;
        static Camera MainCamera;
        static string ImageSaveDirectory;
        static bool Error = false;
        static ManualResetEvent WaitEvent = new ManualResetEvent(false);

        static string ImageFileName;
        static bool atFar = true;

        static void Main(string[] args)
        {
            try
            {
                APIHandler = new CanonAPI();
                List<Camera> cameras = APIHandler.GetCameraList();
                if (!OpenSession())
                {
                    Console.WriteLine("No camera found. Please plug in camera");
                    APIHandler.CameraAdded += APIHandler_CameraAdded;
                    WaitEvent.WaitOne();
                    WaitEvent.Reset();
                }

                if (!Error)
                {
                    //ImageSaveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "RemotePhoto");
                    ImageSaveDirectory = "G:\\CSE260\\dataset\\3";
                    MainCamera.SetSetting(PropertyID.SaveTo, (int)SaveTo.Host);
                    MainCamera.SetCapacity(4096, int.MaxValue);
                    Console.WriteLine($"Set image output path to: {ImageSaveDirectory}");

                    int SHUTTER_ID = 30;

                    // Get aperture and shutter speed tables
                    CameraValue[] AvList = MainCamera.GetSettingsList(PropertyID.Av);
                    CameraValue[] TvList = MainCamera.GetSettingsList(PropertyID.Tv);

                    
                    // Loop over all aperture
                    for (int i_av = 0; i_av < AvList.Length; ++i_av)
                    {
                        // Validate shutter speed
                        if (SHUTTER_ID - i_av >= TvList.Length)
                        {
                            Console.WriteLine("shutter speed out of bounds");
                            break;
                        }

                        
                        // Set Aperture and Shutter Speed
                        MainCamera.SetSetting(PropertyID.Av, AvValues.GetValue((string)AvList[i_av]).IntValue);
                        MainCamera.SetSetting(PropertyID.Tv, TvValues.GetValue((string)TvList[SHUTTER_ID - i_av]).IntValue);

                        Console.WriteLine("set av to " + (string)AvList[i_av]);
                        Console.WriteLine("set tv to " + (string)TvList[SHUTTER_ID - i_av]);

                        System.Threading.Thread.Sleep(500);

                        int now_av = MainCamera.GetInt32Setting(PropertyID.Av);
                        int now_tv = MainCamera.GetInt32Setting(PropertyID.Tv);

                        if ((int)AvList[i_av] != now_av) Console.WriteLine("av comfirmation failed");
                        if ((int)TvList[SHUTTER_ID - i_av] != now_tv) Console.WriteLine("tv comfirmation failed");


                        double shutterTime = TvList[SHUTTER_ID - i_av].DoubleValue;
                        int waitTime = (int)(shutterTime * 1000 + 2000);
                        Console.WriteLine("Wait Time is " + waitTime);

                        // 22 +3 Steps
                        for (int f = 0; f < 22; ++f)
                        {
                            int actualf = f;
                            if (!atFar) actualf = 21 - f;

                            ImageFileName = now_av + "_" + actualf + ".CR3";

                            MainCamera.TakePhoto();

                            System.Threading.Thread.Sleep(waitTime);

                            // Flip the step direction each time to save time
                            if (atFar) MainCamera.SendCommand(CameraCommand.DriveLensEvf, (int)DriveLens.Near3);
                            else MainCamera.SendCommand(CameraCommand.DriveLensEvf, (int)DriveLens.Far3);

                            System.Threading.Thread.Sleep(500);
                        }

                        // Flip far->near and far<-near
                        atFar = !atFar;
                    }

                    /*
                    try { MainCamera.SendCommand(CameraCommand.DriveLensEvf, (int)DriveLens.Far3); }
                    catch (Exception ex)
                    { Console.WriteLine(ex); }*/

                    /*
                    Console.WriteLine("Taking photo with current settings...");
                    CameraValue tv = TvValues.GetValue(MainCamera.GetInt32Setting(PropertyID.Tv));
                    if (tv == TvValues.Bulb) MainCamera.TakePhotoBulb(2);
                    else MainCamera.TakePhoto();
                    WaitEvent.WaitOne();
                    */

                    if (!Error) Console.WriteLine("Photo taken and saved");
                }
            }
            catch (Exception ex) { Console.WriteLine("Error: " + ex.Message); }
            finally
            {
                Console.WriteLine("finally");
                MainCamera?.Dispose();
                APIHandler.Dispose();
                Console.WriteLine("Good bye! (press any key to close)");
                Console.ReadKey();
            }
        }

        private static void APIHandler_CameraAdded(CanonAPI sender)
        {
            try
            {
                Console.WriteLine("Camera added event received");
                if (!OpenSession()) { Console.WriteLine("Sorry, something went wrong. No camera"); Error = true; }
            }
            catch (Exception ex) { Console.WriteLine("Error: " + ex.Message); Error = true; }
            finally { WaitEvent.Set(); }
        }

        private static void MainCamera_DownloadReady(Camera sender, DownloadInfo Info)
        {
            try
            {
                Console.WriteLine("Starting image download...");
                int av = sender.GetInt32Setting(PropertyID.Av);
                Info.FileName = ImageFileName;
                sender.DownloadFile(Info, ImageSaveDirectory);
            }
            catch (Exception ex) { Console.WriteLine("Error: " + ex.Message); Error = true; }
            finally { WaitEvent.Set(); }
        }

        private static bool OpenSession()
        {
            List<Camera> cameras = APIHandler.GetCameraList();
            if (cameras.Count > 0)
            {
                MainCamera = cameras[0];
                MainCamera.DownloadReady += MainCamera_DownloadReady;
                MainCamera.OpenSession();
                Console.WriteLine($"Opened session with camera: {MainCamera.DeviceName}");
                return true;
            }
            else return false;
        }
    }
}
