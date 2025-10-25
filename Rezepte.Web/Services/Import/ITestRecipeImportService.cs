using System.Threading;
using System.Threading.Tasks;

namespace Rezepte.Web.Services.Import
{
    public interface ITestRecipeImportService
    {
        Task<string[]> GetTestUrlsAsync(CancellationToken ct = default);
        Task<bool> HasTestUrlsAsync(CancellationToken ct = default);
    }
}