using Levrum.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Levrum.Data.Sources
{
    public class XmlUtils
    {
        public static ElementPath GetPathToNode(XElement node)
        {
            StringBuilder stringBuilder = new StringBuilder(node.Name.LocalName);
            XElement parent = node.Parent;
            while (!(parent is null))
            {
                stringBuilder.Insert(0, $"{parent.Name.LocalName}/");
                parent = parent.Parent;
            }
            return new ElementPath(stringBuilder.ToString());
        }

        public static XElement GetNodeFromPath(XContainer containerNode, ElementPath path)
        {
            IEnumerable<XElement> matchingDecendants = containerNode.Descendants(path.PathEnd);
            XElement node = matchingDecendants.FirstOrDefault(e => GetPathToNode(e).Equals(path));

            return node;
        }

        public static List<string> GetColumns(Stream stream)
        {
            XDocument doc = XDocument.Load(stream);
            var allElements = doc.Descendants();
            var elementPaths = allElements.Select(e => GetPathToNode(e));
            var uniquePaths = elementPaths.Select(ep => ep.Path).Distinct();

            return uniquePaths.ToList();
        }

        public static List<string> GetColumnValues(string column, Stream stream)
        {
            ElementPath columnPath = new ElementPath(column);
            XDocument doc = XDocument.Load(stream);
            string elementName = columnPath.PathEnd;
            var matchingElements = doc.Descendants(elementName).Where(e => GetPathToNode(e).Equals(columnPath));
            List<string> columnValues = matchingElements.Select(e => e.Value).ToList();

            return columnValues;
        }

        public static List<Record> GetRecords(Stream stream, string incidentNode, string responseNode)
        {
            XDocument doc = XDocument.Load(stream);

            ElementPath incidentPath = new ElementPath(incidentNode);
            ElementPath responsePath = new ElementPath(responseNode);

            ElementPath incidentParentPath = incidentPath.ParentPath;
            ElementPath responseParentPath = responsePath.ParentPath;

            XElement incidentParent = GetNodeFromPath(doc, incidentParentPath);

            List<Record> records = new List<Record>();
            var incidents = incidentParent.Elements(incidentPath.PathEnd).Where(e => GetPathToNode(e).Equals(incidentPath));
            foreach (XElement incident in incidents)
            {
                // Create a record for each response and add the values
                XElement responseParent = incident.Descendants(responseParentPath.PathEnd).First(e => GetPathToNode(e).Equals(responseParentPath));
                var responses = responseParent.Elements(responsePath.PathEnd).Where(e => GetPathToNode(e).Equals(responsePath));
                List<Record> responseRecords = new List<Record>();
                foreach (XElement response in responses)
                {
                    Record record = new Record();
                    var responseDescendants = response.Descendants();
                    foreach (XElement descendant in responseDescendants)
                    {
                        ElementPath descendantPath = GetPathToNode(descendant);
                        string descendantValue = descendant.Value;

                        if (!record.Data.ContainsKey(descendantPath.Path))
                            record.AddValue(descendantPath.Path, descendantValue);
                        else
                            record.AddValue(descendantPath.Path, "MULTIPLE VALUES");
                    }
                    responseRecords.Add(record);
                }

                // Add the incident values to each response
                responseParent.Remove();
                var incidentDescendants = incident.Descendants();
                foreach (XElement descendant in incidentDescendants)
                {
                    ElementPath descendantPath = GetPathToNode(descendant);
                    string descendantValue = descendant.Value;

                    foreach (Record record in responseRecords)
                    {
                        record.AddValue(descendantPath.Path, descendantValue);
                    }
                }

                records.AddRange(responseRecords);
            }

            return records;
        }

        public static Stream GetXmlStream(string compressedContents, FileInfo xmlFile)
        {
            Stream stream = null;

            if (!string.IsNullOrEmpty(compressedContents))
            {
                string xmlContents = LZString.decompressFromUTF16(compressedContents);
                stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlContents));
            }
            else
            {
                stream = xmlFile.OpenRead();
            }

            return stream;
        }
    }

    public class ElementPath : IEquatable<ElementPath>
    {
        public string Path { get; set; }
        public List<string> PathComponents { get; set; }
        public string PathRoot => PathComponents?[0];
        public string PathEnd => PathComponents?.Last();
        public ElementPath ParentPath => new ElementPath(string.Join("/", PathComponents.GetRange(0, PathComponents.Count - 1)));

        public ElementPath(string path)
        {
            Path = path;
            PathComponents = path.Split('/').ToList();
        }

        public override string ToString()
        {
            return Path;
        }

        public bool Equals(ElementPath other)
        {
            if (other is null)
                return false;

            return Path == other.Path;
        }

        public override bool Equals(object obj)
        {
            return obj is ElementPath path &&
                   Equals(obj as ElementPath);
        }

        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }
    }
}
