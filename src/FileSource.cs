using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Synchronization.Files;
using Microsoft.Win32;

namespace SafetyStick
{
	public abstract class FileSource
	{
		public FileSource(string sourceGuid, string destGuid)
		{
			Filter = new FileSyncScopeFilter();
			SourceGuid = new Guid(sourceGuid);
			DestGuid = new Guid(destGuid);
		}

		public enum DispositionChoice {Waiting=0, Calculating, WillBeBackedUp, WillBeSkipped,
			Synchronizing,
			WasBackedUp
		}

		public DispositionChoice Disposition;

		public string Name;
		public abstract string RootFolder{ get;}


		public FileSyncScopeFilter Filter;
		public long NetChangeInBytes;
		public int NewFileCount;
		public int UpdateFileCount;
		public int DeleteFileCount;
		public Guid SourceGuid;
		public Guid DestGuid;

		public void ClearStatistics()
		{
			Disposition = DispositionChoice.Waiting;
			NetChangeInBytes = 0;
			NewFileCount = 0;
			DeleteFileCount = 0;
			UpdateFileCount = 0;
		}

		public string GetDestinationSubFolder(string destinationRoot)
		{
			var dir = Path.Combine(destinationRoot, Name);
			if(!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
			return dir;
		}
	}


	public class ParatextFiles :FileSource
	{

		public ParatextFiles()
			: base("11BCC0DE-C329-4bdd-9A03-ECEC16A588F8", "22BCC0DE-C329-4bdd-9A03-ECEC16A588F8")
		{
			Name = "Paratext";
			Filter.SubdirectoryExcludes.Add("TNE Notes Database");
			Filter.SubdirectoryExcludes.Add("cms");
		}

		public override string RootFolder
		{
			get
			{
				return (string) Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\ScrChecks\1.0", "Settings_Directory", @"C:\my paratext projects");
				//return @"C:\My Paratext Projects";
			}
		}
	}

	public class WeSayFiles : FileSource
	{
		public WeSayFiles()
			: base("472D2CAB-C4FE-4f80-A28E-F9ECA725F6C3", "E732ADF7-81F4-4aef-B664-BE910593288E")
		{
			Name = "WeSay";
			Filter.FileNameExcludes.Add("*.pdf");
		}

		public override string RootFolder
		{
			get
			{
				return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WeSay");
			}
		}
	}

	public class DevChorusFiles : FileSource
	{
		public DevChorusFiles()
			: base("472D2CAB-C4FE-4f80-A21E-F9ECA725F6C3", "E732ADF7-81F4-41ef-B664-BE910593288E")
		{
			Name = "DevChorusFiles";
			Filter.FileNameExcludes.Add("*.dll");
			Filter.FileNameExcludes.Add("*.exe");
		}

		public override string RootFolder
		{
			get
			{
				return @"c:\dev\chorus";
			}
		}
	}

	public class OtherFiles : FileSource
	{
		public OtherFiles()
			: base("98BCC0DE-C329-4bdd-9A03-ECEC16A588F8", "97BCC0DE-C329-4bdd-9A03-ECEC16A588F8")
		{
			Name = "Other";
			Filter.FileNameExcludes.Add("*.exe");
			Filter.FileNameExcludes.Add("*.msi");
			Filter.FileNameExcludes.Add("*.dll");
			Filter.FileNameExcludes.Add("*.jpg");
			Filter.FileNameExcludes.Add("*.mp3");
			Filter.FileNameExcludes.Add("*.wav");
			Filter.FileNameExcludes.Add("*.avi");
			Filter.FileNameExcludes.Add("*.wmv");
			Filter.FileNameExcludes.Add("*.mov");
		}

		public override string RootFolder
		{
			get
			{
				return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			}
		}
	}

	public class OtherDesktopFiles : FileSource
	{
		public OtherDesktopFiles()
			: base("9899C0DE-C329-4bdd-9A03-ECEC16A588F8", "97B990DE-C329-4bdd-9A03-ECEC16A588F8")
		{
			Name = "Other Desktop";
			Filter.FileNameExcludes.Add("*.exe");
			Filter.FileNameExcludes.Add("*.msi");
			Filter.FileNameExcludes.Add("*.dll");
			Filter.FileNameExcludes.Add("*.jpg");
			Filter.FileNameExcludes.Add("*.mp3");
			Filter.FileNameExcludes.Add("*.wav");
			Filter.FileNameExcludes.Add("*.avi");
			Filter.FileNameExcludes.Add("*.wmv");
			Filter.FileNameExcludes.Add("*.mov");
		}

		public override string RootFolder
		{
			get
			{
				return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
			}
		}
	}
	public class FileGroup : FileSource
	{
		private string _path;
		public FileGroup(string path)	//todo: what if this is used for more than one path?
			: base("33BCC0DE-C329-4bdd-9A03-ECEC16A588F8", "44BCC0DE-C329-4bdd-9A03-ECEC16A588F8")
		{
			_path = path;
		}



		public override string RootFolder
		{
			get
			{
				return _path;
			}
		}
	}
}
