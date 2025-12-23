using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Wrappers;
using Core.Notifications;
using MediatR;

namespace Application.Handlers.Notificacoes.Commands
{
    public class EnviarNotificacaoCommand : IRequestWrapper
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public NotificacaoPrioridade Priority { get; set; } = NotificacaoPrioridade.Default;
        public List<string> Tags { get; set; } = new();
        public List<NotificacaoAcao> Actions { get; set; } = new();
    }

    public class EnviarNotificacaoHandler : IRequestHandler<EnviarNotificacaoCommand, Response>
    {
        private readonly INotificacaoService _notificacaoService;

        public EnviarNotificacaoHandler(INotificacaoService notificationService)
        {
            _notificacaoService = notificationService;
        }

        public async Task<Response> Handle(EnviarNotificacaoCommand request, CancellationToken cancellationToken)
        {
            var notification = new NotificacaoMensagem
            {
                Title = request.Title,
                Message = request.Message,
                Priority = (int)request.Priority,
                Tags = request.Tags,
                Actions = request.Actions,
                Markdown = true
            };

            await _notificacaoService.EnviarAsync(notification);

            return Response.Success();
        }
    }

}
