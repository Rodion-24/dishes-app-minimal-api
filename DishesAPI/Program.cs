using System.Security.Claims;
using AutoMapper;
using DishesAPI.DbContexts;
using DishesAPI.Entities;
using DishesAPI.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// register the DbContext on the container, getting the
// connection string from appSettings   
builder.Services.AddDbContext<DishesDbContext>(o => o.UseSqlite(
    builder.Configuration["ConnectionStrings:DishesDBConnectionString"]));

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

var dishesEndpoints = app.MapGroup("/dishes");
var dishWithGuidIdEndpoints = dishesEndpoints.MapGroup("/{dishId:guid}");
var ingredientsEndpoints = dishWithGuidIdEndpoints.MapGroup("/ingredients");

dishesEndpoints.MapGet("", async Task<Ok<IEnumerable<DishDto>>> (DishesDbContext db, IMapper mapper, string? name) =>
{
    return TypedResults.Ok(mapper.Map<IEnumerable<DishDto>>(await db.Dishes
       .Where(d => name == null || d.Name.Contains(name))
       .ToListAsync()));
});

dishWithGuidIdEndpoints.MapGet("", async Task<Results<NotFound, Ok<DishDto>>> (DishesDbContext db, IMapper mapper, Guid dishId) =>
{
    var dishEntity = await db.Dishes
        .FirstOrDefaultAsync(d => d.Id == dishId);

    if (dishEntity == null)
    {
        return TypedResults.NotFound();
    }

    return TypedResults.Ok(mapper.Map<DishDto>(dishEntity));
}).WithName("GetDish");

dishesEndpoints.MapGet("/{dishName}", async Task<Results<NotFound, Ok<DishDto>>> (DishesDbContext db, IMapper mapper, string dishName) =>
{
    var dish = await db.Dishes.FirstOrDefaultAsync(d => d.Name == dishName);

    if (dish == null)
    {
        return TypedResults.NotFound();
    }

    return TypedResults.Ok(mapper.Map<DishDto>(await db.Dishes.FirstOrDefaultAsync(d => d.Name == dishName)));
});

ingredientsEndpoints.MapGet("", async Task<Results<NotFound, Ok<IEnumerable<IngredientDto>>>> (DishesDbContext db, IMapper mapper, Guid dishId) =>
{
    var dish = await db.Dishes.FirstOrDefaultAsync(d => d.Id == dishId);

    if (dish == null)
    {
        return TypedResults.NotFound();
    }

    return TypedResults.Ok(mapper.Map<IEnumerable<IngredientDto>>((await db.Dishes
        .Include(d => d.Ingredients)
        .FirstOrDefaultAsync(d => d.Id == dishId))?.Ingredients));
});

dishesEndpoints.MapPost("", async Task<CreatedAtRoute<DishDto>> (DishesDbContext dishesDbContext,
    IMapper mapper,
    DishForCreationDto dishForCreationDto
) =>
{
    var dishEntity = mapper.Map<Dish>(dishForCreationDto);
    dishesDbContext.Add(dishEntity);
    await dishesDbContext.SaveChangesAsync();

    var dishToReturn = mapper.Map<DishDto>(dishEntity);

    return TypedResults.CreatedAtRoute(dishToReturn, "GetDish", new { dishId = dishEntity.Id });
});

dishWithGuidIdEndpoints.MapPut("", async Task<Results<NotFound, NoContent>> (DishesDbContext dishesDbContext,
    IMapper mapper,
    Guid dishId,
    DishForUpdateDto dishForUpdateDto) =>
{
    var dishEntity = await dishesDbContext.Dishes
        .FirstOrDefaultAsync(d => d.Id == dishId);

    if (dishEntity == null)
    {
        return TypedResults.NotFound();
    }

    mapper.Map(dishForUpdateDto, dishEntity);

    await dishesDbContext.SaveChangesAsync();

    return TypedResults.NoContent();
});

dishWithGuidIdEndpoints.MapDelete("", async Task<Results<NotFound, NoContent>> (DishesDbContext dishesDbContext,
    Guid dishId) =>
{
    var dishEntity = await dishesDbContext.Dishes
        .FirstOrDefaultAsync(d => d.Id == dishId);
    if (dishEntity == null)
    {
        return TypedResults.NotFound();
    }

    dishesDbContext.Dishes.Remove(dishEntity);
    await dishesDbContext.SaveChangesAsync();
    return TypedResults.NoContent();
});

app.Run();