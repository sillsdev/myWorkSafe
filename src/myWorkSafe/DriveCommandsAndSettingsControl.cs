using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace myWorkSafe
{
    public partial class DriveCommandsAndSettingsControl : UserControl
    {
        public DriveCommandsAndSettingsControl(string drivePath)
        {
            InitializeComponent();
            _driveLabel.Text = drivePath;
        }
    }
}
