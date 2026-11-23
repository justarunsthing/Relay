using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Relay.Api.Boards.Queries;

namespace Relay.Api.Hubs;

[Authorize]
public sealed class BoardHub(GetBoardHandler getBoard) : Hub<IBoardClient>
{
    private static readonly object MembershipKey = new();

    public static string GroupName(Guid boardId) => $"board-{boardId:D}";

    public async Task JoinBoard(Guid boardId)
    {
        var board = await getBoard.Handle(boardId, Context.ConnectionAborted) ?? throw new HubException("Board not found.");

        await Clients.Caller.BoardState(board);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(boardId), Context.ConnectionAborted);
        Remember(boardId);
    }

    public Task LeaveBoard(Guid boardId) => Leave(boardId);

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            foreach (var boardId in ForgetAll())
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(boardId));
            }
        }
        finally
        {
            await base.OnDisconnectedAsync(exception);
        }
    }

    private async Task Leave(Guid boardId)
    {
        Forget(boardId);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(boardId));
    }

    private void Remember(Guid boardId)
    {
        lock (Context.Items)
        {
            State().Boards.Add(boardId);
        }
    }

    private void Forget(Guid boardId)
    {
        lock (Context.Items)
        {
            State().Boards.Remove(boardId);
        }
    }

    private Guid[] ForgetAll()
    {
        lock (Context.Items)
        {
            var state = State();
            var boards = state.Boards.ToArray();
            state.Boards.Clear();

            return boards;
        }
    }

    private Membership State()
    {
        if (Context.Items[MembershipKey] is not Membership state)
        {
            state = new Membership();
            Context.Items[MembershipKey] = state;
        }

        return state;
    }

    private sealed class Membership
    {
        public HashSet<Guid> Boards { get; } = [];
    }
}
