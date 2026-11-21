# Decisions

Decisions that are already in the code. SignalR, drag-and-drop, and the Blazor UI are not built yet.

## Concrete handlers, no repository interfaces

Each board use case is a class: `CreateBoardHandler`, `GetMyBoardsHandler`, and `GetBoardHandler`. The endpoint injects that class. The handler uses `AppDbContext` directly.

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

`Order` is an integer rewritten to `0..n-1` when a card moves. A unique index on `(ColumnId, Order)` would fail while two rows briefly share an order during that rewrite. The foreign keys `Column.BoardId` and `Card.ColumnId` are indexed because those are the lookups used to load a board.

## Shared contracts wait for the client

`RegisterRequest`, `LoginResponse`, and the board response records live in the API project. The Blazor client does not call the API yet. Moving them to `Relay.Shared` becomes worth it when the client has to serialize the same shapes. `docs/data-model.md` is the entity diagram for the board tables.
