using Rezepte.Tests.Models;
using System;
using System.Linq;

namespace Rezepte.Tests
{
    internal class TestResultViewModel
    {

        public TestResultViewModel(TestResult result)
        {
            Result = result.Result;
            Description = result.Description;
            Error = result.Error?.ToString();
            HasError = !string.IsNullOrEmpty(Error);
        }

        public bool Result { get; }

        public string Description { get; }

        public string Error { get; }

        public bool HasError { get; }

    }
}
