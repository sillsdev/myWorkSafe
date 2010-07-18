using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Synchronization.Files;

namespace SafeStick
{
	public abstract class FileGroup
	{
		public FileGroup()
		{
			Filter = new FileSyncScopeFilter();
		}

		public string Name;
		public abstract string RootFolder{ get;}

		public FileSyncScopeFilter Filter;
	}


	public class ParatextFiles :FileGroup
	{
		public ParatextFiles()
		{
			Name = "Paratext";
			Filter.SubdirectoryExcludes.Add("TNE Notes Database");
			Filter.SubdirectoryExcludes.Add("cms");
		}

		public override string RootFolder
		{
			get
			{
				//todo: get from registry
				return @"C:\My Paratext Projects";
			}
		}
	}
}
