using Microsoft.EntityFrameworkCore;

namespace Library.API.Entities
{
    public class LibraryContext : DbContext
    {
        public LibraryContext(DbContextOptions<LibraryContext> options) : base(options)        
        {           
        }

        public DbSet<Author> Authors { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<File> Files { get; set; }
    }   

}
