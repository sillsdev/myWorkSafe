using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace myWorkSafe.Groups
{
	public class GroupIniFileReader
	{
		private readonly string _path;

		public GroupIniFileReader(string path)
		{
			_path = path;
		}

		public List<FileGroup> CreateGroups()
		{
			var groups = new List<FileGroup>();

			using (var reader = new ini.IniReader(_path))
			{
				reader.SetCommentDelimiters(new char[] { '#' });
				reader.SetAssignDelimiters(new char[] { '=' });
				while (reader.MoveToNextSection())
				{
					var group = new FileGroupFromIniSection();
					group.Name = reader.Name;

					while (reader.MoveToNextKey())
					{
						switch (reader.Name)
						{
							case "name":
								group.Name = reader.Value;
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
								break;

						}
					}
					groups.Add(group);
				}

			}
			return groups;
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

			return path;
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
