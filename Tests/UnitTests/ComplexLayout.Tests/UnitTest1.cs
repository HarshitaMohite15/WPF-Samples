using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace ComplexLayout.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void ComplexLayoutExe_LaunchesSuccessfully()
        {
            var exePath = Path.GetFullPath(@"ComplexLayout.exe");
            Assert.That(File.Exists(exePath), $"Executable not found at: {exePath}");

            using (var process = new Process())
            {
                process.StartInfo.FileName = exePath;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                // Give the process a moment to start
                Thread.Sleep(1000);

                Assert.That(!process.HasExited, Is.True, "Process exited immediately after launch.");

                // Clean up
                process.Kill(entireProcessTree: true);
            }
        }
    }
}
