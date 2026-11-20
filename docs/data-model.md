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
