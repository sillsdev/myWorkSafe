﻿using System;
using System.IO;
using myWorkSafe;
using NUnit.Framework;
using SIL.TestUtilities;

namespace WorkSafe.Tests
{
	public class MirrorMakerTests
	{
		private MirrorMaker _maker;
		private string _sourceRoot;
		private TemporaryFolder _sourceTempFolder;
		private TemporaryFolder _destTempFolder;
		private string _parentOfDestingationPath;

		[SetUp]
		public void Setup()
		{
			_sourceTempFolder = new TemporaryFolder("MirrorMakerSource");
			_destTempFolder = new TemporaryFolder("MirrorMakerDest");
			_sourceRoot = _sourceTempFolder.Path;
			_parentOfDestingationPath = _destTempFolder.Combine("myWorkSafe-Sally");

			_maker = new MirrorMaker(_sourceRoot, _parentOfDestingationPath);
		}
		[TearDown]
		public void TearDown()
		{
			_sourceTempFolder.Dispose();
			_destTempFolder.Dispose();
		}

	    [Test] public void Run_HaveSomethingToCopyAndDestinationIsEmpty_DestinationFolderCreated()
		{
			using (var tempDest = new TemporaryFolder("MirrorMakerDest-special"))
			{
				Directory.Delete(tempDest.Path);
				var specialMaker = new MirrorMaker(_sourceRoot, tempDest.Path);
			    _sourceTempFolder.GetNewTempFile(true); //if there's nothing to copy, mws will delete the destination
				specialMaker.Run();
				Assert.IsTrue(Directory.Exists(tempDest.Path));
			}
		}
        [Test]
        public void Run_HaveNothingToCopyAndDestinationIsEmpty_DestinationFolderRemoved()
        {
            using (var tempDest = new TemporaryFolder("MirrorMakerDest-special"))
            {
                Directory.Delete(tempDest.Path);
                var specialMaker = new MirrorMaker(_sourceRoot, tempDest.Path);
                specialMaker.Run();
                Assert.IsFalse(Directory.Exists(tempDest.Path));
            }
        }

		[Test]
		public void Run_EmptyDestination_FileCopied()
		{
			var path = CreateSourceDirectoriesAndGenericFile("apple.txt");
			_maker.Run();
			AssertCorrespondingFileExists(path);
		}

		[Test]
		public void Run_EmptyDestination_InnerFileCopied()
		{
			var path = CreateSourceDirectoriesAndGenericFile("fruit", "treefruit", "apple.txt");
			_maker.Run();
			AssertCorrespondingFileExists(path);
		}

		[Test]
		public void Run_DestinationDoesNotHaveDirectory_CorrectSituationInEvent()
		{
			var path = CreateSourceDirectoriesAndGenericFile("fruit");
			MirrorEventArgs gotArgs=null;
			_maker.StartingDirectory += ((o, args) => gotArgs = args);
			_maker.Run();
			Assert.AreEqual(MirrorSituation.DirectoryMissing, gotArgs.Situation);
			AssertCorrespondingFileExists(path);
		}

		[Test]
		public void Run_DestinationAlreadyHasDirectory_CorrectSituationInEvent()
		{
			var path = CreateSourceDirectoriesAndGenericFile("fruit");
			_maker.Run();
			MirrorEventArgs gotArgs = null;
			_maker.StartingDirectory += ((o, args) => gotArgs = args);
			_maker.Run();
			Assert.AreEqual(MirrorSituation.DirectoryExists, gotArgs.Situation);
			AssertCorrespondingFileExists(path);
		}

		[Test]
		public void Run_DestinationDoesNotHaveFile_CorrectSituationInEvent()
		{
			var path = CreateSourceDirectoriesAndGenericFile("fruit","apple.txt");
			MirrorEventArgs gotArgs = null;
			_maker.StartingFile += ((o, args) => gotArgs = args);
			_maker.Run();
			Assert.AreEqual(MirrorSituation.FileMissing, gotArgs.Situation);
			AssertCorrespondingFileExists(path);
		}

		[Test]
		public void Run_DestinationAlreadyHasFile_CorrectSituationInEvent()
		{
			var path = CreateSourceDirectoriesAndGenericFile("fruit","apple.txt");
			_maker.Run();
			MirrorEventArgs gotArgs = null;
			_maker.StartingFile += ((o, args) => gotArgs = args);
			_maker.Run();
			Assert.AreEqual(MirrorSituation.FileIsSame, gotArgs.Situation);
			AssertCorrespondingFileExists(path);
		}

		[Test]
		public void Run_SourceFileNewer_FileCopied()
		{
			var path = CreateSourceDirectoriesAndGenericFile("fruit", "treefruit", "apple.txt");
			_maker.Run();
			File.WriteAllText(path, "newer contents");
            var destPath = GetDestPathFromSourcePath(path);
			File.SetLastWriteTimeUtc(destPath, new DateTime(2010, 1,1));
			_maker.Run();
			AssertDestinationFileUpdated(path);
		}


