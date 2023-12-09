using Rezepte.Tests.Models;
using System;
using System.Linq;

namespace Rezepte.Tests
{
    public class TestResultException: ApplicationException
    {

        public TestResultException(string message)
            : base(message) { }

        public TestResultException(string message, Exception innerException)
            : base(message, innerException) { }

    }

    internal abstract class BaseTest
    {

        public BaseTest() { }

        public BaseTest(params BaseTest[] childTests)
            : this()
        {
            _childTests = childTests;
        }

        private BaseTest[] _childTests;

        public IEnumerable<BaseTest> ChildTests => _childTests;

        protected abstract void Process();

        public virtual void Init() { }

        public virtual void Cleanup() { }

        public void Run()
        {
            try
            {
                Process();
            }
            catch (Exception ex)
            {
                ;
            }
        }

        protected void AddTest(string description, Action testInit, Action testCleanup, Action testAction)
        {
            TestResult result = new TestResult() { Description = description, Result = false };
            try
            {
                OnTesting(new TestResultEventArgs(result));
                testInit();
                testAction();
                result.Result = true;
            }
            catch (TestResultException ex)
            {
                result.Error = ex;
            }
            catch (Exception ex)
            {
                result.Error = ex;
            }
            finally
            {
                OnResult(new TestResultEventArgs(result));
                try
                {
                    testCleanup();
                }
                catch (Exception ex)
                {
                    OnResult(new TestResultEventArgs(new TestResult()
                        {
                            Description = $"Cleanup of {result.Description}",
                            Result = false,
                            Error = ex
                        }));
                }
            }
        }

        private void OnResult(TestResultEventArgs e)
        {
            Result?.Invoke(this, e);
        }

        public event EventHandler<TestResultEventArgs> Result;

        private void OnTesting(TestResultEventArgs e)
        {
            Testing?.Invoke(this, e);
        }

        public event EventHandler<TestResultEventArgs> Testing;

        protected void CheckIsTrue(bool actual, string message = "")
        {
            if (!actual)
                throw new TestResultException($"Condition is not true. {message}".Trim());
        }

        protected void CheckIsFalse(bool actual, string message = "")
        {
            if (actual)
                throw new TestResultException($"Condition is not false. {message}".Trim());
        }

        protected void CheckAreEqual(object expected, object actual, string message = "")
        {
            if ((expected == null) && (actual == null))
                return;
            if (expected == null)
                throw new TestResultException($"expected object is null, actual object is not null. {message}".Trim());
            if (actual == null)
                throw new TestResultException($"actual object is null, expected object is not null. {message}".Trim());
            var expectedType = expected.GetType();
            var actualType = actual.GetType();
            if (expectedType != actualType)
                throw new TestResultException($"expected type {expectedType} differs from actual type {actualType}. {message}".Trim());

            if (!expectedType.Equals(actualType))
                throw new TestResultException($"objects are not equal. {message}".Trim());
        }

        protected void CheckThrows<T>(Action action, string message = "") where T: Exception
        {
            try
            {
                action();
                throw new TestResultException($"Method call does not throw exception. {message}".Trim());
            }
            catch (Exception ex)
            {
                var expectedType = typeof(T);
                var actualType = ex.GetType();
                if (expectedType != actualType)
                    throw new TestResultException($"Method call throws exception of type {actualType}. Exception type {expectedType} was expected. {message}".Trim());
            }
        }

        protected void CheckIsNotNull(object value, string message = "")
        {
            if (value == null)
                throw new TestResultException($"Object is null. {message}".Trim());
        }

        protected void CheckIsNull(object value, string message = "")
        {
            if (value != null)
                throw new TestResultException($"Object is not null. {message}".Trim());
        }

    }
}
