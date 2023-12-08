using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Rezepte.Tests
{
    internal class TestViewModel
    {

        public void RunTestsAsync()
        {
            Task.Run(() => { RunTests(); });
        }

        private void RunTests()
        {
            Tests Tests = new Tests();
            Tests.Result += (sender, e) => { TestResults.Add(new TestResultViewModel(e.Result)); };
            Tests.Run();
        }

        public ObservableCollection<TestResultViewModel> TestResults { get; } = new ObservableCollection<TestResultViewModel>();

    }
}
