using OmlUtilities.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using static OmlUtilities.Core.Oml.OmlHeader;
using static OmlUtilities.Core.Oml;

namespace OML_Utilities_Library
{
    public class OmlUtilities
    {
        // Function to get a stream
        protected Stream _GetStream(string path, bool isInput)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("The " + (isInput ? "input" : "output") + " argument is mandatory.");
            }

            if (path.StartsWith("pipe:", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new NotSupportedException("Piping is not supported in library mode.");
            }
            else
            {
                return isInput ? File.OpenRead(path) : File.OpenWrite(path);
            }
        }

        // Function to get an OML instance
        protected Oml _GetOmlInstance(string input, string version)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentException("The input argument is mandatory.");
            }
            if (string.IsNullOrEmpty(version))
            {
                throw new ArgumentException("The platform version argument is mandatory.");
            }

            if (version.Equals("OL", StringComparison.InvariantCultureIgnoreCase))
            {
                AssemblyUtility.PlatformVersion = PlatformVersion.LatestSupportedVersion;
            }
            else
            {
                PlatformVersion platformVersion = PlatformVersion.Versions.FirstOrDefault(p => p.Label.Equals(version, StringComparison.InvariantCultureIgnoreCase));
                if (platformVersion == null)
                {
                    throw new Exception("Platform version \"" + version + "\" not recognized. Please run ShowPlatformVersions in order to list supported versions.");
                }
                AssemblyUtility.PlatformVersion = platformVersion;
            }

            Stream stream = _GetStream(input, true);
            if (stream.CanSeek)
            {
                return new Oml(stream);
            }
            else
            {
                MemoryStream memoryStream = new MemoryStream();
                byte[] buffer = new byte[32 * 1024]; // 32K buffer for example
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    memoryStream.Write(buffer, 0, bytesRead);
                }
                memoryStream.Position = 0;

                return new Oml(memoryStream);
            }
        }

        // Method to show platform versions
        public IEnumerable<string> ShowPlatformVersions(bool onlyLatest = false, bool showFullVersion = false)
        {
            if (onlyLatest)
            {
                yield return showFullVersion ? PlatformVersion.LatestSupportedVersion.Version.ToString() : PlatformVersion.LatestSupportedVersion.ToString();
            }
            else
            {
                foreach (PlatformVersion version in PlatformVersion.Versions)
                {
                    yield return showFullVersion ? version.Version.ToString() : version.ToString();
                }
            }
        }

        // Method to show headers
        public IEnumerable<string> ShowHeaders(string input, string version, string headerName = null)
        {
            Oml oml = _GetOmlInstance(input, version);
            bool found = false;

            foreach (PropertyInfo property in typeof(OmlHeader).GetProperties())
            {
                OmlHeaderAttribute attribute = (OmlHeaderAttribute)Attribute.GetCustomAttribute(property, typeof(OmlHeaderAttribute));
                if (attribute == null || (!string.IsNullOrEmpty(headerName) && !headerName.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    continue;
                }

                yield return !string.IsNullOrEmpty(headerName) ? property.GetValue(oml.Header, null).ToString() : $"{property.Name}:{property.GetValue(oml.Header, null)}";
                found = true;
            }

            if (!string.IsNullOrEmpty(headerName) && !found)
            {
                throw new Exception("Header name \"" + headerName + "\" was not found.");
            }
        }

        // Method to show fragments
        public IEnumerable<string> ShowFragments(string input, string version, string fragmentName = null)
        {
            Oml oml = _GetOmlInstance(input, version);

            if (string.IsNullOrEmpty(fragmentName))
            {
                foreach (string innerFragmentName in oml.GetFragmentNames())
                {
                    yield return innerFragmentName;
                }
            }
            else
            {
                XElement fragment = oml.GetFragmentXml(fragmentName);
                if (fragment == null)
                {
                    throw new Exception("Unable to get XML content of fragment \"" + fragmentName + "\".");
                }
                yield return fragment.ToString(SaveOptions.DisableFormatting);
            }
        }

        // Method to manipulate OML
        public void Manipulate(string input, string output, string version, string format = null, List<string> headers = null, List<string> fragments = null)
        {
            Oml oml = _GetOmlInstance(input, version);

            // Set headers
            if (headers != null)
            {
                foreach (string headerLine in headers)
                {
                    int colonIndex = headerLine.IndexOf(':');
                    if (colonIndex == -1)
                    {
                        throw new Exception("Unable to parse header value \"" + headerLine + "\". Name and value must be separated by colon (':').");
                    }

                    string headerName = headerLine.Substring(0, colonIndex);
                    if (string.IsNullOrEmpty(headerName))
                    {
                        throw new Exception("The header name in the header parameter is mandatory.");
                    }

                    bool found = false;
                    foreach (PropertyInfo property in typeof(OmlHeader).GetProperties())
                    {
                        OmlHeaderAttribute attribute = (OmlHeaderAttribute)Attribute.GetCustomAttribute(property, typeof(OmlHeaderAttribute));
                        if (attribute == null || !headerName.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase))
                        {
                            continue;
                        }

                        if (attribute.IsReadOnly)
                        {
                            throw new Exception("Cannot change header \"" + property.Name + "\" because it is read-only.");
                        }

                        string headerValue = headerLine.Substring(colonIndex + 1);
                        property.SetValue(oml.Header, headerValue);
                        found = true;
                    }

                    if (!found)
                    {
                        throw new Exception("Header name \"" + headerName + "\" was not found.");
                    }
                }
            }

            // Set fragments
            if (fragments != null)
            {
                foreach (string fragmentLine in fragments)
                {
                    int colonIndex = fragmentLine.IndexOf(':');
                    if (colonIndex == -1)
                    {
                        throw new Exception("Unable to parse fragment value \"" + fragmentLine + "\". Name and value must be separated by colon (':').");
                    }

                    string fragmentName = fragmentLine.Substring(0, colonIndex);
                    if (string.IsNullOrEmpty(fragmentName))
                    {
                        throw new Exception("The fragment name in the fragment parameter is mandatory.");
                    }

                    XElement fragment = XElement.Parse(fragmentLine.Substring(colonIndex + 1));
                    oml.SetFragmentXml(fragmentName, fragment);
                }
            }

            // Save manipulated OML
            Stream outputStream = _GetStream(output, false);
            if (format != null && format.Equals("xml", StringComparison.InvariantCultureIgnoreCase) || format == null && output.EndsWith(".xml", StringComparison.InvariantCultureIgnoreCase))
            {
                StreamWriter sw = new StreamWriter(outputStream);
                sw.Write(oml.GetXml().ToString(SaveOptions.DisableFormatting)); // Export XML
                sw.Flush();
                sw.Close();
            }
            else
            {
                oml.Save(outputStream); // Export OML
            }

            outputStream.Close();
        }

        // Method to search text in OML
        public IEnumerable<string> TextSearch(string omlPathDir, string keywordSearch, string version)
        {
            if (string.IsNullOrEmpty(keywordSearch))
            {
                throw new ArgumentException("Please inform an expression for search and try again.");
            }

            if (!Directory.Exists(omlPathDir))
            {
                throw new DirectoryNotFoundException("Directory not found.");
            }

            var results = new List<string>();
            var watch = System.Diagnostics.Stopwatch.StartNew();

            DirectoryInfo omlDir = new DirectoryInfo(omlPathDir);
            FileInfo[] files = omlDir.GetFiles("*.oml");
            results.Add($"{files.Length} files found.");

            int countFile = 0;
            foreach (FileInfo file in files)
            {
                Oml oml = _GetOmlInstance(file.FullName, version);
                string txtXml = oml.GetXml().ToString();

                int count = 0;
                int i = 0;
                while ((i = txtXml.IndexOf(keywordSearch, i)) != -1)
                {
                    i += keywordSearch.Length;
                    count++;
                }

                countFile++;
                results.Add($"[{countFile}/{files.Length}] - {count} occurrences found in {file.Name}.");
            }

            watch.Stop();
            TimeSpan elapsedMS = TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds);
            string formatElapsedTime = string.Format("{0:D2}h:{1:D2}m:{2:D2}s", elapsedMS.Hours, elapsedMS.Minutes, elapsedMS.Seconds);
            results.Add($"Elapsed time {formatElapsedTime}.");

            return results;
        }
    }
}
