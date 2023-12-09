using System;
using System.Linq;

namespace Rezepte.Tests.Models
{
    public class TestResult
    {

        public string Description { get; set; }

        public bool Result { get; set; }

        public Exception Error { get; set; }

        public Action ActionMethod { get; set; }

        public Action InitMethod { get; set; }

        public Action CleanupMethod { get; set; }

    }

    public class TestResultEventArgs: EventArgs
    {

        public TestResultEventArgs(TestResult result)
        {
            Result = result;
        }

        public TestResult Result { get; }

    }
}
