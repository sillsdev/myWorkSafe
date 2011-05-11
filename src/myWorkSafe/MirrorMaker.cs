using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Palaso.Code;

namespace myWorkSafe
{
	public class MirrorMaker :IDisposable
	{
		public event EventHandler<MirrorEventArgs> StartingDirectory;
		public event EventHandler<MirrorEventArgs> StartingFile;
		public event EventHandler<ItemHandlingErrorArgs> ItemHandlingError;

		private  string _sourceRootPath;
		private  string _parentOfDestinationRootPath;
		List<string> _skippedOrRemovedDirectories;
		private List<FileOrDirectory> _remainingDestItems;
		private bool _cancelRequested;
		public bool WasCancelled;

		//todo: remove
		public MirrorMaker(string sourceRootPath, string parentOfDestinationRootPath)
		{
			_sourceRootPath = sourceRootPath;
			_parentOfDestinationRootPath = parentOfDestinationRootPath;
			_skippedOrRemovedDirectories = new List<string>();
		}


		//todo: remove
		public void Run()
		{
			Run(_sourceRootPath, _parentOfDestinationRootPath);
		}

		public MirrorMaker()
		{
			_skippedOrRemovedDirectories = new List<string>();
		}
		public void Run(string sourceRootPath, string parentOfDestinationRootPath)
		{
			_sourceRootPath = sourceRootPath;
			_parentOfDestinationRootPath = parentOfDestinationRootPath;

			var scanner = new DirectoryScanner();
			scanner.OnError += (sender, args) => RaiseItemHandlingError(args.Path, MirrorAction.Skip, args.Error);
			if(_cancelRequested)
			{
				WasCancelled = true;
				return;
			}

			var sourceItems =scanner.Scan(_sourceRootPath);
			_remainingDestItems = new List<FileOrDirectory>();
			if (Directory.Exists(_parentOfDestinationRootPath))
			{
				_remainingDestItems.AddRange(scanner.Scan(_parentOfDestinationRootPath));
			}

			//nb: this relies on the directories being order from parent-to-children
			foreach (var sourceDirectory in sourceItems.Where(i => i.Type == FileOrDirectory.FileOrDirectoryType.Directory))
			{
				if (_cancelRequested)
				{
					WasCancelled = true;
					return;
				}
				string destination = GetDestination(sourceDirectory.Path);
				if(ShouldCreateDirectory(sourceDirectory))
				{
					CreateDirectory(sourceDirectory.Path);
				}
				RemoveDestinationItemFromRemainingList(destination);
			}
			foreach (var sourceFile in sourceItems.Where(i => i.Type == FileOrDirectory.FileOrDirectoryType.File))
			{
				if (_cancelRequested)
				{
					WasCancelled = true;
					return;
				}
				string destination = GetDestination(sourceFile.Path);
				HandleFile(sourceFile.Path, destination);
				RemoveDestinationItemFromRemainingList(destination);
			}
			
			HandleRemainingDestinationDirectories();
			if (_cancelRequested)
			{
				WasCancelled = true;
				return;
			}
			HandleRemainingDestinationFiles();
			if (_cancelRequested)
			{
				WasCancelled = true;
				return;
			}
		}

		private void CreateDirectory(string sourcePath)
		{
			string destination = GetDestination(sourcePath);
			try
			{
				Directory.CreateDirectory(destination);
			}
			catch (Exception error)
			{
				//enhance: create and use a special CreateDirector action
				RaiseItemHandlingError(destination, MirrorAction.Create, error);
			}
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
				if (_cancelRequested)
					return;
				var action = GetFileActionFromClient(file.Path, 
								MirrorSituation.FileOnDestinationButNotSource, 
								MirrorAction.DoNothing);
				if(action == MirrorAction.Delete)
				{
					File.Delete(file.Path);
				}
			}
		}

		private void HandleRemainingDestinationDirectories()
		{
			foreach (var directory in _remainingDestItems.Where(i => i.Type == FileOrDirectory.FileOrDirectoryType.Directory))
			{
				if (_cancelRequested)
					return;
				var action = GetFileActionFromClient(directory.Path, 
								MirrorSituation.DirectoryOnDestinationButNotSource, 
								MirrorAction.DoNothing);
				if(action == MirrorAction.Delete)
				{
					try
					{
						Directory.Delete(directory.Path, true);
					}
					catch (Exception error)
					{
						RaiseItemHandlingError(directory.Path, MirrorAction.Delete, error);
					}

					_skippedOrRemovedDirectories.Add(directory.Path);
				}
				
			}
		}

	    public int DeletedCount;
        public int UpdatedCount;
        public int CreatedCount;

