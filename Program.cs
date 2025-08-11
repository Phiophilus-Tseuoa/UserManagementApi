using UserManagementAPI.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// --- Service registrations ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- Development tooling ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --- Custom middleware in correct order ---

// 1) Error-handling middleware first
app.UseMiddleware<ErrorHandlingMiddleware>();

// 2) Authentication middleware next
app.UseMiddleware<TokenAuthenticationMiddleware>();

// 3) Logging middleware last (among custom)
app.UseMiddleware<RequestResponseLoggingMiddleware>();

// --- Remaining standard middleware ---
app.UseHttpsRedirection();

// If you later add ASP.NET Core Identity or policy-based auth:
// app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
