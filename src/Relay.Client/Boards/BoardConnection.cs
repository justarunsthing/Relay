using Microsoft.AspNetCore.SignalR.Client;
using Relay.Shared.Boards;
using Relay.Shared.Cards;
using Relay.Shared.Hubs;

namespace Relay.Client.Boards;

public interface IBoardListener
{
    Task OnBoardState(BoardResponse board);

    Task OnCardAdded(CardAdded added);

    Task OnCardUpdated(CardUpdated updated);

    Task OnCardDeleted(CardDeleted deleted);

    Task OnCardMoved(MoveCardResponse move);
}

public sealed class BoardConnection : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly Guid _boardId;

    public BoardConnection(Uri hubUrl, Func<Task<string?>> accessToken, Guid boardId, IBoardListener listener)
    {
        _boardId = boardId;
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => options.AccessTokenProvider = accessToken)
            .Build();

        _connection.On<BoardResponse>("BoardState", listener.OnBoardState);
        _connection.On<CardAdded>("CardAdded", listener.OnCardAdded);
        _connection.On<CardUpdated>("CardUpdated", listener.OnCardUpdated);
        _connection.On<CardDeleted>("CardDeleted", listener.OnCardDeleted);
        _connection.On<MoveCardResponse>("CardMoved", listener.OnCardMoved);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _connection.StartAsync(cancellationToken);
        await _connection.InvokeAsync("JoinBoard", _boardId, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_connection.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("LeaveBoard", _boardId);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or OperationCanceledException or HttpRequestException)
        {
        }

        await _connection.DisposeAsync();
    }
}
