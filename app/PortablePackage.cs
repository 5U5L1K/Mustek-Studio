using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace MustekStudio
{
    internal static class PortablePackage
    {
        public static string DataRoot, EngineRoot, Version;
        static Assembly pdf;
        static readonly object pdfLock = new object();
        static Assembly Assembly { get { return typeof(PortablePackage).Assembly; } }
        static Stream Resource(string name)
        {
            Stream stream = Assembly.GetManifestResourceStream("Portable." + name);
            if (stream == null) throw new IOException("Missing embedded component: " + name);
            return stream;
        }
        static byte[] ReadResource(string name)
        {
            using (Stream stream = Resource(name))
            using (MemoryStream memory = new MemoryStream()) { stream.CopyTo(memory); return memory.ToArray(); }
        }
        public static string Hash(Stream stream)
        {
            using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        public static string HashFile(string path) { using (FileStream stream = File.OpenRead(path)) return Hash(stream); }
        public static void Initialize()
        {
            if (!Environment.Is64BitProcess) throw new PlatformNotSupportedException("Windows x64 is required.");
            AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs args)
            {
                if (new AssemblyName(args.Name).Name != "PdfSharp") return null;
                lock (pdfLock) { if (pdf == null) pdf = System.Reflection.Assembly.Load(ReadResource("PdfSharp.dll")); return pdf; }
            };
            DataRoot = Path.Combine(Path.GetDirectoryName(Assembly.Location), "MustekStudio.Data");
            Directory.CreateDirectory(DataRoot);
            using (StreamReader reader = new StreamReader(Resource("version.txt"), Encoding.UTF8)) Version = reader.ReadToEnd().Trim();
            if (Version.Length != 64) throw new IOException("Invalid embedded package version.");
            foreach (char c in Version) if (!Uri.IsHexDigit(c)) throw new IOException("Invalid package version.");
            EngineRoot = Path.Combine(DataRoot, "engine", Version.Substring(0, 16));
            string lockName;
            using (MemoryStream path = new MemoryStream(Encoding.UTF8.GetBytes(DataRoot.ToUpperInvariant()))) lockName = Hash(path).Substring(0, 24);
            using (Mutex gate = new Mutex(false, "Local\\MustekPortable-" + lockName))
            {
                bool acquired;
                try { acquired = gate.WaitOne(TimeSpan.FromSeconds(60)); }
                catch (AbandonedMutexException) { acquired = true; }
                if (!acquired) throw new IOException("Timed out preparing the portable application.");
                try
                {
                    ExtractVerified();
                    string document = Path.Combine(DataRoot, "app-data", "document.xml");
                    if (!File.Exists(document))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(document));
                        using (FileStream output = new FileStream(document, FileMode.CreateNew, FileAccess.Write))
                        using (Stream input = Resource("defaults.xml")) input.CopyTo(output);
                    }
                }
                finally { gate.ReleaseMutex(); }
            }
        }
        static void ExtractVerified()
        {
            Directory.CreateDirectory(EngineRoot);
            string rootPrefix = Path.GetFullPath(EngineRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            using (Stream payload = Resource("payload.zip"))
            {
                if (Hash(payload) != Version) throw new IOException("Embedded package checksum mismatch.");
                payload.Position = 0;
                using (ZipArchive archive = new ZipArchive(payload, ZipArchiveMode.Read))
                using (StreamReader reader = new StreamReader(Resource("manifest.tsv"), Encoding.UTF8))
                {
                    HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Length == 0) continue;
                        string[] fields = line.Split('\t');
                        long length;
                        if (fields.Length != 3 || fields[0].Length != 64 || !Int64.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out length) || length < 0)
                            throw new IOException("Invalid package file manifest.");
                        string relative = fields[2], target = Path.GetFullPath(Path.Combine(EngineRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
                        if (!target.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) || !seen.Add(relative)) throw new IOException("Invalid package path.");
                        ZipArchiveEntry entry = archive.GetEntry(relative);
                        if (entry == null || entry.Length != length) throw new IOException("Missing package entry: " + relative);
                        if (File.Exists(target) && new FileInfo(target).Length == length && HashFile(target) == fields[0]) continue;
                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                        string temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
                        try
                        {
                            using (Stream input = entry.Open())
                            using (FileStream output = File.Create(temporary)) input.CopyTo(output);
                            if (HashFile(temporary) != fields[0]) throw new IOException("Package file checksum mismatch: " + relative);
                            if (File.Exists(target)) File.Replace(temporary, target, null); else File.Move(temporary, target);
                        }
                        finally { if (File.Exists(temporary)) File.Delete(temporary); }
                    }
                    if (seen.Count != archive.Entries.Count) throw new IOException("Package manifest does not match payload.");
                }
            }
        }
        public static void WriteFailure(Exception error)
        {
            try
            {
                string root = DataRoot ?? Path.Combine(Path.GetDirectoryName(Assembly.Location), "MustekStudio.Data");
                Directory.CreateDirectory(root); File.WriteAllText(Path.Combine(root, "portable-error.txt"), error.ToString(), Encoding.UTF8);
            }
            catch { }
        }
    }
}
