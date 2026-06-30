using Microsoft.EntityFrameworkCore;
using MenuGoBE.Data;
using FluentValidation;
using FluentValidation.AspNetCore;
using MenuGoBE.Interface.Services;
using MenuGoBE.Service;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Repositories;
using MenuGoBE.Mapper;
using AutoMapper;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddAutoMapper(typeof(AutoMapperProfiles));

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

//Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Repository
builder.Services.AddScoped<IChainRepository, ChainRepository>();
builder.Services.AddScoped<ITableRepository, TableRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();

// Service
builder.Services.AddScoped<IChainService, ChainService>();
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddScoped<IAccountService, AccountService>();


builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();


