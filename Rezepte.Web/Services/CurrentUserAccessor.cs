using System.Security.Claims;
using System.Threading.Tasks;

namespace Rezepte.Web.Services;

public class CurrentUserAccessor
{
    private ClaimsPrincipal? _user;
    private TaskCompletionSource<ClaimsPrincipal?> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

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