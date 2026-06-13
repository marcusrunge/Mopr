using MarcusRunge.Mopr.Workbench.Services.Interfaces;

namespace MarcusRunge.Mopr.Workbench.Services
{
    public class MessageService : IMessageService
    {
        public string GetMessage() => "Hello from the Message Service";
    }
}