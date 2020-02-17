using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Osrmnet;

namespace Levrum.Utils.Osm
{
    public class OsmFileInfo
    {
        public bool Zipped { get; set; } = false;

        private FileInfo m_fileInfo = null;
        public FileInfo OsmFile
        {
            get
            {
                return m_fileInfo;
            }
            set
            {
                m_fileInfo = value;
                m_fileInfo.Refresh();
            }
        }

        public FileInfo OsrmFile
        {
            get
            {
                if (OsmFile == null)
                {
                    return null;
                }

                DirectoryInfo directory = OsmFile.Directory;
                string osrmFileName = OsmFile.Name.Substring(0, OsmFile.Name.Length - OsmFile.Extension.Length).Replace(" ", "_");
                FileInfo output = new FileInfo(string.Format("{0}\\{1}.osrm", directory.FullName, osrmFileName));
                return output.Exists ? output : null;
            }
        }

        private ZipArchive ZipArchive { get; set; } = null;

        public string FullName { get { return OsmFile.FullName; } }
        public string Name { get { return OsmFile.Name; } }
        public long Length { get { if (OsmFile.Exists) return OsmFile.Length; else return 0; } }
        public bool Exists { get { return OsmFile.Exists; } }
        public DateTime Created { get { OsmFile.Refresh(); return OsmFile.CreationTime; } }
        public DateTime LastWriteTime { get { OsmFile.Refresh(); return OsmFile.LastWriteTime; } }

        public OsmFileInfo(FileInfo _info = null, bool _zipped = false)
        {
            OsmFile = _info;
            Zipped = _zipped;
        }

        public static explicit operator FileInfo(OsmFileInfo _info)
        {
            return _info.OsmFile;
        }

        public static explicit operator OsmFileInfo(FileInfo _info)
        {
            if (!_info.Exists)
            {
                return null;
            }

            OsmFileInfo output = new OsmFileInfo();
            output.OsmFile = _info;
            return output;
        }

        public string ToString()
        {   
            return OsmFile.Name.Substring(0, OsmFile.Name.Length - OsmFile.Extension.Length);
        }

        public Stream OpenRead()
        {
            if (!OsmFile.Exists)
            {
                throw new FileNotFoundException(m_fileInfo.Name);
            }

            if (Zipped == true)
            {
                if (ZipArchive != null)
                {
                    ZipArchive.Dispose();
                    ZipArchive = null;
                }

                ZipArchive = ZipFile.Open(OsmFile.FullName, ZipArchiveMode.Read);
                ZipArchiveEntry entry = ZipArchive.GetEntry(OsmFile.Name.Substring(0, OsmFile.Name.Length - OsmFile.Extension.Length));
                return entry.Open();
            }
            else
            {
                return OsmFile.OpenRead();
            }
        }

        public Stream OpenWrite()
        {
            if (OsmFile.FullName == string.Empty)
            {
                throw new ArgumentException("File name is null");
            }
            else if (!OsmFile.Exists)
            {
                OsmFile.Create();
            }

            if (Zipped == true)
            {
                if (ZipArchive != null)
                {
                    ZipArchive.Dispose();
                    ZipArchive = null;
                }

                ZipArchive = new ZipArchive(OsmFile.OpenRead(), ZipArchiveMode.Update);
                ZipArchiveEntry osmFileEntry = ZipArchive.CreateEntry(string.Format("{0}.osm", OsmFile.Name.Substring(0, OsmFile.Name.Length - OsmFile.Extension.Length)));
                return osmFileEntry.Open();
            }
            else
            {
                return OsmFile.OpenWrite();
            }
        }

