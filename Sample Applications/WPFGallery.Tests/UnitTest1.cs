using NUnit.Framework;

namespace WPFGallery.Tests
{

    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test] // This will now resolve correctly
        public void Test1()
        {
            Assert.Pass();
        }
    }
}
