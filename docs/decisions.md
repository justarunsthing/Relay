# Decisions

Decisions that are already in the code. The Blazor shell is hosted by the API. The board page joins the SignalR hub, applies card events, shows who is on the board, reconnects after a dropped connection, and moves a card locally before the move request returns.

## Concrete handlers, no repository interfaces

Each board and card use case is a class, such as `CreateBoardHandler` and `MoveCardHandler`. The endpoint injects that class. The handler uses `AppDbContext` directly.

A command repository and a query repository behind interfaces would pay off if the read store and the write store diverged, or if a unit test needed to stub storage. Relay has one PostgreSQL database and one implementation. The interface would wrap the same query that already sits in the handler. The planned API tests run the real handler through `WebApplicationFactory`, so they do not need a fake repository.

Add an interface when a second implementation appears. Until then the handler is the whole use case, and the Entity Framework query stays next to the rule it serves.

Auth does not use this split yet. Register and login are still methods on `AuthEndpoints`, because they were written before the board handlers. New board and card use cases follow the handler split.

## Guid primary keys

`Board`, `Column`, and `Card` use `Guid` keys. A board link is the only access check in v1: any signed-in user who knows the id can open the board. A sequential int in `/api/boards/1` would let someone walk every board. The extra size of a `Guid` does not matter at this scale.

`ApplicationUser.Id` stays the Identity string. `Board.CreatedByUserId` stores that string. It is not a foreign key, so deleting a user does not delete their boards.

## JWT, with the signing key outside git

Login returns an HMAC-SHA256 JWT that lasts 480 minutes. There is no refresh token and no Identity UI. `AddIdentityCore` stores users and hashes passwords.

`Jwt:Issuer`, `Jwt:Audience`, and `Jwt:ExpiryMinutes` live in `appsettings.json`. `Jwt:Key` and the Postgres connection string live in user secrets. A new machine must set both secrets before the API will start. Production is expected to supply the same values as environment variables.

## Development applies migrations on startup

`Program.cs` calls `Database.Migrate()` only when the environment is Development and the process is not an Entity Framework design-time tool. Starting the API creates the `Relay` database and applies pending migrations. Production does not migrate on startup.

The database name is `Relay` with that exact casing. Npgsql creates and opens the quoted name.

## Npgsql provider 10.0.4

Entity Framework Core is 10.0.15. `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.15 was never published. The latest 10.0 package is 10.0.4, and it targets `net10.0`. Version 11.0.0 targets `net11.0` only.

## No unique index on card or column order

`Order` is an integer. Create appends a card at the current count. Delete and move rewrite the affected column or columns to `0..n-1` in one save. A move clamps the requested index into the target column instead of rejecting a number past the end. A unique index on `(ColumnId, Order)` would fail while two rows briefly share an order during that rewrite. The foreign keys `Column.BoardId` and `Card.ColumnId` are indexed because those are the lookups used to load a board.

The move response returns the absolute orders. `CardMoved` broadcasts that same response.

## Shared contracts

`RegisterRequest`, `LoginResponse`, `UserResponse`, `CreateBoardRequest`, `MoveCardRequest`, the board response records, and the hub payloads `CardAdded`, `CardUpdated`, and `CardDeleted` live in `Relay.Shared`. `CardMoved` reuses `MoveCardResponse`. The Blazor client serializes those same shapes. `docs/data-model.md` is the entity diagram for the board tables.

The client stores the JWT in `localStorage` under `relay.token` and attaches it as a bearer token. A signed-out visit to `/` or `/boards/{id}` goes to `/login` with a return URL.

## The hub broadcasts after a successful card write

`BoardHub` is mapped at `/hubs/board` and requires a signed-in user. REST stays the write path. The hub reads the board for `JoinBoard` and does not insert, update, or delete rows.

The WebSocket handshake cannot set an `Authorization` header. The JWT middleware accepts `access_token` in the query string only when the path is under `/hubs/board`. Other routes still require the bearer header, so a query token cannot authorize a card write.

`JoinBoard` loads the board. A missing board is rejected and the connection is not added to a group. Otherwise the caller receives `BoardState` — the board, its columns, its cards in order, and the people already viewing it — and then the connection joins `board-{boardId}`. Card events arrive after that snapshot. `LeaveBoard` removes that connection from the group. The hub remembers which boards the connection joined, and `OnDisconnectedAsync` leaves those groups when the client never called `LeaveBoard`.

Create, update, delete, and move still persist in their handlers. After `SaveChanges` succeeds, the handler broadcasts `CardAdded`, `CardUpdated`, `CardDeleted`, or `CardMoved` to that board's group. `CardMoved` is the move response, including the rewritten orders. `CardDeleted` includes the rewritten orders for the column that lost the card. A validation failure or a missing row returns before the broadcast.

The board page opens the hub for the board it is showing. `AccessTokenProvider` reads the same `relay.token` value the REST client sends as a bearer token. The page replaces its snapshot with `BoardState`. Later card events patch the snapshot. A drop updates the snapshot immediately, then calls the move API. The response and the page's own `CardMoved` echo both write the server orders over that snapshot. A failed move puts the previous snapshot back and shows the error. Another open board page applies the event without a local drop. Leaving the page calls `LeaveBoard`.

The hub connection uses `WithAutomaticReconnect`. A dropped connection keeps the columns on screen and shows a reconnecting banner. On `Reconnected` the page calls `JoinBoard` again. The `BoardState` from that call replaces the columns, the cards, and who is viewing, including a local move that never reached the server. If the retries end and the connection stays closed, the banner says live updates stopped.

## One person per user on a board

`PresenceTracker` is a singleton in the API process. It records each hub connection's user id, display name, and board. The display name comes from the `display_name` claim on the JWT. Two tabs for the same user on the same board count as one person, so opening a second tab does not send `UserJoined` again. `UserLeft` is sent only when that user's last connection leaves, including when the connection drops without calling `LeaveBoard`. The board page shows those display names. There is no backplane, so this count is the count on this one server.
