using System;
using System.Collections.Generic;

namespace TranslationHelper.Helpers
{
    public interface IResourceFileHelper
    {
        Dictionary<string, string> GetNameValuesFromSource(string value);
        Dictionary<string, string> GetAllNameValuesFromSource();
        Dictionary<string, string> GetNameValuesFromTargetUsingValue(string value);
        Dictionary<string, string> GetAllNameValuesFromTarget();
        String GetValueFromTargetUsingKey(string key);
        void WriteNameValuePairToTarget(string key, string value, bool overwrite);
        void SaveChangeToTarget();
    }
}