using System;
using System.IO;
using System.Xml.Linq;
using OutSystems.Oml;

namespace OMLtoXML
{
    public class OMLUtilitiesOMLtoXML
    {
        private XDocument GetXml(Oml oml)
        {
            var root = new XElement("OML");
            foreach (var fragmentName in oml.DumpFragmentsNames())
            {
                var fragment = GetFragmentXml(oml, fragmentName);
                fragment.SetAttributeValue("FragmentName", fragmentName);
                root.Add(fragment);
            }
            return new XDocument(root);
        }

        private XElement GetFragmentXml(Oml oml, string fragmentName)
        {
            using (var reader = oml.GetFragmentXmlReader(fragmentName))
            {
                return reader.ToXElement();
            }
        }

        public string ConvertOMLtoXML(string inputPath)
        {
            try
            {
                var fileBytes = File.ReadAllBytes(inputPath);
                return ConvertOMLtoXML(fileBytes);
            }
            catch (Exception ex)
            {
                // Log the exception or rethrow it
                // For simplicity, we'll just return the error message
                return $"Error: {ex.Message}";
            }
        }

        public string ConvertOMLtoXML(byte[] fileBytes)
        {
            try
            {
                var oml = Oml.LoadWithoutUpgrades(fileBytes, "");
                return GetXml(oml).ToString(SaveOptions.DisableFormatting);
            }
            catch (Exception ex)
            {
                // Log the exception or rethrow it
                // For simplicity, we'll just return the error message
                return $"Error: {ex.Message}";
            }
        }
    }
}
