using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Experimental.IO;
using Microsoft.Win32.SafeHandles;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace myWorkSafe
{
    /// <summary>
    /// This is a wrapper around the Microsoft.Experimental.IO LongPathFile and LongPathDirectory classes
    /// (see http://bcl.codeplex.com  LongPath.zip), with some additional methods going straight to pinvoke as needed.
    /// 
    /// For a bit of peace-of-mind, I've chosen to only use these "experimental" library calls *only* for long paths.
    /// </summary>
    class SafeIO
    {

        internal const int MAX_PATH = 260;
    
        public class Directory
        {
            public static bool Exists(string path)
            {
                if (path.Length < MAX_PATH)
                {
                    return System.IO.Directory.Exists(path);
                }
                else
                {
                    return LongPathDirectory.Exists(path);
                }
            }

            public static void Create(string path)
            {
                if (path.Length < MAX_PATH)
                {
                    System.IO.Directory.CreateDirectory(path);
                }
                else
                {
                    LongPathDirectory.Create(path);
                }
            }

            public static void Delete(string path)
            {
                if (path.Length < MAX_PATH) 
                {
                    System.IO.Directory.Delete(path, true);
                }
                else
                {
                    // NOTE: recursive isn't available on the longpath yet, so we only do this if we have to
                    //TODO: implement the recursive stuff ourself
                    LongPathDirectory.Delete(path);
                }
            }

            public static string[] GetDirectories(string path)
            {
                if (path.Length < MAX_PATH)
                {
                    return System.IO.Directory.GetDirectories(path);
                }
                else
                {
                    return LongPathDirectory.EnumerateDirectories(path).ToArray();
                }

            }

            public static string[] GetFiles(string path)
            {
                if (path.Length < MAX_PATH)
                {
                    return System.IO.Directory.GetFiles(path);
                }
                else
                {
                    return LongPathDirectory.EnumerateFiles(path).ToArray();
                }
            }
        }
        public class File
        {
            public static void Delete(string path)
            {
                 if (path.Length < MAX_PATH)
                {
                    System.IO.File.Delete(path);
                }
                else
                {
                    LongPathFile.Delete(path);
                }
            }

            public static bool Exists(string path)
            {
                if (path.Length < MAX_PATH)
                {
                    return System.IO.File.Exists(path);
                }
                else
                {
                    return LongPathFile.Exists(path);
                }
            }

            public static void Copy(string source, string dest, bool overwrite)
            {
                if (source.Length < MAX_PATH && dest.Length < MAX_PATH)
                {
                    System.IO.File.Copy(source,dest,overwrite);
                }
                else
                {
                    LongPathFile.Copy(source,dest,overwrite);
                }
            }


            public static DateTime GetLastWriteTimeUtc(string path)
            {
                if (path.Length < MAX_PATH)
                {
                    return System.IO.File.GetLastWriteTimeUtc(path);
                }
                else
                {
                    WIN32_FIND_DATA data;
                    FindFirstFile(LongPathPrefix + path, out data);
                    return DateTime.FromFileTimeUtc(((((long)data.ftLastWriteTime.dwHighDateTime) << 32) | data.ftLastWriteTime.dwLowDateTime));
                }
            }

            public static void SetLastWriteTimeUtc(string path, DateTime time)
            {
                if (path.Length < MAX_PATH)
                {
                    System.IO.File.SetLastWriteTimeUtc(path, time);
                }
                else
                {
                    //TODO
                }
            }


            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            internal struct WIN32_FIND_DATA
            {
                internal FileAttributes dwFileAttributes;
                internal FILETIME ftCreationTime;
                internal FILETIME ftLastAccessTime;
                internal FILETIME ftLastWriteTime;
                internal int nFileSizeHigh;
                internal int nFileSizeLow;
                internal int dwReserved0;
                internal int dwReserved1;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
                internal string cFileName;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_ALTERNATE)]
                internal string cAlternate;
            }

            internal const string LongPathPrefix = @"\\?\";
            internal const int MAX_ALTERNATE = 14;

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern FileAttributes GetFileAttributes(string lpFileName);

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

            internal const int INVALID_FILE_ATTRIBUTES = -1;

            internal static int TryGetFileAttributes(string normalizedPath, out FileAttributes attributes)
            {
                // NOTE: Don't be tempted to use FindFirstFile here, it does not work with root directories

                attributes = GetFileAttributes(normalizedPath);
                if ((int)attributes == INVALID_FILE_ATTRIBUTES)
                    return Marshal.GetLastWin32Error();

                return 0;
            }

            [DllImport("kernel32.dll")]
            public static extern bool GetFileTime(SafeFileHandle hf, out long cre, out long acc, out long mod);

        }
    }
}
