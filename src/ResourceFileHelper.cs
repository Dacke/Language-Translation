using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace TranslationHelper
{
    public class ResourceFileHelper
    {
        private readonly XDocument _xDocSource;
        private readonly XDocument _xDocTarget;

        private const string ROOT_ELEMENT = "root";
        private const string DATA_ELEMENT = "data";
        private const string DATA_NAME_ATTRIBUTE = "name";
        private const string VALUE_ELEMENT = "value";

        #region Properties

        public String EnglishResourceFile { get; private set; }
        public String TargetResourceFile { get; private set; }

        #endregion

        #region Public Methods

        public ResourceFileHelper(String englishResourceFile, String targetResourceFile)
        {
            EnglishResourceFile = englishResourceFile;
            TargetResourceFile = targetResourceFile;

            _xDocSource = XDocument.Load(EnglishResourceFile);
            _xDocTarget = XDocument.Load(TargetResourceFile);
        }
        
        public Dictionary<String, String> GetNameValuesFromSource(string value)
        {
            return GetNameValueDictionaryFromResourceFile(_xDocSource, value);
        }

        public Dictionary<String, String> GetNameValuesFromTarget(string value)
        {
            return GetNameValueDictionaryFromResourceFile(_xDocTarget, value);
        }


        #endregion

        #region Private Methods

        private Dictionary<string, string> GetNameValueDictionaryFromResourceFile(XDocument xDoc, string value)
        {
            if (xDoc == null) throw new Exception("The document file is not valid or points to a corrupt or locked file.  Please check the file and then proceed.");

            var rootElement = _xDocSource.Element(ROOT_ELEMENT);
            if (rootElement == null) throw new Exception("No <root> element can be found in the file loaded.  Please verify that you have selected a resource file that follows the Microsoft ResX Schema version 2.0");

            var matchingElements = rootElement.Elements(DATA_ELEMENT).Where(se =>
            {
                var xElement = se.Element(VALUE_ELEMENT);
                return (xElement != null && xElement.Value.Trim().ToLower() == value);
            }).ToArray();

            return (matchingElements.Any())
                       ? matchingElements.Where(e => e.Attribute(DATA_NAME_ATTRIBUTE) != null & e.Element(VALUE_ELEMENT) != null)
                                         .ToDictionary(k => k.Attribute(DATA_NAME_ATTRIBUTE).Value,
                                                       v => v.Element(VALUE_ELEMENT).Value)
                       : new Dictionary<string, string>();
        }

        #endregion
    }
}
