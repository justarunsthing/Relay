using System.Security.Claims;
using Relay.Api.Auth;
using Relay.Api.Cards.Commands;
using Relay.Shared.Cards;

namespace Relay.Api.Cards;

public sealed record CardRequestError(string Error);

public static class CardEndpoints
{
    public static IServiceCollection AddCardHandlers(this IServiceCollection services)
    {
        services.AddScoped<CreateCardHandler>();
        services.AddScoped<UpdateCardHandler>();
        services.AddScoped<DeleteCardHandler>();
        services.AddScoped<MoveCardHandler>();
        return services;
    }

    public static IEndpointRouteBuilder MapCardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/boards").RequireAuthorization().MapPost("/{id:guid}/cards", Create);

        var cards = app.MapGroup("/api/cards").RequireAuthorization();
        cards.MapPut("/{id:guid}", Update);
        cards.MapDelete("/{id:guid}", Delete);
        cards.MapPost("/{id:guid}/move", Move);

        return app;
    }

    private static async Task<IResult> Create(
        Guid id,
        CreateCardCommand command,
        CreateCardHandler handler,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUser.Id(user);

        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var result = await handler.Handle(id, command, userId, cancellationToken);

        return result switch
        {
            CreateCardResult.Created created => Results.Created($"/api/cards/{created.Card.Id}", created.Card),
            CreateCardResult.Invalid invalid => Results.BadRequest(new CardRequestError(invalid.Error)),
            _ => Results.NotFound()
        };
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateCardCommand command,
        UpdateCardHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(id, command, cancellationToken);

        return result switch
        {
            UpdateCardResult.Updated updated => Results.Ok(updated.Card),
            UpdateCardResult.Invalid invalid => Results.BadRequest(new CardRequestError(invalid.Error)),
            _ => Results.NotFound()
        };
    }

    private static async Task<IResult> Delete(
        Guid id,
        DeleteCardHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(id, cancellationToken);

        return result is DeleteCardResult.Deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> Move(
        Guid id,
        MoveCardRequest request,
        MoveCardHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(id, request, cancellationToken);

        return result switch
        {
            MoveCardResult.Moved moved => Results.Ok(moved.Move),
            MoveCardResult.Invalid invalid => Results.BadRequest(new CardRequestError(invalid.Error)),
            _ => Results.NotFound()
        };
    }
}
