using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using myWorkSafe.ini;

namespace myWorkSafe.Groups
{
	public class GroupIniFileReader
	{
		private readonly string _path;

		public GroupIniFileReader(string path)
		{
			_path = path;
		}

		/// <summary>
		/// For tests's convenience
		/// </summary>
		public List<FileGroup> CreateGroups()
		{
			var groups = new List<FileGroup>();
			CreateGroups(groups);
			return groups;
		}

		/// <summary>
		/// UPdates the groups in the list (so it can be 
		/// called multiple times with different ini's, and the last one wins)
		/// </summary>
		/// <param name="groups"></param>
		public void CreateGroups(List<FileGroup> groups)
		{
			bool inSecondRun = groups.Count > 0;
			int nextInsertLocation = 0;

			using (var reader = new ini.IniReader(_path))
			{
				reader.AcceptNoAssignmentOperator = true;
				reader.SetCommentDelimiters(new char[] { '#' });
				reader.SetAssignDelimiters(new char[] { '=' });
				reader.AcceptCommentAfterKey = true;//todo: doesn't work
				reader.MoveToNextSection();
				while (reader.ReadState == ini.IniReadState.Interactive)
				{
					if(reader.Name=="Settings")
					{
						if(nextInsertLocation >0)
						{
							Palaso.Reporting.ErrorReport.NotifyUserOfProblem("The [Settings] section needs to be at the top of the file in '{0}'.",
												 _path);
						}
						ReadSettingsSection(reader, groups);
						continue;
					}
					FileGroupFromIniSection group = (FileGroupFromIniSection) groups.FirstOrDefault(g => g.SectionName == reader.Name);
					if (group == null)
					{
						group = new FileGroupFromIniSection();
						groups.Insert(nextInsertLocation, group);//this gives new groups in an override ini priority
						nextInsertLocation++;

						group.SectionName = reader.Name;
						group.Name = reader.Name;
					}



					while (reader.MoveToNextKey())//nb: unfortnately, this ini libary will move us to a new section as well
					{
						switch (reader.Name)
						{
							case "name":
								group.Name = reader.Value;
								break;
							case "skip":
								group.RootFolder = string.Empty;
								break;
							case "rootFolder":
								if(!string.IsNullOrEmpty(group.RootFolder))
								{
									if(Directory.Exists(group.RootFolder))
										break; //we already have a good one
								}
								group.RootFolder = ProcessPath(reader.Value);
								break;
							case "excludeFile":
								group.AddExcludeFilePattern(reader.Value);
								break;
							case "excludeFolder":
								group.AddExcludeFolder(reader.Value);
								break;
							default:
								Palaso.Reporting.ErrorReport.NotifyUserOfProblem("The program doesn't understand this '{0}', in the file {1}",
								                                                 reader.Name, _path);
								break;

						}
					}
				}

			}
		}

		private void ReadSettingsSection(IniReader reader, List<FileGroup> groups)
		{
			while (reader.MoveToNextKey())//nb: unfortnately, this ini libary will move us to a new section as well
			{
				switch (reader.Name)
				{
					case "clearAllPreviousSettings":
						groups.Clear();
						break;
					default:
						Palaso.Reporting.ErrorReport.NotifyUserOfProblem("The program doesn't understand this '{0}', in the Settings section of file {1}",
																		 reader.Name, _path);
						break;

				}
			}
		}

		public static string ProcessPath(string path)
		{
			if (path.Contains("HKEY_LOCAL_MACHINE"))
				return ProcessPathWithRegistry(path);

			path= path.Replace("$MyDocuments$", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
			path = path.Replace("$Desktop$", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
			path = path.Replace("$LocalApplicationData$", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
			path = path.Replace("$CommonApplicationData$", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
			path = path.Replace("$ApplicationData$", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

			path = path.Replace("$MyMusic$", Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
			path = path.Replace("$MyPictures$", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));

			if (path.Contains("$MyVideos"))
			{              
				path = path.Replace("$MyVideos$", GetVideosPath());
			}
			return path;
		}

		private static string GetVideosPath()
		{
			var win7 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "My Videos");
			if (Directory.Exists(win7))
				return win7;

			var xp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Videos");
			return xp;
		}

		private static string ProcessPathWithRegistry(string path)
		{
			string[] parts = path.Split(new[]{','});
			if(parts.Length == 1)
			{
				return (string)Registry.GetValue(parts[0], null /*default*/,null);
			}
			return (string)Registry.GetValue(parts[0], parts[1],null);
		}
	}
}
