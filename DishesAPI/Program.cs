using System.Security.Claims;
using AutoMapper;
using DishesAPI.DbContexts;
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

app.MapGet("/dishes", async Task<Ok<IEnumerable<DishDto>>> (DishesDbContext db, IMapper mapper, string? name) =>
{
    return TypedResults.Ok(mapper.Map<IEnumerable<DishDto>>(await db.Dishes
       .Where(d => name == null || d.Name.Contains(name))
       .ToListAsync()));
});

app.MapGet("/dishes/{dishId:guid}", async Task<Results<NotFound, Ok<DishDto>>> (DishesDbContext db, IMapper mapper, Guid dishId) =>
{
    var dishEntity = await db.Dishes
        .FirstOrDefaultAsync(d => d.Id == dishId);

    if (dishEntity == null)
    {
        return TypedResults.NotFound();
    }

    return TypedResults.Ok(mapper.Map<DishDto>(dishEntity));
});

app.MapGet("/dishes/{dishName}", async Task<Results<NotFound, Ok<DishDto>>> (DishesDbContext db, IMapper mapper, string dishName) =>
{
    var dish = await db.Dishes.FirstOrDefaultAsync(d => d.Name == dishName);

    if (dish == null)
    {
        return TypedResults.NotFound();
    }

    return TypedResults.Ok(mapper.Map<DishDto>(await db.Dishes.FirstOrDefaultAsync(d => d.Name == dishName)));
});

app.MapGet("/dishes/{dishId}/ingredients", async Task<Results<NotFound, Ok<IEnumerable<IngredientDto>>>> (DishesDbContext db, IMapper mapper, Guid dishId) =>
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

app.Run();