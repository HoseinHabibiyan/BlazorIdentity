using Microsoft.EntityFrameworkCore;

namespace Backend.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options);