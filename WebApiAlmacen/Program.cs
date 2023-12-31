using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;
using WebApiAlmacen.Filters;
using WebApiAlmacen.Middlewares;
using WebApiAlmacen.Models;
using WebApiAlmacen.Services;

var builder = WebApplication.CreateBuilder(args);

#region SERVICES



// Para evitar, dentro de los Controllers, cuando hacemos consultas de varias tablas (conocidas como join en sql), una referencia infinita entre relaciones
builder.Services.AddControllers(options =>
{
    // Integramos el filtro de excepci�n para todos los controladores
    options.Filters.Add<FiltroDeExcepcion>();
}).AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
// Add services to the container.

// Para evitar, dentro de los Controllers, cuando hacemos consultas de varias tablas (conocidas como join en sql), una referencia infinita entre relaciones
//builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);


// Capturamos del app.settings la cadena de conexi�n a la base de datos
// Configuration.GetConnectionString va directamente a la propiedad ConnectionStrings y de ah� tomamos el valor de DefaultConnection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// Nuestros servicios resolver�n dependencias de otras clases
// Registramos en el sistema de inyecci�n de dependencias de la aplicaci�n el ApplicationDbContext
// Conseguimos una instancia o configuraci�n global de la base de datos para todo el proyecto
builder.Services.AddDbContext<MiAlmacenContext>(options =>
{
    options.UseSqlServer(connectionString);
    // Esta opci�n deshabilita el tracking a nivel de proyecto (NoTracking).
    // Por defecto siempre hace el tracking. Con esta configuraci�n, no.
    // En cada operaci�n de modificaci�n de datos en los controladores, deberemos habilitar el tracking en cada operaci�n
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
}
);

// Gesti�n de archivos
// Para poder utilizar AddHttpContextAccessor en los controllers o en otros servicios (en nuestro caso, el servicio GestorArchivosLocal)
// Debemos incluir el servicio en el Program de esta manera
builder.Services.AddHttpContextAccessor();
// Nuestro servicio de gesti�n de Archivos GestorArchivosLocal es un servicio que debemos incluir en el Program para que lo use
// cualquier controlador
builder.Services.AddTransient<IGestorArchivos, GestorArchivosLocal>();
builder.Services.AddTransient<OperacionesService>();
builder.Services.AddTransient<HashService>();

// Configuramos la seguridad en el proyecto. Manifestamos que se va a implementar la seguridad
// mediante JWT firmados por la firma que est� en el app.settings.development.json con el nombre ClaveJWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
               .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
               {
                   ValidateIssuer = false,
                   ValidateAudience = false,
                   ValidateLifetime = true,
                   ValidateIssuerSigningKey = true,
                   IssuerSigningKey = new SymmetricSecurityKey(
                     Encoding.UTF8.GetBytes(builder.Configuration["ClaveJWT"]))
               });

builder.Services.AddHostedService<TareaProgramadaService>();
builder.Services.AddDataProtection();




// CORS Policy
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("https://www.apirequest.io").AllowAnyMethod().AllowAnyHeader();
        // builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[]{}
                    }
                });
});

// Serilog
//Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
//builder.Host.UseSerilog();

#endregion

var app = builder.Build();

#region MIDDLEWARES

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseMiddleware<LogFileIPMiddleware>();
app.UseMiddleware<LogFileBodyHttpResponseMiddleware>();

app.UseFileServer(); //PARA QUE TE LLEVE AL INDEX.HTML DEL WWWROOT

//app.UseStaticFiles();

app.UseAuthorization();

#endregion

app.MapControllers();

app.Run();
