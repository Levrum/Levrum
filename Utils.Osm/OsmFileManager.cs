using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Levrum.Utils.Osm
{
    public class OsmFileManager
    {
        DirectoryInfo m_storageDir = new DirectoryInfo(string.Format("{0}\\Levrum\\OsmFiles", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)));
        public DirectoryInfo StorageDirectory
        {
            get
            {
                return m_storageDir;
            }
            set
            {
                m_storageDir = value;
                if (!m_storageDir.Exists)
                    m_storageDir.Create();
            }
        }

        public OsmFileManager()
        {
            if (StorageDirectory.Exists == false)
                StorageDirectory.Create();
        }

        public List<OsmFileInfo> GetOsmFiles()
        {
            List<OsmFileInfo> output = new List<OsmFileInfo>();
            try
            {
                DirectoryInfo[] directories = StorageDirectory.GetDirectories();
                FileInfo[] files;
                foreach (DirectoryInfo directory in directories)
                {
                    foreach (FileInfo file in directory.EnumerateFiles())
                    {
                        string name = file.Name.ToLowerInvariant();
                        if (name.EndsWith(".osm.zip"))
                        {
                            OsmFileInfo info = new OsmFileInfo(file, true);
                            output.Add(info);
                        }
                        else if (name.EndsWith(".osm") || name.EndsWith(".pbf"))
                        {
                            OsmFileInfo info = new OsmFileInfo(file, false);
                            output.Add(info);
                        }
                    }
                }

                files = StorageDirectory.GetFiles();
                foreach (FileInfo file in files)
                {
                    if (file.Extension.ToLowerInvariant() == ".osm")
                    {
                        OsmFileInfo existingInfo = (from OsmFileInfo o in output
                                                    where o.OsmFile.Name == file.Name
                                                    select o).FirstOrDefault();

                        if (existingInfo == null)
                        {
                            AddOsmFile(file);
                        }
                        else if (file.LastWriteTime > existingInfo.LastWriteTime)
                        {
                            UpdateOsmFile(existingInfo, file);
                        }
                    }
                }
            } catch (Exception ex)
            {
                LogHelper.LogException(ex);
                output.Clear();
            }

            return output;
        }

        public OsmFileInfo AddOsmFile(FileInfo file, bool compress = false, bool overwrite = true)
        {
            try
            {
                string baseName = file.Name.Substring(0, file.Name.Length - file.Extension.Length).Replace(" ", "_");
                DirectoryInfo directory = new DirectoryInfo(string.Format("{0}\\{1}", StorageDirectory, baseName));
                if (overwrite == false)
                {
                    int nextDirNum = 1;
                    while (directory.Exists)
                    {
                        directory = new DirectoryInfo(string.Format("{0}\\{1}_{2}", StorageDirectory, baseName, nextDirNum));
                        nextDirNum++;
                    }
                    directory.Create();
                }
                else
                {
                    if (directory.Exists)
                    {
                        directory.Delete(true);
                    }

                    directory.Create();
                }

                string newPath = string.Format("{0}\\{1}", directory.FullName, file.Name);
                file.CopyTo(newPath);

                OsmFileInfo info = new OsmFileInfo(new FileInfo(newPath), compress);
                if (compress)
                {
                    info.Compress();
                }

                return info;
            } catch (Exception ex)
            {
                LogHelper.LogException(ex);
                return null;
            }
        }

        public void RemoveOsmFile(OsmFileInfo info)
        {
            try
            {
                DirectoryInfo directory = info.OsmFile.Directory;
                directory.Delete(true);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        public void UpdateOsmFile(OsmFileInfo existingFile, FileInfo file)
        {
            if (existingFile.Zipped)
            {
                existingFile.UpdateZip(file);
            } else
            {
                file.CopyTo(existingFile.FullName, true);
            }
        }
    }
}