		[Test]
		public void Run_ToldToSkipDirectory_DoesNotCopyDirectoryOrChildren()
		{
			CreateSourceDirectoriesAndGenericFile("fruit", "skipMe", "treefruit", "apple.txt");
			var path = CreateSourceDirectoriesAndGenericFile("fruit", "vinefruit", "rasberry.txt");
			_maker.StartingDirectory += ((o, args) =>
											{ if (args.Path.Contains("skip")) 
												args.PendingAction = MirrorAction.Skip; });
			_maker.Run();
			AssertCorrespondingFileExists(path);
			AssertCorrespondingDirectoryDoesNotExist(_sourceTempFolder.Combine("fruit","skipMe"));
		}

		[Test]
		public void Run_ToldToSkipFile_DoesNotCopy()
		{
			var dontCopy = CreateSourceDirectoriesAndGenericFile("fruit", "treefruit", "skip.txt");
			var path = CreateSourceDirectoriesAndGenericFile("fruit", "treefruit", "apple.txt");
			_maker.StartingFile += ((o, args) =>
												{
													if (args.Path.Contains("skip"))
														args.PendingAction = MirrorAction.Skip;
												});
			_maker.Run();
			AssertCorrespondingFileExists(path);
			AssertCorrespondingFileDoesNotExist(dontCopy);
		}



		[Test]
		public void Run_SourceMissingFileAndCallbackSaysToRemove_RemovedOnDestination()
		{
			// NB: this requires the engine to also scan the destination to find left-overs

			var path = CreateSourceDirectoriesAndGenericFile("fruit", "treefruit", "apple.txt");
			_maker.Run();
			AssertCorrespondingFileExists(path);
			File.Delete(path);
			_maker.StartingFile += ((o, args) =>
								{
									if (args.Situation == MirrorSituation.FileOnDestinationButNotSource)
										args.PendingAction = MirrorAction.Delete;
								});
			_maker.Run();
			AssertCorrespondingFileDoesNotExist(path);
		}

		[Test]
		public void Run_SourceMissingDirectoryAndCallbackSaysToRemove_RemovedOnDestination()
		{
			var path = CreateSourceDirectoriesAndGenericFile("fruit", "treefruit", "apple.txt");
			_maker.Run();
			AssertCorrespondingFileExists(path);
			Directory.Delete(_sourceTempFolder.Combine("fruit"),true);
			_maker.StartingFile += ((o, args) =>
								{
									if (args.Situation == MirrorSituation.DirectoryOnDestinationButNotSource)
										args.PendingAction = MirrorAction.Delete;
								});
			_maker.Run();
			AssertCorrespondingDirectoryDoesNotExist(_sourceTempFolder.Combine("fruit"));
		}

		private void AssertCorrespondingDirectoryDoesNotExist(string sourcePath)
		{
			Assert.IsFalse(Directory.Exists(GetDestPathFromSourcePath(sourcePath)),"The directory was not supposed to exist");
		}


		private void AssertCorrespondingFileDoesNotExist(string sourcePath)
		{
			Assert.IsFalse(File.Exists(GetDestPathFromSourcePath(sourcePath)), "The file was not supposed to exist");
		}

		private string CreateSourceDirectoriesAndGenericFile(params string[] partsEndingInFileName)
		{
			return CreateSourceDirectoriesAndFile("sourceContents", partsEndingInFileName);
		}

		private string CreateSourceDirectoriesAndFile(string contents,  params string[] partsEndingInFileName)
		{
			string dir = _sourceTempFolder.Path;
			for (int i = 0; i < partsEndingInFileName.Length-1; i++)
			{
				dir =Path.Combine(dir,partsEndingInFileName[i]);
				if(!Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
			}
			string path = Path.Combine(dir, partsEndingInFileName[partsEndingInFileName.Length - 1]);
			File.WriteAllText(path, contents);
			return path;
		}

		private void AssertCorrespondingFileExists(string sourcePath)
		{
			var destPath = GetDestPathFromSourcePath(sourcePath);
			Assert.IsTrue(File.Exists(destPath),"File Doesn't Exist");
		}


		private void AssertDestinationFileUpdated(string sourcePath)
		{
			var destPath = GetDestPathFromSourcePath(sourcePath);
			Assert.AreEqual(File.ReadAllText(sourcePath), File.ReadAllText(destPath));
			Assert.AreEqual(File.GetLastWriteTimeUtc(sourcePath), File.GetLastWriteTimeUtc(destPath));
		}

		private string GetDestPathFromSourcePath(string sourcePath)
		{
			var relativePath = sourcePath.Replace(_sourceRoot, "").Trim(new char[] { Path.DirectorySeparatorChar });
			return Path.Combine(_parentOfDestingationPath, relativePath);
		}
	}
}
