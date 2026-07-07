using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Implementations
{
    internal class UserRepository : CreateableBindableBase<IUserRepository, UserRepository, IPersistenceBase>, IUserRepository
    {
        protected override void OnCreate(IPersistenceBase @base)
        {
        }

        protected override Task OnCreateAsync(IPersistenceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}