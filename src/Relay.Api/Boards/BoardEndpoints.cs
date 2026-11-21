using System.Security.Claims;
using Relay.Api.Auth;
using Relay.Api.Boards.Commands;
using Relay.Api.Boards.Queries;
using Relay.Shared.Boards;

namespace Relay.Api.Boards;

public static class BoardEndpoints
{
    public static IServiceCollection AddBoardHandlers(this IServiceCollection services)
    {
        services.AddScoped<CreateBoardHandler>();
        services.AddScoped<GetMyBoardsHandler>();
        services.AddScoped<GetBoardHandler>();
        return services;
    }

    public static IEndpointRouteBuilder MapBoardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/boards").RequireAuthorization();

        group.MapPost("/", Create);
        group.MapGet("/", List);
        group.MapGet("/{id:guid}", Get);

        return app;
    }

    private static async Task<IResult> Create(
        CreateBoardRequest request,
        CreateBoardHandler handler,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUser.Id(user);

        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var result = await handler.Handle(request, userId, cancellationToken);

        return result switch
        {
            CreateBoardResult.Created created => Results.Created($"/api/boards/{created.Board.Id}", created.Board),
            CreateBoardResult.Invalid invalid => Results.BadRequest(new BoardRequestError(invalid.Error)),
            _ => Results.BadRequest(new BoardRequestError("The board could not be created."))
        };
    }

    private static async Task<IResult> List(
        GetMyBoardsHandler handler,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUser.Id(user);

        if (userId is null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(await handler.Handle(userId, cancellationToken));
    }

    private static async Task<IResult> Get(
        Guid id,
        GetBoardHandler handler,
        CancellationToken cancellationToken)
    {
        var board = await handler.Handle(id, cancellationToken);

        return board is null ? Results.NotFound() : Results.Ok(board);
    }
}
