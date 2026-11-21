# API

The API is the ASP.NET Core host in `src/Relay.Api`. In Development it listens on `http://localhost:5034`. One `dotnet run` also serves the Blazor client: `/`, `/login`, `/register`, and `/boards/{id}` fall back to `index.html`. Protected routes expect `Authorization: Bearer <token>`. The client keeps that token in `localStorage` under `relay.token`.

```mermaid
flowchart LR
    browser[Browser] --> host[Relay.Api]
    host --> pages[index.html and Blazor files]
    host --> api[REST endpoints]
    pages --> stored[relay.token]
    stored --> api
```

Auth routes are mapped in `src/Relay.Api/Auth/AuthEndpoints.cs`. The register and login actions are implemented in that file. Board routes are mapped in `src/Relay.Api/Boards/BoardEndpoints.cs` and card routes in `src/Relay.Api/Cards/CardEndpoints.cs`. Each of those routes delegates to one handler per use case.

```mermaid
flowchart LR
    postBoard[POST boards] --> createHandler[CreateBoardHandler]
    getBoards[GET boards] --> listHandler[GetMyBoardsHandler]
    getBoard[GET board by id] --> readHandler[GetBoardHandler]
    postCard[POST card] --> createCard[CreateCardHandler]
    putCard[PUT card] --> updateCard[UpdateCardHandler]
    deleteCard[DELETE card] --> deleteHandler[DeleteCardHandler]
    moveCard[POST move] --> moveHandler[MoveCardHandler]
    createHandler --> db[AppDbContext]
    listHandler --> db
    readHandler --> db
    createCard --> db
    updateCard --> db
    deleteHandler --> db
    moveHandler --> db
```

`BoardEndpoints` reads the caller id, calls the handler, and turns the handler result into a status code. Handlers own validation and Entity Framework. They return values, not HTTP responses.

## Auth

| Method | Path | Auth | Success | Notes |
| --- | --- | --- | --- | --- |
| POST | `/api/auth/register` | No | 201 | Body: `email`, `password`, `displayName`. Returns the user. |
| POST | `/api/auth/login` | No | 200 | Body: `email`, `password`. Returns `token` and `user`. |
| GET | `/api/auth/me` | Yes | 200 | Returns the current user. Missing or invalid token is 401. |

Register returns 400 when a field is missing, the password does not meet Identity's rules, or the email is already registered. Identity requires at least 6 characters with an uppercase letter, a lowercase letter, a digit, and a symbol. Login returns 401 for an unknown email or a wrong password.

## Boards

Every board route requires a signed-in user.

| Method | Path | Success | Notes |
| --- | --- | --- | --- |
| POST | `/api/boards` | 201 | Body: `name`. Creates the board and seeds To Do, In Progress, and Done. |
| GET | `/api/boards` | 200 | Boards created by the current user, newest first. |
| GET | `/api/boards/{id}` | 200 | Board, columns, and cards ordered by `Order`. Any signed-in user. |

A missing or blank name is 400. A name longer than 80 characters is 400. An unknown board id is 404. A request with no token is 401.

## Cards

Every card route requires a signed-in user. Handlers live in `src/Relay.Api/Cards/Commands`. These routes do not broadcast over SignalR.

| Method | Path | Success | Notes |
| --- | --- | --- | --- |
| POST | `/api/boards/{id}/cards` | 201 | Body: `columnId`, `title`, optional `description`. Appends to that column. |
| PUT | `/api/cards/{id}` | 200 | Body: `title`, optional `description`. |
| DELETE | `/api/cards/{id}` | 204 | Deletes the card and rewrites the column order. |
| POST | `/api/cards/{id}/move` | 200 | Body: `columnId`, `order`. Returns the reindexed cards. |

Title is required, trimmed, and at most 120 characters. Description is optional, trimmed, and at most 2000 characters. A blank description is stored as null. A missing card, board, or column is 404. A column on a different board is 400.

A move removes the card from its current order, inserts it at `order` clamped into the target column, then rewrites both columns to `0..n-1`. Moving inside one column rewrites that column only. The response lists every card in the affected columns: `cardId`, `fromColumnId`, `toColumnId`, and `orders`.

```mermaid
flowchart TD
    load[Load card and target column] --> missing{Either missing}
    missing -->|yes| notFound[404]
    missing -->|no| sameBoard{Same board}
    sameBoard -->|no| rejected[400]
    sameBoard -->|yes| remove[Remove card from source order]
    remove --> insert[Insert at clamped index]
    insert --> rewrite[Rewrite affected columns from zero]
    rewrite --> save[Save]
```
