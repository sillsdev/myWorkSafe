using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Palaso.Code;

namespace myWorkSafe
{
	public class MirrorMaker
	{
		public event EventHandler<CancellableEventArgs> StartingDirectory;
		public event EventHandler<CancellableEventArgs> StartingFile;

		private readonly string _sourceRootPath;
		private readonly string _parentOfDestinationRootPath;
		List<string> _skippedOrRemovedDirectories;
		private List<FileOrDirectory> _remainingDestItems;

		public MirrorMaker(string sourceRootPath, string parentOfDestinationRootPath)
		{
			_sourceRootPath = sourceRootPath;
			_parentOfDestinationRootPath = parentOfDestinationRootPath;
			_skippedOrRemovedDirectories = new List<string>();
		}

		public void Run()
		{
			var scanner = new DirectoryScanner();
			var sourceItems =scanner.Scan(_sourceRootPath);
			_remainingDestItems = new List<FileOrDirectory>();
			if (Directory.Exists(_parentOfDestinationRootPath))
			{
				_remainingDestItems.AddRange(scanner.Scan(_parentOfDestinationRootPath));
			}

			//nb: this relies on the directories being order from parent-to-children
			foreach (var sourceDirectory in sourceItems.Where(i => i.Type == FileOrDirectory.FileOrDirectoryType.Directory))
			{
				string destination = GetDestination(sourceDirectory.Path);
				if(ShouldCreateDirectory(sourceDirectory))
				{
					Directory.CreateDirectory(GetDestination(sourceDirectory.Path));
				}
				RemoveDestinationItemFromRemainingList(destination);
			}
			foreach (var sourceFile in sourceItems.Where(i => i.Type == FileOrDirectory.FileOrDirectoryType.File))
			{
				string destination = GetDestination(sourceFile.Path);
				HandleFile(sourceFile.Path, destination);
				RemoveDestinationItemFromRemainingList(destination);
			}
			
			HandleRemainingDestinationDirectories();
			HandleRemainingDestinationFiles();
		}

		private void RemoveDestinationItemFromRemainingList(string destination)
		{
			var existing = _remainingDestItems.Find(f => f.Path == destination);
			if(existing !=null)
			{
				_remainingDestItems.Remove(existing);
			}
		}

		private void HandleRemainingDestinationFiles()
		{
			foreach (var file in _remainingDestItems.Where(i => i.Type == FileOrDirectory.FileOrDirectoryType.File))
			{
				var action = GetFileActionFromClient(file.Path, 
								MirrorSituation.FileOnDestinationButNotSource, 
								MirrorAction.DoNothing);
				if(action == MirrorAction.Remove)
				{
					File.Delete(file.Path);
				}
			}
		}

		private void HandleRemainingDestinationDirectories()
		{
			foreach (var directory in _remainingDestItems.Where(i => i.Type == FileOrDirectory.FileOrDirectoryType.Directory))
			{
				var action = GetFileActionFromClient(directory.Path, 
								MirrorSituation.DirectoryOnDestinationButNotSource, 
								MirrorAction.DoNothing);
				if(action == MirrorAction.Remove)
				{
					Directory.Delete(directory.Path, true);
					_skippedOrRemovedDirectories.Add(directory.Path);
				}
				
			}
		}

		private void HandleFile(string source, string dest)
		{
			switch (ShouldCreateOrUpdateFile(source))
			{
				case MirrorAction.Remove:
					if (File.Exists(dest))
					{
						File.Delete(dest);
					}
					break;
				case MirrorAction.DoNothing:
				case MirrorAction.Skip:
					break;
				case MirrorAction.Create:
				case MirrorAction.Update:
					if (File.Exists(dest))
					{
						File.Delete(dest);
					}
					File.Copy(source, dest);
					break;
				default:
					ThrowProgramError("Unexpected enumeration in switch: {0}", source);
					break;
			}
			
		}

		private void ThrowProgramError(params string[] parts)
		{
			var msg = String.Format(parts[0], parts.Skip(1));
			throw new ApplicationException(msg);
		}

		private bool ShouldCreateDirectory(FileOrDirectory sourceDirectory)
		{
			var dest = GetDestination(sourceDirectory.Path);
			var situation = MirrorSituation.DirectoryExists;
			var defaultAction = MirrorAction.DoNothing;
			if(!Directory.Exists(dest))
			{
				 situation = MirrorSituation.DirectoryMissing;
				 defaultAction = MirrorAction.Create;
			}

			var action = GetDirectoryActionFromClient(sourceDirectory, situation, defaultAction);
			Guard.Against(situation ==  MirrorSituation.DirectoryExists && action == MirrorAction.Create, "Told to create an existing directory");
			Guard.Against(situation == MirrorSituation.DirectoryMissing && action == MirrorAction.Remove, "Told to remove an non-existant directory");
			return action == MirrorAction.Create;
		}

