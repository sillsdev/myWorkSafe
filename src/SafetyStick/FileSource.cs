using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Synchronization.Files;
using Microsoft.Win32;
using Palaso.Extensions;

namespace myWorkSafe
{
	public abstract class FileSource
	{
		public FileSource(string sourceGuid, string destGuid)
		{
			Filter = new FileSyncScopeFilter();
			
			//this could bear further research, but so far, it looks like helping the MS file sync provider
			//with these guids and persisten metadata files, if anything, just leads to behavior contrary
			//to what we want for simple backup.  
			SourceGuid = Guid.NewGuid();
			DestGuid = Guid.NewGuid();
			//	SourceGuid = new Guid(sourceGuid);
			//	DestGuid = new Guid(destGuid);
			Filter.AttributeExcludeMask = FileAttributes.Hidden | FileAttributes.Temporary | FileAttributes.System;
			Filter.FileNameExcludes.Add("parent.lock");//a mozilla thing (e.g. thunderbird)
		}

		public enum DispositionChoice {Waiting=0, Calculating, WillBeBackedUp, NotEnoughRoom,
			Synchronizing,
			WasBackedUp,
			WillBeDeleted,
			Hide
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
		public string SourceTempMetaFile;
		public string DestTempMetaFile;

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

		public bool GetIsRelevantOnThisMachine()
		{
			return Directory.Exists(RootFolder);
		}

		public bool ShouldSkipDirectory(FileData directoryData)
		{
			//we bracket with spaces so that we don't match substrings
			string value = " "+directoryData.Name.ToLower()+" ";
			if(Filter.SubdirectoryExcludes.Any(s => (" "+s.ToLower()+" ") ==value))
				return true;
			return ShouldSkip(directoryData.RelativePath);
		}

		public bool ShouldSkip(string relativePath)
		{
			string value = "//"+relativePath.ToLower()+"//";
			var parts = relativePath.Split(new char[] {Path.DirectorySeparatorChar});
			foreach (var part in parts)
			{
				if(Filter.SubdirectoryExcludes.Any(s => s.ToLower() == part.ToLower()))
					return true;
			}
			return false;
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

	/*
	 There doesn't appear to be a way to know where the backups go, nor identify them, as the
	 * are simply zips.
	public class TEBackupFiles : FileSource
	{
		public TEBackupFiles()
			: base("472D2CAB-C4FE-4f80-A21E-F9ECA725F6C3", "E732ADF7-81F4-41ef-B664-BE910593288E")
		{
			Name = "TE Backup Files";
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
	*
	 */

	public class OtherFiles : FileSource
	{
		public OtherFiles()
			: base("98BCC0DE-C329-4bdd-9A03-ECEC16A588F8", "97BCC0DE-C329-4bdd-9A03-ECEC16A588F8")
		{
			Name = "Other in My Documents";
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
	
	public class WindowsLiveMail : FileSource
	{
		public WindowsLiveMail()
			: base("1239CDE9-3355-4d67-9826-1FC4376462EA", "1249CDE9-3355-4d67-9826-1FC4376462EA")
		{
			Name = "Windows Live Mail";
			Filter.SubdirectoryExcludes.Add("news.sil.org.pg");
			Filter.SubdirectoryExcludes.Add("Your Feeds");
			//Filter.SubdirectoryExcludes.Add("Deleted Items");
		}

		public override string RootFolder
		{
			get
			{
				var localData= Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				return localData.CombineForPath(@"Microsoft\Windows Live Mail");
			}
		}
	}
	
	public class ThunderbirdMail : FileSource
	{
		public ThunderbirdMail()
			: base("1239CDE9-3355-4d67-9826-1FC4376462EA", "1249CDE9-3355-4d67-9826-1FC4376462EA")
		{
			Name = "Thunderbird Mail";
			Filter.FileNameExcludes.Add("global-messages-db.sqlite");//just a big index
			Filter.SubdirectoryExcludes.Add("News");
			Filter.SubdirectoryExcludes.Add("extensions");
		}

		public override string RootFolder
		{
			get
			{
				var appData= Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				//on my win7 machine, this is in the "roaming" dir.
				return appData.CombineForPath(@"Thunderbird");
			}
		}
	}
	public class MyPictures : FileSource
	{
		public MyPictures()
			: base("2179CDE9-3355-4d67-9826-1FC4376462EA", "1119CDE9-3355-4d67-9826-1FC4376462EA")
		{
			Name = "My Pictures";
			Filter.SubdirectoryExcludes.Add("Sample Pictures");
		}

		public override string RootFolder
		{
			get
			{
				return Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
			}
		}
	}

	public class MyMusic : FileSource
	{
		public MyMusic()
			: base("3339CDE9-3355-4d67-9826-1FC4376462EA", "4449CDE9-3355-4d67-9826-1FC4376462EA")
		{
			Name = "My Music";
			Filter.SubdirectoryExcludes.Add("Sample Music");
		}

		public override string RootFolder
		{
			get
			{
				return Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
			}
		}
	}

	public class MyVideos : FileSource
	{
		public MyVideos()
			: base("5B32BBB0-1F19-4f5a-9F35-651DB56DA010", "5532BBB0-1F19-4f5a-9F35-651DB56DA010")
		{
			Name = "Videos";
			Filter.SubdirectoryExcludes.Add("Sample Videos");
		}

		public override string RootFolder
		{
			get
			{
				var win7= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "My Videos");
				if(Directory.Exists(win7))
					return win7;
	
				var xp= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Videos");
				return xp;
			}
		}
	}
	public class RawDirectorySource : FileSource
	{
		private string _path;

		public RawDirectorySource(string name, string rootFolder, 
			IEnumerable<string> excludeFilePattern, IEnumerable<string> excludeDirectoryName)
			:base(null,null)
		{
			_path = rootFolder;
			Name = name;
			if (null != excludeFilePattern)
			{
				foreach (var pattern in excludeFilePattern)
				{
					Filter.FileNameIncludes.Add(pattern);
				}
			}
			if (null != excludeDirectoryName)
			{
				foreach (var dir in excludeDirectoryName)
				{
					Filter.SubdirectoryExcludes.Add(dir);
				}
			}
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
