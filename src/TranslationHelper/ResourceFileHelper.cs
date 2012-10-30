using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace TranslationHelper
{
    public class ResourceFileHelper : IDisposable
    {
        private readonly XDocument _xDocSource;
        private XDocument _xDocTarget;

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

        public void Dispose()
        {
            try { _xDocTarget.Save(TargetResourceFile); } 
            catch (Exception ex) { throw ex; }
        }
        
        public Dictionary<String, String> GetNameValuesFromSource(string value)
        {
            return GetNameValueDictionaryFromResourceFile(_xDocSource, value);
        }

        public Dictionary<String, String> GetAllNameValuesFromSource()
        {
            return GetAllValuesDictionaryFromResourceFile(_xDocSource);
        }

        public Dictionary<String, String> GetNameValuesFromTargetUsingValue(string value)
        {
            return GetNameValueDictionaryFromResourceFile(_xDocTarget, value);
        }

        public Dictionary<String, String> GetAllNameValuesFromTarget()
        {
            return GetAllValuesDictionaryFromResourceFile(_xDocTarget);
        }

        public String GetValueFromTargetUsingKey(string key)
        {
            var value = GetXElementForKeyOrDefault(_xDocTarget, key).Element(VALUE_ELEMENT).Value;
            return value;
        }
        
        public void WriteNameValuePairToTarget(string key, string value, bool overwrite)
        {
            var targetValue = GetXElementForKeyOrDefault(_xDocTarget, key);
            if ((targetValue.Element(VALUE_ELEMENT).Value != String.Empty) && (overwrite == false))
                    return;
                        
            targetValue.Element(VALUE_ELEMENT).Value = value;
        }

        public void SaveChangeToTarget()
        {
            _xDocTarget.Save(this.TargetResourceFile);
            _xDocTarget = XDocument.Load(TargetResourceFile);
        }

        #endregion

        #region Private Methods

        private Dictionary<string, string> GetNameValueDictionaryFromResourceFile(XDocument xDoc, string value)
        {
            var rootElement = ValidateXDocumentAsResourceFile(xDoc);

            var matchingElements = rootElement.Elements(DATA_ELEMENT).Where(se =>
                                                                            {
                                                                                var xElement = se.Element(VALUE_ELEMENT);
                                                                                return (xElement != null && xElement.Value.Trim().ToLower() == value.ToLower());
                                                                            }).ToArray();

            return (matchingElements.Any())
                        ? MatchingElementsToDictionary(matchingElements)
                        : new Dictionary<string, string>();
        }
        
        private Dictionary<string, string> GetAllValuesDictionaryFromResourceFile(XDocument xDoc)
        {
            var rootElement = ValidateXDocumentAsResourceFile(xDoc);

            var matchingElements = rootElement.Elements(DATA_ELEMENT).ToArray();

            return (matchingElements.Any())
                        ? MatchingElementsToDictionary(matchingElements)
                        : new Dictionary<string, string>();
        }

        private XElement GetXElementForKeyOrDefault(XDocument xDoc, string key)
        {
            var rootElement = ValidateXDocumentAsResourceFile(xDoc);

            var matchingElement = rootElement.Elements(DATA_ELEMENT)
                                             .SingleOrDefault(e => e.Attribute(DATA_NAME_ATTRIBUTE).Value.ToLower() == key.ToLower());
            if (matchingElement == null)
            {
                matchingElement = new XElement(DATA_ELEMENT,
                                    new XAttribute(DATA_NAME_ATTRIBUTE, key),
                                    new XAttribute(XNamespace.Xml + "space", "preserve"),
                                    new XElement(VALUE_ELEMENT, String.Empty));
                rootElement.Add(matchingElement);
            }

            return matchingElement;
        }

        private XElement ValidateXDocumentAsResourceFile(XDocument xDoc)
        {
            if (xDoc == null) 
                throw new Exception("The document file is not valid or points to a corrupt or locked file.  Please check the file and then proceed.");

            var rootElement = xDoc.Element(ROOT_ELEMENT);
            if (rootElement == null) 
                throw new Exception("No <root> element can be found in the file loaded.  Please verify that you have selected a resource file that follows the Microsoft ResX Schema version 2.0");

            return rootElement;
        }

        private Dictionary<string, string> MatchingElementsToDictionary(IEnumerable<XElement> matchingElements)
        {
            return matchingElements.Where(e => e.Attribute(DATA_NAME_ATTRIBUTE) != null & e.Element(VALUE_ELEMENT) != null)
                                   .ToDictionary(k => k.Attribute(DATA_NAME_ATTRIBUTE).Value,
                                                 v => v.Element(VALUE_ELEMENT).Value);
        }

        #endregion
    }
}