		private MirrorAction ShouldCreateOrUpdateFile(string source)
		{
			var dest = GetDestination(source);
			var situation = MirrorSituation.FileIsSame;
			var defaultAction = MirrorAction.DoNothing;
			if (!File.Exists(dest))
			{
				situation = MirrorSituation.FileMissing;
				defaultAction = MirrorAction.Create;
			}
			else
			{
				DateTime sourceWriteTime = File.GetLastWriteTimeUtc(source);
				DateTime destWriteTime = File.GetLastWriteTimeUtc(dest);
				if(sourceWriteTime > destWriteTime)
				{
					situation = MirrorSituation.SourceFileNewer;
					defaultAction = MirrorAction.Update;
				}
				else if (sourceWriteTime < destWriteTime)
				{
					situation = MirrorSituation.SourceFileOlder;
					defaultAction = MirrorAction.Update; //NOTICE, we default to overwriting, even if the dest is newer!
				}
				else
				{
					situation = MirrorSituation.FileIsSame;
				}
			}

			var action = GetFileActionFromClient(source, situation, defaultAction);
			Guard.Against(situation == MirrorSituation.FileIsSame && action == MirrorAction.Create, "Told to create an existing file");
			Guard.Against(situation == MirrorSituation.FileMissing && action == MirrorAction.Remove, "Told to remove an non-existant file");
			Guard.Against(situation == MirrorSituation.FileMissing && action == MirrorAction.Update, "Told to update an non-existant file");
			return action;
		}

		private MirrorSituation GetSituationForFile(string path)
		{
			var dest = GetDestination(path);
			if (!File.Exists(dest))
			{
				return MirrorSituation.FileMissing;
			}
			return MirrorSituation.FileIsSame;
		}


		private MirrorAction GetDirectoryActionFromClient(FileOrDirectory directory, MirrorSituation situation, MirrorAction action)
		{
			var args = new CancellableEventArgs(directory.Path, situation,action);
			EventHandler<CancellableEventArgs> handler = StartingDirectory;
			if (handler != null)
			{
				handler(this, args);
			}
			if(args.PendingAction == MirrorAction.Skip || args.PendingAction ==MirrorAction.Remove)
			{
				_skippedOrRemovedDirectories.Add(directory.Path);
			}
			return args.PendingAction;
		}

		private MirrorAction GetFileActionFromClient(string path, MirrorSituation situation, MirrorAction action)
		{
			//note... this is needed only because we don't currently have a 
			//hierarchical list of things to walk, such that we trim in a more
			//effecient way
			if(_skippedOrRemovedDirectories.Any(d=> path.StartsWith(d)))
			{
				return MirrorAction.Skip;
			}

			var args = new CancellableEventArgs(path, situation, action);
			EventHandler<CancellableEventArgs> handler = StartingFile;
			if (handler != null)
			{
				handler(this, args);
			}
			return args.PendingAction;
		}
		
		private string GetDestination(string path)
		{
			var relativePath = path.Replace(_sourceRootPath, "").Trim(new char[]{Path.DirectorySeparatorChar});
			string destination = Path.Combine(_parentOfDestinationRootPath, relativePath);
			return destination;
		}


	}

	public enum MirrorAction { Skip, Create, Remove,
		DoNothing,
		Update
	}
	public enum MirrorSituation { DirectoryMissing, FileMissing, SourceFileOlder, SourceFileNewer,
		DirectoryExists,
		FileIsSame,
		FileOnDestinationButNotSource,
		DirectoryOnDestinationButNotSource
	}

	public class CancellableEventArgs : EventArgs
	{
		/// <summary>
		/// The path to the file or directory on the source side of the mirror, unless
		/// the situation indicates that we're talking about a destination file which is missing from the source
		/// </summary>
		public string Path;

		/// <summary>
		/// What the engine detected
		/// </summary>
		public MirrorSituation Situation;

		/// <summary>
		/// Read this to see what the engine plans to do, change it to what the engine will do
		/// </summary>
		public MirrorAction PendingAction;

		public CancellableEventArgs(string path, MirrorSituation situation, MirrorAction defaultAction)
		{
			Situation = situation;
			Path = path;
			PendingAction = defaultAction;
		}
	}
}
