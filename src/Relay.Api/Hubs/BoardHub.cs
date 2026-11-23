using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Relay.Api.Auth;
using Relay.Api.Boards.Queries;
using Relay.Api.Presence;
using Relay.Shared.Hubs;

namespace Relay.Api.Hubs;

[Authorize]
public sealed class BoardHub(GetBoardHandler getBoard, PresenceTracker presence) : Hub<IBoardClient>
{
    private static readonly object MembershipKey = new();

    public static string GroupName(Guid boardId) => $"board-{boardId:D}";

    public async Task JoinBoard(Guid boardId)
    {
        var board = await getBoard.Handle(boardId, Context.ConnectionAborted) ?? throw new HubException("Board not found.");
        var principal = Context.User ?? throw new HubException("You are not signed in.");
        var userId = CurrentUser.Id(principal) ?? throw new HubException("You are not signed in.");
        var name = CurrentUser.DisplayName(principal);
        var displayName = string.IsNullOrWhiteSpace(name) ? userId : name.Trim();
        var joined = presence.Join(Context.ConnectionId, boardId, userId, displayName);
        var group = GroupName(boardId);

        Remember(boardId);
        await Clients.Caller.BoardState(board, joined.Users);
        await Groups.AddToGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted);

        if (joined.FirstForUser)
        {
            await Clients.GroupExcept(group, Context.ConnectionId)
                .UserJoined(new UserJoined(boardId, new BoardUser(userId, displayName)));
        }
    }

    public Task LeaveBoard(Guid boardId) => Leave(boardId);

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            foreach (var boardId in ForgetAll())
            {
                await Drop(boardId);
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
        await Drop(boardId);
    }

    private async Task Drop(Guid boardId)
    {
        var left = presence.Leave(Context.ConnectionId, boardId);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(boardId));

        if (left is { LastForUser: true })
        {
            await Clients.Group(GroupName(boardId)).UserLeft(new UserLeft(boardId, left.UserId));
        }
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
