# Data model

```mermaid
erDiagram
    Board ||--o{ Column : "cascade deletes"
    Column ||--o{ Card : "cascade deletes"

    Board {
        guid Id PK
        string Name
        string CreatedByUserId
        datetime CreatedAt
    }

    Column {
        guid Id PK
        guid BoardId FK
        string Title
        int Order
    }

    Card {
        guid Id PK
        guid ColumnId FK
        string Title
        string Description "optional"
        int Order
        string CreatedByUserId
        datetime UpdatedAt
    }
```

`Column.BoardId` and `Card.ColumnId` are indexed. `CreatedAt` and `UpdatedAt` are `DateTimeOffset` in the entities.

`ApplicationUser.Id` is the Identity string id. `Board.CreatedByUserId` and `Card.CreatedByUserId` store that same string. Neither is a foreign key, so deleting a user does not delete boards or cards.

```mermaid
erDiagram
    ApplicationUser ||--o{ Board : "user id stored, no foreign key"
    ApplicationUser ||--o{ Card : "user id stored, no foreign key"

    ApplicationUser {
        string Id PK
        string DisplayName
        string Email
    }
```
