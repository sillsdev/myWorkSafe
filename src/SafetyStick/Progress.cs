using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace myWorkSafe
{
    public interface IProgress
    {
        void WriteStatus(string message, params object[] args);
        void WriteMessage(string message, params object[] args);
        void WriteWarning(string message, params object[] args);
        void WriteException(Exception error);
        void WriteError(string message, params object[] args);
        void WriteVerbose(string message, params object[] args);
        bool ShowVerbose {set; }
        bool CancelRequested { get;  set; }
		bool ErrorEncountered { get; set; }
    }

    public class NullProgress : IProgress
    {
        public void WriteStatus(string message, params object[] args)
        {
            
        }

        public void WriteMessage(string message, params object[] args)
        {
        }

        public void WriteWarning(string message, params object[] args)
        {
        }

        public void WriteException(Exception error)
        {
            
        }

        public void WriteError(string message, params object[] args)
        {
			ErrorEncountered = true;
        }

        public void WriteVerbose(string message, params object[] args)
        {
            
        }

        public bool ShowVerbose
        {
            get { return false; }
            set {  }
        }

        public bool CancelRequested { get; set; }

    	public bool ErrorEncountered{get;set;}
    }

    public class MultiProgress : IProgress, IDisposable
    {
        private readonly List<IProgress> _progressHandlers=new List<IProgress>();
        private bool _cancelRequested;

        public MultiProgress(IEnumerable<IProgress> progressHandlers)
        {
            _progressHandlers.AddRange(progressHandlers);
        }


        public bool CancelRequested
        {

            get
            {
                foreach (var handler in _progressHandlers)
                {
                    if (handler.CancelRequested)
                        return true;
                }
                return _cancelRequested;
            }
            set
            {
                _cancelRequested = value;
            }
        }

		public bool ErrorEncountered
		{
			get; set;
		}

    	public void WriteStatus(string message, params object[] args)
        {
            foreach (var handler in _progressHandlers)
            {
                handler.WriteStatus(message, args);
            }
        }

        public void WriteMessage(string message, params object[] args)
        {
            foreach (var handler in _progressHandlers)
            {
                handler.WriteMessage(message, args);
            }
        }

        public void WriteWarning(string message, params object[] args)
        {
            foreach (var handler in _progressHandlers)
            {
                handler.WriteWarning(message, args);
            }
        }

        public void WriteException(Exception error)
        {
             foreach (var handler in _progressHandlers)
            {
                handler.WriteException(error);
            }
        	ErrorEncountered = true;
        }

        public void WriteError(string message, params object[] args)
        {
            foreach (var handler in _progressHandlers)
            {
                handler.WriteError(message, args);
            }
			ErrorEncountered = true;
        }

        public void WriteVerbose(string message, params object[] args)
        {
            foreach (var handler in _progressHandlers)
            {
                handler.WriteVerbose(message, args);
            }            
        }

        public bool ShowVerbose
        {
            set //review: the best policy isn't completely clear here
            {
                foreach (var handler in _progressHandlers)
                {
                    handler.ShowVerbose = value;
                }
            }
        }

        public void Dispose()
        {
            foreach (var handler in _progressHandlers)
            {
                var d = handler as IDisposable;
                if(d!=null)
                    d.Dispose();
            }
        }

        public void Add(IProgress progress)
        {
            _progressHandlers.Add(progress);
        }
    }

    public class ConsoleProgress : IProgress, IDisposable
    {
        public static int indent = 0;
        private bool _verbose;

        public ConsoleProgress()
        {
        }

        public ConsoleProgress(string mesage, params string[] args)
        {
            WriteStatus(mesage, args);
            indent++;
        }
		public bool ErrorEncountered { get; set; }

        public void WriteStatus(string message, params object[] args)
        {
#if MONO
            Console.Write("                          ".Substring(0, indent*2));
            Console.WriteLine(string.Format(message, args));
#else
            Debug.Write("                          ".Substring(0, indent * 2));
            Debug.WriteLine(string.Format(message, args));
#endif
        }

        public void WriteMessage(string message, params object[] args)
        {
            WriteStatus(message, args);
           
        }


        public void WriteWarning(string message, params object[] args)
        {
            WriteStatus("Warning: "+ message, args);
        }

        public void WriteException(Exception error)
        {
            WriteError("Exception: ");
            WriteError(error.Message);
            WriteError(error.StackTrace);

            if (error.InnerException != null)
            {
                ++indent;
                WriteError("Inner: ");
                WriteException(error.InnerException);
                --indent;
            }
			ErrorEncountered = true;
        }
    

        public void WriteError(string message, params object[] args)
        {
            WriteStatus("Error: "+ message, args);
			ErrorEncountered = true;
        }

        public void WriteVerbose(string message, params object[] args)
        {
            if(!_verbose)
                return;
            var lines = String.Format(message, args);
            foreach (var line in lines.Split('\n'))
            {
                WriteStatus("    " + line);
            }

        }

        public bool ShowVerbose
        {
            set { _verbose = value; }
        }

        public bool CancelRequested { get; set; }

        ///<summary>
        ///Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        ///</summary>
        ///<filterpriority>2</filterpriority>
        public void Dispose()
        {
            if(indent>0)
                indent--;
        }



    }

    /// <summary>
    /// Just conveys status, not all messages
    /// </summary>
    public class LabelStatus : IProgress
    {
        private Label _box;

        public LabelStatus(Label box)
        {
            _box = box;
        }

        public bool ShowVerbose
        {
            set { }
        }
		public bool ErrorEncountered { get; set; }

        public bool CancelRequested { get; set; }


        public void WriteStatus(string message, params object[] args)
        {
            try
            {
                _box.Invoke(new Action(() =>
                {
                    _box.Text = String.Format(message + Environment.NewLine, args);
                }));
            }
            catch (Exception)
            {

            }
        }

        public void WriteMessage(string message, params object[] args)
        {
            
        }

        public void WriteWarning(string message, params object[] args)
        {
        }

        public void WriteException(Exception error)
        {
            WriteError("Error");
			ErrorEncountered = true;
        }

        public void WriteError(string message, params object[] args)
        {
            WriteStatus(message,args);
			ErrorEncountered = true;
        }

        public void WriteVerbose(string message, params object[] args)
        {
            
        }
   
    }

    public class TextBoxProgress : GenericProgress
    {
        private RichTextBox _box;

        public TextBoxProgress(RichTextBox box)
        {
            _box = box;
            _box.Multiline = true;
        }


        public override void WriteMessage(string message, params object[] args)
        {
            try
            {
             // if (_box.InvokeRequired)
                _box.Invoke(new Action( ()=>
                {
                    _box.Text += "                          ".Substring(0, indent * 2);
                    _box.Text += String.Format(message + Environment.NewLine, args);
                }));
            }
            catch (Exception)
            {

            }
//            _box.Invoke(new Action<TextBox, int>((box, indentX) =>
//            {
//                box.Text += "                          ".Substring(0, indentX * 2);
//                box.Text += String.Format(message + Environment.NewLine, args);
//            }), _box, indent);
        }


        public override void WriteException(Exception error)
        {
            WriteError("Exception: ");
            WriteError(error.Message);
            WriteError(error.StackTrace);
            if (error.InnerException != null)
            {
                ++indent;
                WriteError("Inner: ");
                WriteException(error.InnerException);
                --indent;
            }
        }

       
    }

    public class StringBuilderProgress : GenericProgress
    {
        private StringBuilder _builder = new StringBuilder();
      
        public override void WriteMessage(string message, params object[] args)
        {
            _builder.Append("                          ".Substring(0, indent * 2));
            _builder.AppendFormat(message+Environment.NewLine, args);
        }

        public string Text
        {
            get { return _builder.ToString(); }
        }

        public void Clear()
        {
            _builder = new StringBuilder();
        }
    }

    public class StatusProgress : IProgress
    {

        public string LastStatus { get; private set; }
        public string LastWarning { get; private set; }
        public string LastError { get; private set; }
        public bool CancelRequested { get; set; }
        public bool WarningEncountered { get { return !string.IsNullOrEmpty(LastWarning); } }
		public bool ErrorEncountered { get { return !string.IsNullOrEmpty(LastError); }
			set { }
		}


       public  void WriteStatus(string message, params object[] args)
        {
            LastStatus = string.Format(message, args);
        }
        public void WriteWarning(string message, params object[] args)
        {
            LastWarning = string.Format(message, args);
            LastStatus = LastWarning;
        }

        public void WriteException(Exception error)
        {
            WriteError(error.Message);
        }

        public void WriteError(string message, params object[] args)
        {
            LastError = string.Format(message, args);
            LastStatus = LastError;
        }

        public void WriteMessage(string message, params object[] args)
        {
        }
        
        public void WriteVerbose(string message, params object[] args)
        {
        }

        public bool ShowVerbose
        {
            set {  }
        }

        public bool WasCancelled
        {
            get
            {
                if(LastWarning!=null)
                    return LastWarning.ToLower().Contains("cancelled");
                return false;
            }//improve: this is pretty flimsy
        }

        public void Clear()
        {
            LastError = LastWarning =LastStatus = string.Empty;
        }
    }

    public abstract class GenericProgress : IProgress
    {
        public int indent = 0;
        private bool _verbose;

        public GenericProgress()
        {
        }
        public bool CancelRequested { get; set; }
        public abstract void WriteMessage(string message, params object[] args);
		public bool ErrorEncountered { get; set; }

        public void WriteStatus(string message, params object[] args)
        {
            WriteMessage(message, args);
        }

        public void WriteWarning(string message, params object[] args)
        {
            WriteMessage("Warning: " + message, args);
        }

        public virtual void WriteException(Exception error)
        {
            WriteError(error.Message);
        }

        public void WriteError(string message, params object[] args)
        {
            WriteMessage("Error:" + message, args);
        }

        public void WriteVerbose(string message, params object[] args)
        {
            if(!_verbose)
                return;
            var lines = String.Format(message, args);
            foreach (var line in lines.Split('\n'))
            {

                WriteMessage("   " + line);
            }

        }

        public bool ShowVerbose
        {
            set { _verbose = value; }
        }
    }
}