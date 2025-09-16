using System.Diagnostics;
using NUnit.Framework;
using System.IO;
using System.Threading;
using System.Windows.Automation;

namespace AlternatingAppearanceOfItems.Tests
{
    public class Tests
    {
        private Process _appProcess;
        private AutomationElement _mainWindow;

        [SetUp]
        public void Setup()
        {
            // Adjust the path to your application's EXE as needed           // var exePath = Path.GetFullPath(@"..\..\..\..\..\UnitTests\AlternatingAppearanceOfItems.Tests\bin\Debug\net10.0-windows\AlternatingAppearanceOfItems.exe");
           
        }

        [TearDown]
        public void TearDown()
        {
            if (_appProcess != null)
            {
                if (!_appProcess.HasExited)
                    _appProcess.Kill();
                _appProcess.Dispose();
                _appProcess = null;
            }
        }

        [Test]
        public void App_Launches_MainWindow_Successfully()
        {
            Exception threadException = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var app = new App(); 
                    var window = new MainWindow(); 
                    app.Startup += (s, e) =>
                    {                        
                        Assert.IsNotNull(window);
                        Assert.IsTrue(window.IsLoaded == false); 
                        window.Show();
                    };
                    app.Run();
                }
                catch (Exception ex)
                {
                    threadException = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            // Give the window time to open
            Thread.Sleep(1000);

            // Signal the app to shutdown
            thread.Interrupt();
            // Wait for the thread to finish
            thread.Join(2000);

            Assert.IsNull(threadException, $"App threw exception: {threadException}");
        }
    }
}