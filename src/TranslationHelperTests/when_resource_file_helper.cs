using System;
using System.IO;
using NUnit.Framework;
using TranslationHelper.Helpers;

namespace TranslationHelperTests
{
    [TestFixture]
    public class when_resource_file_helper_spec : SpecificationBase
    {
        private ResourceFileHelper sut;
        private string _sourceFilePath;
        private string _targetFilePath;

        protected override void Given()
        {
            _sourceFilePath = (Environment.CurrentDirectory + "\\SampleResourceFiles\\EnglishSample.resx");
            _targetFilePath = String.Format("{0}\\SampleResourceFiles\\target_{1}.resx", Environment.CurrentDirectory, Guid.NewGuid());
            File.Copy((Environment.CurrentDirectory + "\\SampleResourceFiles\\TargetTemplate.resx"), _targetFilePath);

            sut = new ResourceFileHelper(_sourceFilePath, _targetFilePath);
        }

        protected override void CleanUp()
        {
            File.Delete(_targetFilePath);
        }
        
        [Then]
        public void WhenGettingKeysFromSource_OnlySingleValueFound()
        {
            var keys = sut.GetNameValuesFromSource("Only a single key should be found.");
            Assert.That(keys.Count, Is.EqualTo(1));
        }

        [Then]
        public void WhenGettingKeysFromSource_ThreeValuesFound()
        {
            var keys = sut.GetNameValuesFromSource("Multiple Keys Matching.");
            Assert.That(keys.Count, Is.EqualTo(3));
        }

        [Then]
        public void WhenGettingKeys_ValueNotFound()
        {
            var keys = sut.GetNameValuesFromSource(String.Format("Value that you will not find in the resource file {{0}}" + Guid.NewGuid()));
            Assert.That(keys, Is.Not.Null);
            Assert.That(keys, Is.Empty);
        }

        [Then]
        public void WhenGettingMatchingKeysInTarget()
        {
            var keys = sut.GetNameValuesFromTargetUsingValue("Value Found In Target File");
            Assert.That(keys.Count, Is.EqualTo(1));
        }

        [Then]
        public void WhenGettingAllValuesFromSource()
        {
            var keys = sut.GetAllNameValuesFromSource();
            Assert.That(keys.Count, Is.EqualTo(5));
        }

        [Then]
        public void WhenGettingAllValuesFromTarget()
        {
            var keys = sut.GetAllNameValuesFromTarget();
            Assert.That(keys.Count, Is.EqualTo(1));
        }

        [Then]
        public void WhenWritingValuesToTarget()
        {
            Assert.DoesNotThrow(() => sut.WriteNameValuePairToTarget("WhenWritingValuesToTarget_TestKey", "Test Value", false));
            Assert.DoesNotThrow(() => sut.WriteNameValuePairToTarget("WhenWritingValuesToTarget_TestKey", "Test Value", true));
        }

        [Then]
        public void WhenWritingValuesToTargetWithoutOverwrite()
        {
            const string key = "WhenWritingValuesToTargetWithoutOverwrite_TestKey";
            const string goodValue = "This value is a test value for test WhenWritingValuesToTargetWithoutOverwrite";
            const string badValue = "This value is a bad test value for test WhenWritingValuesToTargetWithoutOverwrite";

            Assert.DoesNotThrow(() => sut.WriteNameValuePairToTarget(key, goodValue, true));
            Assert.DoesNotThrow(() => sut.SaveChangeToTarget());
            Assert.That(sut.GetValueFromTargetUsingKey(key), Is.EqualTo(goodValue));

            Assert.DoesNotThrow(() => sut.WriteNameValuePairToTarget(key, badValue, false));
            Assert.DoesNotThrow(() => sut.SaveChangeToTarget());

            Assert.That(sut.GetValueFromTargetUsingKey(key), Is.Not.EqualTo(badValue));
        }

        [Then]
        public void WhenValidatingValuesWrittenToTarget()
        {
            const string key = "WhenValidatingValuesWrittenToTarget_TestKey";
            const string value = "This value is a test value";

            Assert.DoesNotThrow(() => sut.WriteNameValuePairToTarget(key, value, false));
            Assert.DoesNotThrow(() => sut.SaveChangeToTarget());
            Assert.That(sut.GetValueFromTargetUsingKey(key), Is.EqualTo(value));
        }
        
        [Then]
        public void WhenSavingChangesToTarget()
        {
            Assert.DoesNotThrow(() => sut.WriteNameValuePairToTarget("New_Key", "New value that was not found in the target before.", false));
            Assert.DoesNotThrow(() => sut.SaveChangeToTarget());
        }

        [Then]
        public void WhenOverwritingExistingValuesToTarget()
        {
            Assert.DoesNotThrow(() => sut.WriteNameValuePairToTarget("Key1", "Key1Value", false));
            Assert.DoesNotThrow(() => sut.WriteNameValuePairToTarget("Key2", "Key2Value", false));
            Assert.DoesNotThrow(() => sut.WriteNameValuePairToTarget("Key3", "Key3Value", false));
            Assert.DoesNotThrow(() => sut.WriteNameValuePairToTarget("Key4", "Key4Value", false));
            Assert.DoesNotThrow(() => sut.SaveChangeToTarget());

            using (var rfhOne = new ResourceFileHelper(_sourceFilePath, _targetFilePath))
            {
                Assert.That(rfhOne.GetNameValuesFromTargetUsingValue("Key1Value").Count, Is.EqualTo(1));
                Assert.That(rfhOne.GetNameValuesFromTargetUsingValue("Key2Value").Count, Is.EqualTo(1));
                Assert.That(rfhOne.GetNameValuesFromTargetUsingValue("Key3Value").Count, Is.EqualTo(1));
                Assert.That(rfhOne.GetNameValuesFromTargetUsingValue("Key4Value").Count, Is.EqualTo(1));

                Assert.DoesNotThrow(() => rfhOne.WriteNameValuePairToTarget("Key1", "NewKey1Value", true));
                Assert.DoesNotThrow(() => rfhOne.SaveChangeToTarget());
            }

            using (var rfhTwo = new ResourceFileHelper(_sourceFilePath, _targetFilePath))
            {
                Assert.That(rfhTwo.GetNameValuesFromTargetUsingValue("NewKey1Value").Count, Is.EqualTo(1));
            }
        }

        [Then]
        public void WhenDisposingChangesAreSavedToTarget()
        {
            const string testKey = "WhenDisposingChangesAreSavedToTarget_Key";
            const string testValue = "Value that is to be saved to the target upon dispose.";

            using (var sutOne = new ResourceFileHelper(_sourceFilePath, _targetFilePath))
            {
                Assert.DoesNotThrow(() => sutOne.WriteNameValuePairToTarget(testKey, testValue, false));
                Assert.DoesNotThrow(() => sut.SaveChangeToTarget());
            }

            var sutTwo = new ResourceFileHelper(_sourceFilePath, _targetFilePath);
            Assert.That(sutTwo.GetValueFromTargetUsingKey(testKey), Is.EqualTo(testValue));
        }
    }
}
