using Core.Notifications;

namespace Application.Common.Interfaces
{
    public interface INotificacaoService
    {
        Task EnviarAsync(NotificacaoMensagem mensagem);
    }
}
