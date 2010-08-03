using System.Collections.Generic;

namespace myWorkSafe.Groups
{
	public class RawDirectoryGroup : FileGroup
	{
		public RawDirectoryGroup(string name, string rootFolder, 
		                          IEnumerable<string> excludeFilePattern, IEnumerable<string> excludeDirectoryName)
		{
			RootFolder = rootFolder;
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
			get; set;

		}
	}
}