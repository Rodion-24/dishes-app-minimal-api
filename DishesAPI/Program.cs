using DishesAPI.DbContexts;
using DishesAPI.Entities;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// register the DbContext on the container, getting the
// connection string from appSettings   
builder.Services.AddDbContext<DishesDbContext>(o => o.UseSqlite(
    builder.Configuration["ConnectionStrings:DishesDBConnectionString"]));

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.MapGet("/dishes", async (DishesDbContext db) =>
{
    return await db.Dishes.ToListAsync();
});

app.MapGet("/dishes/{dishId:guid}", async (DishesDbContext db, Guid dishId) =>
{
    return await db.Dishes.FirstOrDefaultAsync(d => d.Id == dishId);
});

app.MapGet("/dishes/{dishName}", async (DishesDbContext db, string dishName) =>
{
    return await db.Dishes.FirstOrDefaultAsync(d => d.Name == dishName);
});

app.MapGet("/dishes/{dishId}/ingredients", async (DishesDbContext db, Guid dishId) =>
{
    return (await db.Dishes
        .Include(d => d.Ingredients)
        .FirstOrDefaultAsync(d => d.Id == dishId))?.Ingredients;
});

app.Run();