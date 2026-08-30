using MarcusRunge.Mopr.Workbench.Contracts.Miras;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Miras;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Test
{
    internal sealed class MirasFlowServiceTestContext : IDisposable
    {
        public MirasFlowServiceTestContext()
        {
            ApplicationLifetime = new TestApplicationLifetime();
            MirasService = new Mock<IMirasService>(MockBehavior.Strict);

            Core = new CoreFactory(null, ApplicationLifetime, MirasService.Object).Create();

            MirasApplicationService = Core.MirasApplicationService ?? throw new InvalidOperationException("The MIRAS application service was not initialized.");

            Flow = MirasApplicationService.MirasFlowService;
        }

        public TestApplicationLifetime ApplicationLifetime { get; }

        public ICore Core { get; }

        public IMirasFlowService Flow { get; }

        public IMirasApplicationService MirasApplicationService { get; }

        public Mock<IMirasService> MirasService { get; }

        public void Dispose() => ApplicationLifetime.Dispose();
    }
}