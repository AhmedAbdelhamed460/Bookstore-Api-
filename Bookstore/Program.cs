using Bookstore.Models;
using Bookstore.Reposiotries;
using Bookstore.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<BookStoreDbContext>(option => option.UseSqlServer(builder.Configuration.GetConnectionString("default")));
builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddIdentity<AppUser, IdentityRole>() //Identity
    .AddEntityFrameworkStores<BookStoreDbContext>()
    .AddDefaultTokenProviders(); ;

//Reset Password Token Life Span Time
builder.Services.Configure<DataProtectionTokenProviderOptions>(opt =>
   opt.TokenLifespan = TimeSpan.FromHours(2));

builder.Services.Configure<IdentityOptions>(opts =>
{
    opts.Password.RequireNonAlphanumeric = false;
    opts.Password.RequireDigit = false;
    opts.Password.RequireLowercase = false;
    opts.Password.RequireUppercase = false;
    opts.Password.RequiredLength = 3;
});

//JWT Validate
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateLifetime = true,
        ValidateAudience = false,
        ValidateIssuer = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("my_secret_key_123456"))
    };
});

builder.Services.AddScoped<IBookRepo, BookRepo>(); //Ingect IBookRepo

builder.Services.AddScoped<IOrderDetailRepo, OrderDetailRepo>(); //Ingect IOrderDetailRepo

builder.Services.AddScoped<ICategoryRepository, CategoryRepository>(); //Ingect IOrderDetailRepo
builder.Services.AddScoped<IPublisherRepository, PublisherRepository>(); //Ingect IOrderDetailRepo
builder.Services.AddScoped<IShopingCartrRepo, ShopingCartrRepo>(); //Ingect IOrderDetailRepo



builder.Services.AddScoped<IReviewRepo, ReviewRepo>(); //Ingect IReviewRepo
builder.Services.AddScoped<IbooksRepo, booksRepo>(); //Ingect IReviewRepos
builder.Services.AddScoped<IAuthorRepository, AuthorRepository>(); //Ingect IReviewRepo
builder.Services.AddScoped<IOrderRep, OrderRep>(); //Ingect IReviewRepo

builder.Services.AddScoped<IEmailSender,EmailSender>();





builder.Services.AddCors();
builder.Services.AddSwaggerGen();

var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(c => c.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
