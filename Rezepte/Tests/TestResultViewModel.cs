using Rezepte.Tests.Models;
using Rezepte.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;

namespace Rezepte.Tests
{
    internal class TestResultViewModel: BaseViewModel
    {

        private TestResult _result;

        public TestResultViewModel(TestResult result)
        {
            Update(result);
        }

        public void Update(TestResult result)
        {
            skipRunning = true;
            Result = result.Result;
            Description = result.Description;
            Error = result.Error?.ToString();
            HasError = !string.IsNullOrEmpty(Error);
            _result = result;
            skipRunning = false;
        }

        private bool skipRunning = false;

        private void RunTest()
        {
            if (skipRunning || (_result == null))
                return;
            Debugger.Break();
            try
            {
                _result.InitMethod();
                _result.ActionMethod();
                _result.CleanupMethod();
                skipRunning = true;
                Result = true;
                Error = string.Empty;
                HasError = !string.IsNullOrEmpty(Error);
                skipRunning = false;
            }
            catch (Exception ex)
            {
                Error = ex.ToString();
                HasError = !string.IsNullOrEmpty(Error);
                Result = false;
            }
        }

        public bool Result
        {
            get
            {
                return GetProperty<bool>();
            }
            set
            {
                bool changed = value != Result;
                SetProperty<bool>(value);
                if (value && changed)
                    RunTest();
            }
        }

        public string Description
        {
            get
            {
                return GetProperty<string>();
            }
            set
            {
                SetProperty<string>(value);
            }
        }

        public string Error
        {
            get
            {
                return GetProperty<string>();
            }
            set
            {
                SetProperty<string>(value);
            }
        }

        public bool HasError
        {
            get
            {
                return GetProperty<bool>();
            }
            set
            {
                SetProperty<bool>(value);
            }
        }

    }
}
