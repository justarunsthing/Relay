using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Relay.Shared.Boards;
using Relay.Shared.Cards;
using Relay.Shared.Hubs;

namespace Relay.Client.Boards;

public interface IBoardListener
{
    Task OnBoardState(BoardResponse board, IReadOnlyList<BoardUser> users);

    Task OnCardAdded(CardAdded added);

    Task OnCardUpdated(CardUpdated updated);

    Task OnCardDeleted(CardDeleted deleted);

    Task OnCardMoved(MoveCardResponse move);

    Task OnUserJoined(UserJoined joined);

    Task OnUserLeft(UserLeft left);

    Task OnReconnecting();

    Task OnReconnected();

    Task OnConnectionClosed();
}

public sealed class BoardConnection : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly Guid _boardId;
    private readonly IBoardListener _listener;
    private int _disposed;

    public BoardConnection(Uri hubUrl, Func<Task<string?>> accessToken, Guid boardId, IBoardListener listener)
    {
        _boardId = boardId;
        _listener = listener;
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => options.AccessTokenProvider = accessToken)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<BoardResponse, IReadOnlyList<BoardUser>>("BoardState", listener.OnBoardState);
        _connection.On<CardAdded>("CardAdded", listener.OnCardAdded);
        _connection.On<CardUpdated>("CardUpdated", listener.OnCardUpdated);
        _connection.On<CardDeleted>("CardDeleted", listener.OnCardDeleted);
        _connection.On<MoveCardResponse>("CardMoved", listener.OnCardMoved);
        _connection.On<UserJoined>("UserJoined", listener.OnUserJoined);
        _connection.On<UserLeft>("UserLeft", listener.OnUserLeft);
        _connection.Reconnecting += OnReconnecting;
        _connection.Reconnected += OnReconnected;
        _connection.Closed += OnClosed;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _connection.StartAsync(cancellationToken);
        await _connection.InvokeAsync("JoinBoard", _boardId, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

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

    private Task OnReconnecting(Exception? _) =>
        Disposed ? Task.CompletedTask : _listener.OnReconnecting();

    private async Task OnReconnected(string? _)
    {
        if (Disposed)
        {
            return;
        }

        try
        {
            await _connection.InvokeAsync("JoinBoard", _boardId);

            if (!Disposed)
            {
                await _listener.OnReconnected();
            }
        }
        catch (Exception ex) when (ex is HubException or HttpRequestException or InvalidOperationException or ObjectDisposedException)
        {
            if (!Disposed)
            {
                await _listener.OnConnectionClosed();
            }
        }
    }

    private Task OnClosed(Exception? _) =>
        Disposed ? Task.CompletedTask : _listener.OnConnectionClosed();

    private bool Disposed => Volatile.Read(ref _disposed) == 1;
}
