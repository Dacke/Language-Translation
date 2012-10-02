using System;
using System.IO;
using NUnit.Framework;
using TranslationHelper;

namespace TranslationHelperTests
{
    [TestFixture]
    public class ResourceFileHelperTests : SpecificationBase
    {
        private ResourceFileHelper sut;
        private string _targetFilePath;

        protected override void Given()
        {
            _targetFilePath = String.Format("{0}\\SampleResourceFiles\\target_{1}.resx", Environment.CurrentDirectory, Guid.NewGuid());
            File.Copy((Environment.CurrentDirectory + "\\SampleResourceFiles\\TargetTemplate.resx"), _targetFilePath);

            sut = new ResourceFileHelper((Environment.CurrentDirectory + "\\SampleResourceFiles\\EnglishSample.resx"), _targetFilePath);
        }

        protected override void CleanUp()
        {
            File.Delete(_targetFilePath);
        }
        
        [Then]
        public void WhenGettingValuesFromSource_OnlySingleValueFound()
        {
            var keys = sut.GetNameValuesFromSource("Only a single value should be found.");
            Assert.That(keys.Count, Is.EqualTo(1));
        }
    }
}
