using System.Security.Claims;
using System.Threading.Tasks;

namespace Rezepte.Web.Services;

/// <summary>
/// Represents the current user accessor class.
/// </summary>
public class CurrentUserAccessor
{
    private ClaimsPrincipal? _user;
    private TaskCompletionSource<ClaimsPrincipal?> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Represents the public class.
    /// </summary>
    public ClaimsPrincipal? User
    {
        get => _user;
        set
        {
            _user = value;
            if (!_tcs.Task.IsCompleted)
            {
                _tcs.TrySetResult(_user);
            }
        }
    }

    /// <summary>
    /// waits the for user async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public Task<ClaimsPrincipal?> WaitForUserAsync(CancellationToken ct = default)
    {
        if (_user is not null)
            return Task.FromResult<ClaimsPrincipal?>(_user);

        if (ct.CanBeCanceled)
        {
            var tcs = _tcs;
            ct.Register(() => tcs.TrySetCanceled(ct));
            return tcs.Task;
        }
        return _tcs.Task;
    }
}
