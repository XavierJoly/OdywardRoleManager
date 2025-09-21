using System.Threading;
using System.Threading.Tasks;
using _0900_OdywardRoleManager.Models;

namespace _0900_OdywardRoleManager.Services;

public interface IAuthService
{
    Task<AuthContext> AuthenticateAsync(CancellationToken cancellationToken = default);

    Task<AuthContext?> TryAcquireTokenSilentAsync(CancellationToken cancellationToken = default);
}
