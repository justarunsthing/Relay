using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Relay.Api.Cards.Commands;
using Relay.Shared.Auth;
using Relay.Shared.Boards;
using Relay.Shared.Cards;

namespace Relay.Api.Tests;

public sealed class BoardApiTests(RelayApiFactory factory) : IClassFixture<RelayApiFactory>
{
    [Fact]
    public async Task Health_returns_200()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Anonymous_board_create_returns_401()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/boards", new CreateBoardRequest("Nope"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Move_into_another_column_rewrites_both_columns_to_contiguous_orders()
    {
        using var client = await SignedInClient();
        var board = await CreateBoard(client, "Move");
        var todo = board.Columns.Single(column => column.Title == "To Do");
        var progress = board.Columns.Single(column => column.Title == "In Progress");

        var alpha = await CreateCard(client, board.Id, todo.Id, "Alpha");
        var beta = await CreateCard(client, board.Id, todo.Id, "Beta");
        var gamma = await CreateCard(client, board.Id, progress.Id, "Gamma");

        var response = await client.PostAsJsonAsync(
            $"/api/cards/{alpha.Id}/move",
            new MoveCardRequest(progress.Id, 0));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var move = await response.Content.ReadFromJsonAsync<MoveCardResponse>();
        Assert.NotNull(move);
        Assert.Equal(alpha.Id, move.CardId);
        Assert.Equal(todo.Id, move.FromColumnId);
        Assert.Equal(progress.Id, move.ToColumnId);
        AssertContiguous(move.Orders, todo.Id);
        AssertContiguous(move.Orders, progress.Id);
        Assert.Equal(0, OrderOf(move.Orders, beta.Id));
        Assert.Equal(todo.Id, ColumnOf(move.Orders, beta.Id));
        Assert.Equal(0, OrderOf(move.Orders, alpha.Id));
        Assert.Equal(progress.Id, ColumnOf(move.Orders, alpha.Id));
        Assert.Equal(1, OrderOf(move.Orders, gamma.Id));
        Assert.Equal(progress.Id, ColumnOf(move.Orders, gamma.Id));

        var stored = await client.GetFromJsonAsync<BoardResponse>($"/api/boards/{board.Id}");
        Assert.NotNull(stored);
        var storedTodo = stored.Columns.Single(column => column.Id == todo.Id);
        var storedProgress = stored.Columns.Single(column => column.Id == progress.Id);
        Assert.Equal(["Beta"], storedTodo.Cards.OrderBy(card => card.Order).Select(card => card.Title));
        Assert.Equal(["Alpha", "Gamma"], storedProgress.Cards.OrderBy(card => card.Order).Select(card => card.Title));
        Assert.Equal(Enumerable.Range(0, storedTodo.Cards.Count), storedTodo.Cards.Select(card => card.Order).Order());
        Assert.Equal(Enumerable.Range(0, storedProgress.Cards.Count), storedProgress.Cards.Select(card => card.Order).Order());
    }

    [Fact]
    public async Task Move_of_a_missing_card_returns_404()
    {
        using var client = await SignedInClient();

        var response = await client.PostAsJsonAsync(
            $"/api/cards/{Guid.NewGuid()}/move",
            new MoveCardRequest(Guid.NewGuid(), 0));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<HttpClient> SignedInClient()
    {
        var client = factory.CreateClient();
        var email = $"tests-{Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Test123!", "Tester"));
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Test123!"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.Token);
        return client;
    }

    private static async Task<BoardResponse> CreateBoard(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/boards", new CreateBoardRequest(name));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var board = await response.Content.ReadFromJsonAsync<BoardResponse>();
        Assert.NotNull(board);
        return board;
    }

    private static async Task<CardResponse> CreateCard(HttpClient client, Guid boardId, Guid columnId, string title)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/boards/{boardId}/cards",
            new CreateCardCommand(columnId, title, null));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var card = await response.Content.ReadFromJsonAsync<CardResponse>();
        Assert.NotNull(card);
        return card;
    }

    private static void AssertContiguous(IReadOnlyList<CardOrderResponse> orders, Guid columnId)
    {
        var columnOrders = orders
            .Where(order => order.ColumnId == columnId)
            .Select(order => order.Order)
            .Order()
            .ToArray();

        Assert.NotEmpty(columnOrders);
        Assert.Equal(Enumerable.Range(0, columnOrders.Length), columnOrders);
    }

    private static int OrderOf(IReadOnlyList<CardOrderResponse> orders, Guid cardId) =>
        orders.Single(order => order.CardId == cardId).Order;

    private static Guid ColumnOf(IReadOnlyList<CardOrderResponse> orders, Guid cardId) =>
        orders.Single(order => order.CardId == cardId).ColumnId;
}
