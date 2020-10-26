using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace DailyXMLAggregator
{
    public static class DataManager
    {
        public static List<FileInfo> FilesToProcess { get; set; } = new List<FileInfo>();

        /// <summary>
        /// Gets all the XML files from <see cref="ConfigurationManager.SourceDirectory"/> and loads
        /// them info <see cref="FilesToProcess"/>.
        /// </summary>
        public static void GetFilesToProcess()
        {
            // Get all xml files from source directory
            List<FileInfo> filesToProcess = ConfigurationManager.SourceDirectory.GetFiles("*.xml").ToList();
            FilesToProcess = filesToProcess;
        }

        /// <summary>
        /// Appends each file in <see cref="FilesToProcess"/> to the appropriate daily digest file.
        /// Removes each file from <see cref="ConfigurationManager.SourceDirectory"/> after being
        /// appended to the daily digest file.
        /// </summary>
        public static void ArchiveData()
        {
            XDocument digestXML = null;
            foreach (FileInfo incidentFile in FilesToProcess)
            {
                XDocument incidentXML = ParseXMLFromFile(incidentFile);
                DateTime incidentDateTime = GetDate(incidentXML);
                FileInfo digestFile = GetDigestFile(incidentDateTime);
                digestXML = GetDigestXML(incidentDateTime.Date, digestXML);
                AppendIncident(incidentXML, digestXML);
                WriteDigestFile(digestXML, digestFile);
                RemoveFileFromSourceDirectory(incidentFile);
            }
        }        

        /// <summary>
        /// Adds the given incident to the digest xml document
        /// </summary>
        /// <param name="incidentXML"></param>
        /// <param name="digestXML"></param>
        private static void AppendIncident(XDocument incidentXML, XDocument digestXML)
        {
            bool allowDuplicateIds = false;

            XElement incidents = digestXML.Element("Incidents");
            XElement newIncident = incidentXML.Element("Incident");

            // Check if incident id already exists in file
            bool isDuplicateId = incidents.Elements().Any(el => el.Element("IncidentId").Value == newIncident.Element("IncidentId").Value);
            if (!allowDuplicateIds && isDuplicateId)
            {
                return;
            }

            // Append new incident in proper spot to maintain order
            bool appended = false;
            List<XElement> existingIncidents = incidents.Elements("Incident").ToList();
            DateTime newIncidentTime = GetDate(newIncident);
            for (int i = existingIncidents.Count - 1; i >= 0; i--)
            {
                DateTime curIncidentTime = GetDate(existingIncidents[i]);
                if (newIncidentTime >= curIncidentTime)
                {
                    existingIncidents[i].AddAfterSelf(newIncident);
                    appended = true;
                    break;
                }
            }
            if (!appended)
            {
                incidents.AddFirst(newIncident);
            }
        }

        /// <summary>
        /// Removes the given file from the source directory
        /// </summary>
        /// <param name="file"></param>
        private static void RemoveFileFromSourceDirectory(FileInfo file)
        {
            if (file.Directory.FullName == ConfigurationManager.SourceDirectory.FullName)
            {
                file.Delete();
            }
        }

        /// <summary>
        /// Creates a new <see cref="XDocument"/> with an empty Incidents element and date attribute
        /// with value of <paramref name="date"/>
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private static XDocument CreateNewDigestXDoc(DateTime date)
        {
            XDocument doc = new XDocument();
            XElement incidentsElement = new XElement("Incidents");
            incidentsElement.SetAttributeValue("Date", date.ToString("MM/dd/yyyy"));
            doc.Add(incidentsElement);

            return doc;
        }

        /// <summary>
        /// Returns the appropriate digest <see cref="XDocument"/> based on <paramref name="date"/>.
        /// If <paramref name="currentDigestXML"/> is the appropriate digest then it is returned.
        /// Otherwise the appropriate digest xml object is loaded from a file if it exists or, if not,
        /// a new empty digest is returned.
        /// </summary>
        /// <param name="date"></param>
        /// <param name="currentDigestXML"></param>
        /// <returns></returns>
        private static XDocument GetDigestXML(DateTime date, XDocument currentDigestXML)
        {
            XDocument digestXML;
            if (currentDigestXML is null)
            {
                digestXML = GetDigestXML(date);
            }
            else
            {
                try
                {
                    DateTime currentDigestDate = DateTime.Parse(currentDigestXML.Element("Incidents").Attribute("Date").Value);
                    if (currentDigestDate == date)
                    {
                        digestXML = currentDigestXML;
                    }
                    else
                    {
                        digestXML = GetDigestXML(date);
                    }
                }
                catch (FormatException fex)
                {
                    digestXML = GetDigestXML(date);
                }
            }

            return digestXML;
        }

        /// <summary>
        /// Returns the appropriate digest <see cref="XDocument"/> based on <paramref name="date"/>.
        /// If the appropiate digest exists in <see cref="ConfigurationManager.ArchiveDirectory"/>,
        /// it is read in and returned. Otherwise, a new empty digest is returned.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private static XDocument GetDigestXML(DateTime date)
        {
            FileInfo digestFile = GetDigestFile(date);
            XDocument digestXML;
            if (digestFile.Exists && digestFile.Length > 0)
            {
                try
                {
                    digestXML = ParseXMLFromFile(digestFile);
                }
                catch (Exception ex)
                {
                    digestXML = CreateNewDigestXDoc(date);
                }
            }
            else
            {
                digestXML = CreateNewDigestXDoc(date);
            }

            return digestXML;
        }

        /// <summary>
        /// Read data from the given file and parses it into a <see cref="XDocument"/>.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static XDocument ParseXMLFromFile(FileInfo file)
        {
            XDocument doc;
            using (StreamReader reader = file.OpenText())
            {
                doc = XDocument.Load(reader);
            }
            return doc;            
        }

        /// <summary>
        /// Gets the path to the appropriate digest file based on <paramref name="date"/> and returns a
        /// <see cref="FileInfo"/>. Creates the file if no file exists at the path.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private static FileInfo GetDigestFile(DateTime date)
        {
            string fileName = $"{date.Month}-{date.Day}-{date.Year}.xml";
            string filePath = Path.Combine(ConfigurationManager.ArchiveDirectory.FullName, fileName);
            FileInfo file = new FileInfo(filePath);
            if (!file.Exists)
            {
                try
                {
                    using (file.Create()) ;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            return file;
        }

        /// <summary>
        /// Serializes <paramref name="digestXML"/> and writes it to <paramref name="digestFile"/>.
        /// </summary>
        /// <param name="digestXML"></param>
        /// <param name="digestFile"></param>
        private static void WriteDigestFile(XDocument digestXML, FileInfo digestFile)
        {
            using (FileStream fs = digestFile.OpenWrite())
            {
                digestXML.Save(fs);
            }
        }

        /// <summary>
        /// Returns the date of the given daily digest <see cref="XDocument"/>.
        /// </summary>
        /// <param name="digestXML"></param>
        /// <returns></returns>
        private static DateTime GetDate(XDocument digestXML)
        {
            XElement incident = digestXML.Element("Incident");
            return GetDate(incident);
        }

        /// <summary>
        /// Returns the date of the given incident.
        /// </summary>
        /// <param name="incidentElement"></param>
        /// <returns></returns>
        private static DateTime GetDate(XElement incidentElement)
        {
            XElement exposures = incidentElement.Element("Exposures");
            XElement exposure = exposures.Element("Exposure");
            XElement datetime = exposure.Element("AlarmDatetime");
            string dateString = datetime.Value;
            var dto = DateTimeOffset.Parse(dateString);
            return dto.DateTime;
        }
    }
}
