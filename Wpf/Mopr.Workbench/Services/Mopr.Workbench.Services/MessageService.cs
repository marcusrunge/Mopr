using Mopr.Workbench.Services.Interfaces;

namespace Mopr.Workbench.Services
{
    public class MessageService : IMessageService
    {
        public string GetMessage() => "Hello from the Message Service";
    }
}