		private void HandleFile(string source, string dest)
		{
			MirrorAction action = GetActionForFile(source);
			try
			{
				switch (action)
				{
					case MirrorAction.Delete:
						if (File.Exists(dest))
						{
							File.Delete(dest);
						    ++DeletedCount;
						}
						break;
					case MirrorAction.DoNothing:
					case MirrorAction.Skip:
						break;
					case MirrorAction.Create:
						if (File.Exists(dest))
						{
							File.Delete(dest);
						}
						File.Copy(source, dest);
                        ++CreatedCount;
				        break;
					case MirrorAction.Update:
				        ++UpdatedCount;
                       File.Copy(source, dest, true);
                       File.SetLastWriteTimeUtc(dest, File.GetLastWriteTimeUtc(source));
                       //always fails. Ah well. Debug.Assert(File.GetLastWriteTimeUtc(dest) == File.GetLastWriteTimeUtc(source));
                       break;
					default:
						ThrowProgramError("Unexpected enumeration in switch: {0}", source);
						break;
				}
			}
			catch (Exception error)
			{
				RaiseItemHandlingError(source, action, error);
			}
		}

		private void RaiseItemHandlingError(string path, MirrorAction action, Exception error)
		{
			var args = new ItemHandlingErrorArgs(path, action, error);
			EventHandler<ItemHandlingErrorArgs> handler = ItemHandlingError;
			if (handler != null)
			{
				handler(this, args);
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
			Guard.Against(situation == MirrorSituation.DirectoryMissing && action == MirrorAction.Delete, "Told to remove an non-existant directory");
			return action == MirrorAction.Create;
		}

		private MirrorAction GetActionForFile(string source)
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
				var dif = TimeSpan.FromTicks(Math.Abs(sourceWriteTime.Ticks - destWriteTime.Ticks));
				
				//for some reason, the target is always a couple seconds later
				if(dif.Seconds < 5)
				{
					situation = MirrorSituation.FileIsSame;
				}
				else if(sourceWriteTime > destWriteTime)
				{
					situation = MirrorSituation.SourceFileNewer;
					defaultAction = MirrorAction.Update;
				}
				else if (sourceWriteTime < destWriteTime)
				{
					situation = MirrorSituation.SourceFileOlder;
					defaultAction = MirrorAction.Update; //NOTICE, we default to overwriting, even if the dest is newer!
				}
			}

			var action = GetFileActionFromClient(source, situation, defaultAction);
			Guard.Against(situation == MirrorSituation.FileIsSame && action == MirrorAction.Create, "Told to create an existing file");
			Guard.Against(situation == MirrorSituation.FileMissing && action == MirrorAction.Delete, "Told to remove an non-existant file");
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
			var args = new MirrorEventArgs(directory.Path, situation,action);
			EventHandler<MirrorEventArgs> handler = StartingDirectory;
			if (handler != null)
			{
				handler(this, args);
			}
			if(args.PendingAction == MirrorAction.Skip || args.PendingAction ==MirrorAction.Delete)
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

			var args = new MirrorEventArgs(path, situation, action);
			EventHandler<MirrorEventArgs> handler = StartingFile;
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


		public void Cancel()
		{
			_cancelRequested = true;
		}

		public void Dispose()
		{
		}
	}

	public enum MirrorAction { Skip, Create, Delete,
		DoNothing,
		Update
	}
	public enum MirrorSituation { DirectoryMissing, FileMissing, SourceFileOlder, SourceFileNewer,
		DirectoryExists,
		FileIsSame,
		FileOnDestinationButNotSource,
		DirectoryOnDestinationButNotSource,
		Error
	}

	public class MirrorEventArgs : EventArgs
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

		public MirrorEventArgs(string path, MirrorSituation situation, MirrorAction defaultAction)
		{
			Situation = situation;
			Path = path;
			PendingAction = defaultAction;
		}

        /// <summary>
        /// becuase we're a bit over-simple here, we don't have the actual destination path, so at least we can strip off the c:\
        /// when saying "we're now creating ______ "
        /// </summary>
        /// <returns></returns>
	    public string GetDestinationPathForDisplay()
	    {
            //TODO this is giving wrong locations. In actuality, the location is preceded by the group name
            try
            {
                var root = System.IO.Path.GetPathRoot(Path);
                return Path.Substring(root.Length).Replace("{","{{").Replace("}","}}");
            }
            catch(Exception e)
            {
                return "error in GetDestinationPathForDisplay() "+e.Message;
            }
	    }
	}

	public class ItemHandlingErrorArgs: MirrorEventArgs
	{
		public Exception Exception;

		public ItemHandlingErrorArgs(string path, 
			MirrorAction actionThatWasBeingAttempted,
			Exception error)
			: base(path, MirrorSituation.Error, actionThatWasBeingAttempted)
		{
			Exception = error;
		}
	}
}
