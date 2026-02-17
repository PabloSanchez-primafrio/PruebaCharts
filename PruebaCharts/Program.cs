using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PruebaCharts.Services;
using PruebaCharts.Data;
using PruebaCharts.Models;
using PruebaCharts.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Registra ActiveDirectoryService usando su Singleton existente
builder.Services.AddSingleton(sp => ActiveDirectoryService.Instance);

builder.Services.AddScoped<ConsultaService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
