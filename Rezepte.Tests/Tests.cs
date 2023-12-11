using Rezepte.Tests.Models;
using Rezepte.Tests.Services;
using Rezepte.Tests.Services.Chefkoch;
using Rezepte.Tests.Services.Database;
using System;
using System.Linq;

namespace Rezepte.Tests
{
    public class Tests
    {

        private void Init() { }

        private void Clean() { }

        public void Run()
        {
            try
            {
                try
                {
                    RunTest(new CockingDatabaseTests());
                    RunTest(new ChefkochSiteTests());
                    RunTest(new ReceiptLibraryTests());
                    RunTest(new ReceiptTests());
                }
                finally
                {
                    Clean();
                }
            }
            catch
            {
                ;
            }
        }

        private void RunTest(BaseTest test)
        {
            test.Testing += Test_Testing;
            test.Result += Test_Result;
            test.Init();
            try
            {
                test.Run();
                if (test.ChildTests != null)
                    foreach (var child in test.ChildTests)
                        RunTest(child);
            }
            finally
            {
                test.Cleanup();
            }
        }

        private void Test_Testing(object sender, TestResultEventArgs e)
        {
            Testing?.Invoke(sender, e);
        }

        private void Test_Result(object sender, TestResultEventArgs e)
        {
            Result?.Invoke(sender, e);
        }

        public event EventHandler<TestResultEventArgs> Result;

        public event EventHandler<TestResultEventArgs> Testing;

    }
}
