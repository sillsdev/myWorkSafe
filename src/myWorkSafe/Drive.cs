using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Palaso.UsbDrive;

namespace myWorkSafe
{
    class Drive
    {
        private readonly UsbDriveInfo _drive;

        public Drive(UsbDriveInfo drive)
        {
            _drive = drive;
        }

        public bool IsAKnownBackupDrive
        {
            get
            {
                
                try
                {
                    return Directory.Exists(BackupControl.GetDestinationFolderPath(_drive.RootDirectory.ToString()));
                }
                catch
                {
                    return false;
                }
            }

        }


        public static IEnumerable<Drive> GetFoundDrives()
        {
            List<UsbDriveInfo> usbDriveInfos = new List<UsbDriveInfo>();
            try
            {
                usbDriveInfos = UsbDriveInfo.GetDrives();
            }
            catch
            {
                //swallow, just return the empty set of drives
            }
            foreach (var foundDrive in usbDriveInfos)
            {
                yield return new Drive(foundDrive);
            }
        }

    }
}
