using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using WIA;

namespace SUNO.logic
{
    public class CameraLogic
    {

        private CommonDialog WIA_camera;

        public static void InitCamera(model.WIACamera camera, String iso, String exposure, ref TrajectorySet ts)
        {
            ts.ISO = "" + iso;
            ts.EXP = "" + exposure;

            InitCamera(camera,iso, exposure);
        }

        public static void InitCamera(model.WIACamera camera, String iso, String exposure)
        {   
            DeviceManager deviceManager = new DeviceManager();
            Device device = null;
            
            foreach (DeviceInfo info in deviceManager.DeviceInfos)
            {
                if (camera != null)
                    if (info.DeviceID == camera.DeviceID)
                    {
                        device = info.Connect();
                        foreach (Property item in device.Items[1].Properties)
                        {
                            //set WIA_DPC_EXPOSURE_MODE  to EXPOSUREMODE_MANUAL
                            if (item.PropertyID == 0x00000804)
                            {

                            }
                            //set WIA_DPC_EXPOSURE_TIME to 
                            if (item.PropertyID == 0x00000806)
                            {

                            }

                            //set iso WIA_DPC_EXPOSURE_INDEX
                            if (item.PropertyID == 0x00000823)
                            {

                            }

                            //flash WIA_DPC_FLASH_MODE to FLASHMODE_OFF
                            if (item.PropertyID == 0x00000808)
                            {

                            }
                        }
                    }
            }
        }

        public static string getInfo(model.WIACamera camera){
           DeviceManager deviceManager = new DeviceManager();
           Device device = null;
           String result = "";
           foreach (DeviceInfo info in deviceManager.DeviceInfos)
           {
               if (camera != null)
                   if (info.DeviceID == camera.DeviceID)
                   {

                       device = info.Connect();
                       foreach (Property item in device.Properties)
                       {
                           result += (item.IsReadOnly + ": " + item.Name + "  (" + item.PropertyID + ") \n");
                       }
                   }
           }
           return result;
        }

        public static object[] ListDevices()
        {
            List<object> deviceList = new List<object>();
            // Clear the ListBox.
            // Create a DeviceManager instance
            DeviceManager deviceManager = new DeviceManager();
            model.WIACamera device;

            // Loop through the list of devices and add the name to the listbox
            for (int i = 1; i <= deviceManager.DeviceInfos.Count; i++)
            {
                if (deviceManager.DeviceInfos[i].Type == WiaDeviceType.CameraDeviceType)
                {
                    device = new model.WIACamera();
                    device.DeviceID = deviceManager.DeviceInfos[i].DeviceID;
                    device.Name = deviceManager.DeviceInfos[i].Properties["Name"].get_Value().ToString();

                    deviceList.Add(device);
                }
            }

            return deviceList.ToArray();
        }

        public static Image TakePicture(model.WIACamera camera,ref TrajectorySet ts)
        {
            ts.shot_time = DateTime.Now;

            return TakePicture(camera);
        }

        public static Image TakePicture(model.WIACamera camera)
        {
            Image image = null;
            String file_name = ".\\IMG\\"+DateTime.Now+".jpg";
            file_name=file_name.Replace("/", "_");
            file_name = file_name.Replace(":", "_");
            file_name = file_name.Replace(" ", "_");
            DeviceManager deviceManager = new DeviceManager();
            Device device = null;
            foreach (DeviceInfo info in deviceManager.DeviceInfos)
            {
                if (camera!=null)
                if (info.DeviceID == camera.DeviceID)
                {
                    
                    device = info.Connect();
                    int count = device.Items.Count;

                    //takePicture
                    device.ExecuteCommand("{AF933CAC-ACAD-11D2-A093-00C04F72DC3C}");

                    while (device.Items.Count != count + 1)
                        continue;

                    Item wiaItem = device.Items[device.Items.Count];
                    ImageFile imageFile = wiaItem.Transfer("{B96B3CAE-0728-11D3-9D7B-0000F81EF32E}");
                    imageFile.SaveFile(file_name);
                    
                    image = Image.FromFile(file_name);
                    
                    break;
                }
            }



            return image;
        }
    }
}