        public byte[] ReadAllBytes()
        {
            if (!OsmFile.Exists)
            {
                throw new FileNotFoundException(OsmFile.FullName);
            }
            else if (Zipped == true)
            {
                if (ZipArchive != null)
                {
                    ZipArchive.Dispose();
                    ZipArchive = null;
                }

                ZipArchive = new ZipArchive(OsmFile.OpenRead(), ZipArchiveMode.Update);
                ZipArchiveEntry osmFileEntry = ZipArchive.CreateEntry(string.Format("{0}.osm", OsmFile.Name.Substring(0, OsmFile.Name.Length - OsmFile.Extension.Length)));
                
                using (Stream stream = osmFileEntry.Open())
                {
                    using (MemoryStream memStream = new MemoryStream())
                    {
                        byte[] buffer = new byte[16 * 1024];
                        int read;
                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            memStream.Write(buffer, 0, read);
                        }
                        return memStream.ToArray();
                    }
                }
            }
            else
            {
                return File.ReadAllBytes(OsmFile.FullName);
            }
        }

        public string ReadAllText()
        {
            if (!OsmFile.Exists)
            {
                throw new FileNotFoundException(OsmFile.FullName);
            }
            else if (Zipped == true)
            {
                if (ZipArchive != null)
                {
                    ZipArchive.Dispose();
                    ZipArchive = null;
                }

                ZipArchive = new ZipArchive(OsmFile.OpenRead(), ZipArchiveMode.Update);
                ZipArchiveEntry osmFileEntry = ZipArchive.CreateEntry(string.Format("{0}.osm", OsmFile.Name.Substring(0, OsmFile.Name.Length - OsmFile.Extension.Length)));

                using (Stream stream = osmFileEntry.Open())
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            else
            {
                return File.ReadAllText(OsmFile.FullName);
            }
        }

        public void WriteAllText(string input)
        {
            if (OsmFile.FullName == string.Empty)
            {
                throw new ArgumentException("File name is null");
            }
            else if (Zipped == true)
            {
                if (ZipArchive != null)
                {
                    ZipArchive.Dispose();
                    ZipArchive = null;
                }

                ZipArchive = new ZipArchive(OsmFile.OpenRead(), ZipArchiveMode.Update);
                ZipArchiveEntry osmFileEntry = null;
                string entryName = string.Format("{0}.osm", OsmFile.Name.Substring(0, OsmFile.Name.Length - OsmFile.Extension.Length));
                try
                {
                    osmFileEntry = ZipArchive.GetEntry(entryName);
                } catch (Exception ex)
                {
                    
                }
                if (osmFileEntry == null)
                {
                    osmFileEntry = ZipArchive.CreateEntry(entryName);
                }

                using (Stream stream = osmFileEntry.Open())
                {
                    using (StreamWriter sw = new StreamWriter(stream))
                    {
                        sw.Write(input);
                    }
                }

            }
            else
            {
                File.WriteAllText(OsmFile.FullName, input);
            }
        }

        public void UpdateZip(FileInfo file)
        {
            if (Zipped == false)
            {
                return;
            }

            string text = File.ReadAllText(file.FullName);
            WriteAllText(text);
        }

        public void Compress()
        {
            if (OsmFile.Extension == ".zip" || OsmFile.Extension == ".pbf")
            {
                if (Zipped || OsmFile.Extension == ".pbf") // PBF files are already compressed, we don't deal with them
                {
                    return;
                }
                else
                {
                    Zipped = true;
                    return;
                }
            }

            FileInfo tempFile = null;
            DirectoryInfo osmFileDir = new DirectoryInfo(OsmFile.DirectoryName);
            DirectoryInfo tempDir = new DirectoryInfo(string.Format("{0}\\temp\\{1}", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DateTime.Now.Ticks));

            string name = OsmFile.Name;
            string zipName = string.Format("{0}\\{1}.zip", osmFileDir.FullName, name);

            try
            {
                tempDir.Create();
                tempFile = new FileInfo(string.Format("{0}\\{1}", tempDir.FullName, OsmFile.Name));
                OsmFile.CopyTo(tempFile.FullName);
                ZipFile.CreateFromDirectory(tempDir.FullName, zipName);

                FileInfo zipFile = new FileInfo(zipName);
                if (zipFile.Exists)
                {
                    OsmFile.Delete();
                    OsmFile = zipFile;
                }
                else
                {
                    throw new Exception("Unable to compress OSM file, zipFile does not exist.");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {
                if (tempDir.Exists)
                {
                    tempDir.Delete(true);
                }
            }
        }

        public void Decompress()
        {
            if (OsmFile.Extension != ".zip")
            {
                return;
            }

            DirectoryInfo osmFileDir = new DirectoryInfo(OsmFile.DirectoryName);

            try
            {
                ZipArchive archive = ZipFile.OpenRead(OsmFile.FullName);
                ZipArchiveEntry entry = archive.Entries[0];
                string fileName = string.Format("{0}\\{1}", osmFileDir.FullName, entry.FullName);
                entry.ExtractToFile(fileName);
                FileInfo unzippedFile = new FileInfo(fileName);
                if (unzippedFile.Exists)
                {
                    OsmFile.Delete();
                    OsmFile = unzippedFile;
                }
                else
                {
                    throw new Exception("Unable to decompress OSM file.");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        public void CreateOsrmFiles(string profilePath = "")
        {
            FileInfo osmFile = OsmFile;

            // osrm can't handle spaces in osm filenames
            string lastName = osmFile.Name;
            string safeName = string.Format("{0}.osm", osmFile.Name.Replace(' ', '_').Substring(0, osmFile.Name.Length - osmFile.Extension.Length));
            try
            {
                if (!safeName.Equals(lastName))
                {
                    osmFile.CopyTo(osmFile.DirectoryName + "\\" + safeName);
                    osmFile = new FileInfo(osmFile.DirectoryName + "\\" + safeName);
                }

                string osrmExtractPath = AppDomain.CurrentDomain.BaseDirectory + "osrm\\osrm-extract.exe";
                FileInfo osrmExtract = new FileInfo(osrmExtractPath);
                if (!osrmExtract.Exists)
                {
                    string osrmZipPath = AppDomain.CurrentDomain.BaseDirectory + "osrm.zip";
                    FileInfo osrmZip = new FileInfo(osrmZipPath);
                    if (!osrmZip.Exists)
                    {
                        throw new Exception("Unable to create OSRM files. OSRM is missing.");
                    }

                    ZipFile.ExtractToDirectory(osrmZipPath, AppDomain.CurrentDomain.BaseDirectory);
                }

                if (string.IsNullOrEmpty(profilePath))
                {
                    profilePath = AppDomain.CurrentDomain.BaseDirectory + "osrm\\car.lua";
                }

                ProcessStartInfo info = new ProcessStartInfo();
                info.Arguments = osmFile.FullName + " -p " + profilePath;
                info.WindowStyle = ProcessWindowStyle.Hidden;
                info.CreateNoWindow = false;
                info.FileName = osrmExtractPath;
                info.UseShellExecute = true;
                using (Process extractProcess = Process.Start(info))
                {
                    while (!extractProcess.WaitForExit(1000))
                    {
                        if (!extractProcess.Responding)
                            throw new Exception("osrm-extract stopped responding while processing OSM file.");
                    }
                }

                info = new ProcessStartInfo();
                info.Arguments = OsrmFile?.FullName;
                info.WindowStyle = ProcessWindowStyle.Hidden;
                info.CreateNoWindow = true;
                info.FileName = AppDomain.CurrentDomain.BaseDirectory + "osrm\\osrm-contract.exe";
                info.UseShellExecute = false;
                using (Process contractProcess = Process.Start(info))
                {
                    while (!contractProcess.WaitForExit(1000))
                    {
                        if (!contractProcess.Responding)
                            throw new Exception("osrm-contract stopped responding while processing data.");
                    }
                }
            }
            catch (ThreadAbortException ex)
            {
                File.Delete(OsrmFile.FullName);
            }
            finally
            {
                if (!safeName.Equals(lastName))
                {
                    File.Delete(osmFile.DirectoryName + "\\" + safeName);
                }
            }
        }
    }
}
