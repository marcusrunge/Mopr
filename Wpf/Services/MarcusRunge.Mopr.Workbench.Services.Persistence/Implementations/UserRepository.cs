using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Implementations
{
    internal class UserRepository : CreateableBindableBase<IUserRepository, UserRepository, IPersistenceBase>, IUserRepository
    {
        private IPersistenceBase? _base;
        protected override void OnCreate(IPersistenceBase @base)
        {
            _base = @base;
        }

        protected override Task OnCreateAsync(IPersistenceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}