using Rezepte.Tests.Models;
using Rezepte.ViewModels;
using System;
using System.Linq;

namespace Rezepte.Tests
{
    internal class TestResultViewModel: BaseViewModel
    {

        public TestResultViewModel(TestResult result)
        {
            Update(result);
        }

        public void Update(TestResult result)
        {
            Result = result.Result;
            Description = result.Description;
            Error = result.Error?.ToString();
            HasError = !string.IsNullOrEmpty(Error);
        }

        public bool Result
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
