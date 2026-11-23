using Relay.Shared.Hubs;

namespace Relay.Api.Presence;

public sealed record PresenceJoin(bool FirstForUser, IReadOnlyList<BoardUser> Users);

public sealed record PresenceLeave(string UserId, bool LastForUser);

public sealed class PresenceTracker
{
    private readonly object _gate = new();
    private readonly Dictionary<ConnectionBoard, Person> _connections = [];

    public PresenceJoin Join(string connectionId, Guid boardId, string userId, string displayName)
    {
        lock (_gate)
        {
            var key = new ConnectionBoard(connectionId, boardId);
            var alreadyHere = _connections.ContainsKey(key);
            var firstForUser = !alreadyHere && !OnBoard(boardId, userId);

            _connections[key] = new Person(userId, displayName);

            return new PresenceJoin(firstForUser, UsersOn(boardId));
        }
    }

    public PresenceLeave? Leave(string connectionId, Guid boardId)
    {
        lock (_gate)
        {
            var key = new ConnectionBoard(connectionId, boardId);

            if (!_connections.Remove(key, out var person))
            {
                return null;
            }

            return new PresenceLeave(person.UserId, !OnBoard(boardId, person.UserId));
        }
    }

    private bool OnBoard(Guid boardId, string userId) =>
        _connections.Any(entry => entry.Key.BoardId == boardId && entry.Value.UserId == userId);

    private List<BoardUser> UsersOn(Guid boardId) =>
        _connections
            .Where(entry => entry.Key.BoardId == boardId)
            .Select(entry => new BoardUser(entry.Value.UserId, entry.Value.DisplayName))
            .DistinctBy(user => user.UserId)
            .OrderBy(user => user.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(user => user.UserId, StringComparer.Ordinal)
            .ToList();

    private readonly record struct ConnectionBoard(string ConnectionId, Guid BoardId);

    private sealed record Person(string UserId, string DisplayName);
}
