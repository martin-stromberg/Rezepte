using System.Threading;
using System.Threading.Tasks;

namespace Rezepte.Web.Services.Import
{
    /// <summary>
    /// Defines the itest recipe import service interface.
    /// </summary>
    public interface ITestRecipeImportService
    {
        /// <summary>
        /// Gets the test urls async.
        /// </summary>
        /// <param name="ct">The ct parameter.</param>
        /// <returns>The result.</returns>
        Task<string[]> GetTestUrlsAsync(CancellationToken ct = default);
        /// <summary>
        /// Determines whether test urls async.
        /// </summary>
        /// <param name="ct">The ct parameter.</param>
        /// <returns>The result.</returns>
        Task<bool> HasTestUrlsAsync(CancellationToken ct = default);
    }
}
