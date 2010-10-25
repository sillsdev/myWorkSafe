using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace myWorkSafe.Groups
{
	public abstract class FileGroup
	{
		public FileGroup()
		{
			Filter = new FileSyncScopeFilter();
			
			//this could bear further research, but so far, it looks like helping the MS file sync provider
			//with these guids and persisten metadata files, if anything, just leads to behavior contrary
			//to what we want for simple backup.  
			SourceGuid = Guid.NewGuid();
			DestGuid = Guid.NewGuid();

			Filter.AttributeExcludeMask = FileAttributes.Hidden | FileAttributes.Temporary | FileAttributes.System;
		}

		public enum DispositionChoice {Hide=0, Waiting, Calculating, WillBeBackedUp, NotEnoughRoom,
			Synchronizing,
			WasBackedUp,
			WillBeDeleted,
			Cancelled
		}

		public DispositionChoice Disposition;

		public string Name;
		public string RootFolder
		{
			get { return _rootFolder; }
			set
			{
				_rootFolder = value;
				if(string.IsNullOrEmpty(_rootFolder))
				{
					Disposition = DispositionChoice.Hide;
				}
				else if(Directory.Exists(_rootFolder))
				{
					Disposition = DispositionChoice.Waiting;
				}
				else
				{
					Disposition = DispositionChoice.Hide;
				}

			//enhance... ideally we'd return hide if the directory was there but no
			//relevant files were there. E.g. if we had MyVideos, but the only
			//thing in there was "Sample Videos", which are filtered out, we
			//would like to not show this group.
			}
		}


		public FileSyncScopeFilter Filter;
		public long NetChangeInBytes;
		public int NewFileCount;
		public int UpdateFileCount;
		public int DeleteFileCount;
		public Guid SourceGuid;
		public Guid DestGuid;

		private string _rootFolder="??";
		private string _sourceTempMetaFile;
		private string _destTempMetaFile;
		public string SectionName { get; set; }

		public void ClearStatistics()
		{
			if (Disposition == DispositionChoice.WillBeBackedUp)
			{
				Disposition = DispositionChoice.Waiting;
			}
			NetChangeInBytes = 0;
			NewFileCount = 0;
			DeleteFileCount = 0;
			UpdateFileCount = 0;

		}

		/// <summary>
		/// When a file disappears from the source, should the backup also remove it?
		/// </summary>
		public bool NormallyPropogateDeletions { get; set; }

		public string SourceTempMetaFile
		{
			get {
				if(_sourceTempMetaFile==null)
				{
					_sourceTempMetaFile = Path.GetTempFileName();
				}
				return _sourceTempMetaFile;
			}
		}
		public string DestTempMetaFile
		{
			get
			{
				if (_destTempMetaFile == null)
				{
					_destTempMetaFile = Path.GetTempFileName();
				}
				return _destTempMetaFile;
			}
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


		public bool ShouldSkipSubDirectory(string path)
		{
			//we bracket with spaces so that we don't match substrings
			string value = " " + path.ToLower() + " ";
			return Filter.SubdirectoryExcludes.Any(s => (" " + s.ToLower() + " ") == value);
		}

		public bool ShouldSkipFile(string relativePath)
		{
			string value = "//"+relativePath.ToLower()+"//";
			var parts = relativePath.Split(new char[] {Path.DirectorySeparatorChar});
			foreach (var part in parts)
			{
				if(Filter.SubdirectoryExcludes.Any(s => s.ToLower() == part.ToLower()))
					return true;
			}

            if (Filter.FileNameExcludes.Any(pattern => parts.Last().EndsWith(pattern.Replace("*.",""))))
                return true;

			return false;
		}
	}

	public class FileSyncScopeFilter
	{
		public FileSyncScopeFilter()
		{
			SubdirectoryExcludes = new List<string>();
			FileNameExcludes = new List<string>();
		}
		public List<string> SubdirectoryExcludes;
		public FileAttributes AttributeExcludeMask;
		public List<string> FileNameExcludes;
	}

	/*
	public class ParatextFiles :FileGroup
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

	public class WeSayFiles : FileGroup
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


	public class OtherFiles : FileGroup
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

	public class OtherDesktopFiles : FileGroup
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
	
	public class WindowsLiveMail : FileGroup
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
	
	public class ThunderbirdMail : FileGroup
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
	public class MyPictures : FileGroup
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

	public class MyMusic : FileGroup
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

	public class MyVideos : FileGroup
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
	 */
}
