﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace myWorkSafe
{
    public partial class LogBox : UserControl, IProgress
    {
    	private Action<IProgress> _getDiagnosticsMethod;

    	public LogBox()
        {
            InitializeComponent();

            //On some machines (winXP?) we get in trouble if we don't make sure these boxes are visible before
            //they get invoked() to. 
            _box.CreateControl();
    	    _verboseBox.Visible = true;
            _verboseBox.CreateControl();
        }


        public void WriteStatus(string message, params object[] args)
        {
            WriteMessage(message, args);
        }

        public void WriteMessage(string message, params object[] args)
        {
            Write(Color.Black, message, args);
        }

        private void Write(Color color, string message, params object[] args)
        {
//            try
//            {
                 SafeInvoke(_box,new Action(() =>
                 {
                                _box.SelectionStart = _box.Text.Length;
                                _box.SelectionColor = color;
                                _box.AppendText(String.Format(message + Environment.NewLine, args));
                                       }));

                SafeInvoke(_verboseBox,new Action(() =>
                {
                    _verboseBox.SelectionStart = _verboseBox.Text.Length;
                    _verboseBox.SelectionColor = color;
                    _verboseBox.AppendText(String.Format(message + Environment.NewLine, args));
                })); 
//            }
//            catch (Exception)
//            {
//
//            }
        }

        public void WriteWarning(string message, params object[] args)
        {
            Write(Color.Blue, "Warning: " + message, args);
        }

		/// <summary>
		/// This is a callback the client can set to soemthing which will then generate
		/// Write() calls.  If it is set, the user sees a "Run diagnostics" menu item.
		/// </summary>
		public Action<IProgress> GetDiagnosticsMethod
		{
			get
			{ return _getDiagnosticsMethod; }
			set
			{
				_getDiagnosticsMethod = value;
				_runDiagnostics.Visible = (_getDiagnosticsMethod != null);
			}
		}
        public void WriteException(Exception error)
        {
            Write(Color.Red, "Exception: " +error.Message);
            WriteVerbose(error.StackTrace);
            if (error.InnerException != null)
            {
                WriteError("Inner--> ");
                WriteException(error.InnerException);
            }
        }

        public void WriteError(string message, params object[] args)
        {
            Write(Color.Red,Environment.NewLine+ "Error:" + message, args);
			SafeInvoke(_reportProblemLink, new Action(() =>
			{
				_reportProblemLink.Visible = true;
			}));

        	ErrorEncountered = true;
        }

		public bool ErrorEncountered
		{
			get;
			set;
		}

        /// <summary>
        /// This is an attempt to avoid a mysterious crash (B.Waters) where the invoke was happening before the window's handle had been created
        /// </summary>
		public void SafeInvoke(Control box, Action action)
		{


                if (!box.IsHandleCreated)
                {
                    Debug.Fail("In release build, would have given up writing this message, because the destination control isn't built yet.");
                    return;
                }
                
            
		    box.Invoke(action);
		}

        public void WriteVerbose(string message, params object[] args)
        {
            SafeInvoke(_verboseBox,new Action(() =>
            {
                _verboseBox.SelectionStart = _verboseBox.Text.Length;
                _verboseBox.SelectionColor = Color.DarkGray;
                _verboseBox.AppendText(String.Format(message + Environment.NewLine, args));
            })); 
        }

        public bool ShowVerbose
        {
            set { _showDetailsMenu.Checked = value; }
        }

        public bool CancelRequested
        {
            get { return false; }
            set {  }
        }

        private void _showDetails_CheckedChanged(object sender, EventArgs e)
        {
            _verboseBox.Visible = _showDetailsMenu.Checked;
            _box.Visible = !_showDetailsMenu.Checked;

#if MONO  //mono (2.0?) doesn't update the size of the box when invisible, apparently
            if (_showDetailsMenu.Checked)
            {
                _verboseBox.Bounds = _box.Bounds;
            }
            else
            {
                _box.Bounds = _verboseBox.Bounds;
            }
#endif
        }

        private void _copyToClipboardLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
#if MONO
//at least on Xubuntu, getting some rtf on the clipboard would mean that when you pasted, you'd see rtf
            Clipboard.SetText(_verboseBox.Text);
#else
            Clipboard.SetText(_verboseBox.Rtf, TextDataFormat.Rtf);
#endif
        }

        public void Clear()
        {
            _box.Text = "";
            _verboseBox.Text = "";
        }

        

        private void copyToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _copyToClipboardLink_LinkClicked(sender, null);
        }

        private void LogBox_BackColorChanged(object sender, EventArgs e)
        {
            this._menu.BackColor =  this.BackColor;
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

		private void _reportProblemLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			if(!ReportError(_verboseBox.Text))
			{
				Clipboard.SetText(_verboseBox.Text);
				MessageBox.Show(
					"Information on what happened has been copied to your clipboard. Please email it to the developers of the program you are using.");
			}
		}

		private static bool ReportError(string msg)
		{
			try
			{
				Palaso.Reporting.ErrorReport.ReportNonFatalMessageWithStackTrace(msg);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

    	private void OnRunDiagnosticsClick(object sender, EventArgs e)
    	{
			if (GetDiagnosticsMethod != null)
			{
				ShowVerbose = true;
				GetDiagnosticsMethod(this);
			}
    	}
    }
}
