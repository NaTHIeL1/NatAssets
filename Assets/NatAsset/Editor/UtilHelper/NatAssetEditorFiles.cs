using System;
using System.IO;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
namespace NATFrameWork.NatAsset.Editor
{
    public static class NatAssetEditorFiles
    {
        private static StringComparison PathComparison => Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        public static string HashFile(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create()) return Hex(sha.ComputeHash(stream));
        }
        public static string HashText(string text)
        {
            using (var sha = SHA256.Create()) return Hex(sha.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }
        private static string Hex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        public static void WriteJson(string path, object value)
        {
            path = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string pending = path + ".pending-" + Guid.NewGuid().ToString("N");
            string backup = pending + ".previous";
            try
            {
                // Flush the complete new state before replacing the last good state.
                using (var stream = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    byte[] bytes = new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(value, true));
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
#if UNITY_EDITOR_WIN
                // Mono's File.Replace(source, destination, null) intermittently fails on Windows.
                // A native rename in the same directory preserves atomic publication without that path.
                for (int attempt = 0; ; attempt++)
                {
                    if (MoveFileEx(pending, path, 0x1 | 0x8)) break; // REPLACE_EXISTING | WRITE_THROUGH
                    int error = Marshal.GetLastWin32Error();
                    if ((error == 32 || error == 33) && attempt < 3)
                    {
                        System.Threading.Thread.Sleep(25 * (attempt + 1));
                        continue;
                    }
                    throw new IOException("无法原子保存构建记录：" + path, new Win32Exception(error));
                }
#else
                if (File.Exists(path)) File.Replace(pending, path, backup);
                else File.Move(pending, path);
#endif
            }
            catch
            {
                if (!File.Exists(path) && File.Exists(backup)) File.Move(backup, path);
                throw;
            }
            finally
            {
                DeleteTemporaryFile(pending);
                if (File.Exists(path)) DeleteTemporaryFile(backup);
            }
        }

#if UNITY_EDITOR_WIN
        [DllImport("kernel32.dll", EntryPoint = "MoveFileExW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool MoveFileEx(string source, string destination, int flags);
#endif

        private static void DeleteTemporaryFile(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { /* A completed state write is still valid when temporary cleanup is delayed. */ }
            catch (UnauthorizedAccessException) { }
        }
        public static T ReadJson<T>(string path) where T : class =>
            File.Exists(path) ? JsonUtility.FromJson<T>(File.ReadAllText(path)) : null;

        internal static void TryDeleteFailedBuild(string directory, string parent)
        {
            try
            {
                DeleteBuildDirectory(directory, parent);
            }
            catch (Exception error)
            {
                // Keep the original build exception when a locked file prevents cleanup.
                Debug.LogWarning($"[NatAsset] 清理失败的资源版本目录失败：{directory}\n{error}");
            }
        }

        private static void DeleteBuildDirectory(string directory, string parent)
        {
            RequireChild(directory, parent);
            if (!Directory.Exists(directory)) return;
            foreach (string child in Directory.GetDirectories(directory))
                DeleteBuildDirectory(child, directory);
            foreach (string file in Directory.GetFiles(directory))
            {
                RequireChild(file, directory);
                File.Delete(file);
            }
            Directory.Delete(directory);
        }

        public static void RequireChild(string path, string parent)
        {
            string root = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(path).StartsWith(root, PathComparison))
                throw new IOException("路径超出受管理目录：" + path);
            for (string current = Path.GetFullPath(path); !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
                if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("受管理路径不能经过目录联接或符号链接：" + current);
        }

    }
}
