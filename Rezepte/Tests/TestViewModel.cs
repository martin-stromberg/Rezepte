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
            Tests.Testing += (sender, e) => { TestResults.Add(new TestResultViewModel(e.Result)); };
            Tests.Result += (sender, e) =>
            {
                var vm = TestResults.FirstOrDefault(vm => vm.Description == e.Result.Description);
                if (vm == null)
                    TestResults.Add(new TestResultViewModel(e.Result));
                else
                    vm.Update(e.Result);
            };
            Tests.Run();
        }

        public ObservableCollection<TestResultViewModel> TestResults { get; } = new ObservableCollection<TestResultViewModel>();

    }
}
