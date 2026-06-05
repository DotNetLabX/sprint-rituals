using Microsoft.EntityFrameworkCore;

namespace Blocks.EntityFrameworkCore;

public abstract class ApplicationDbContext<TDbContext>(DbContextOptions<TDbContext> options)
    : DbContext(options) where TDbContext : DbContext;
