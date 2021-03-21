using Library.API.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Services
{
    public class LibraryRepository : ILibraryRepository
    {
        private LibraryContext _context;

        public LibraryRepository(LibraryContext context)
        {
            _context = context;
        }

        public async Task<bool> SaveAsync()
        {
            return (await _context.SaveChangesAsync() >= 0);
        }

        public async Task<IEnumerable<Author>> GetAuthorsAsync()
        {
            return await _context.Authors.OrderBy(a => a.FirstName).ThenBy(a => a.LastName).ToListAsync();
        }

        public async Task<Author> GetAuthorAsync(Guid id)
        {            
            return await _context.Authors.FirstOrDefaultAsync(a => a.Id == id);            
        }

        public async Task<bool> AuthorExistsAsync(Guid id)
        {
            return await _context.Authors.AnyAsync(a => a.Id == id);
        }

        public async void AddAuthorAsync(Author author)
        {
            author.Id = Guid.NewGuid();
            await _context.Authors.AddAsync(author);

            // the repository fills the id (instead of using identity columns)
            if (author.Books.Any())
            {
                foreach (var book in author.Books)
                {
                    book.Id = Guid.NewGuid();
                }
            }
        }
       
        public void UpdateAuthor(Author author)
        {
            // no code in this implementation
        }

        public void DeleteAuthor(Author author)
        {
            _context.Authors.Remove(author);
        }

        public async Task<IEnumerable<Book>> GetBooksForAuthorAsync(Guid id)
        {
            return await _context.Books
                        .Where(b => b.AuthorId == id).OrderBy(b => b.Title).ToListAsync();
        }

        public async Task<Book> GetBookForAuthorAsync(Guid authorId, Guid bookId)
        {
            return await _context.Books
              .Where(b => b.AuthorId == authorId && b.Id == bookId).FirstOrDefaultAsync();
        }

        public async void AddBookForAuthor(Guid authorId, Book book)
        {
            var author = await GetAuthorAsync(authorId);
            if (author != null)
            {
                // if there isn't an id filled out (ie: we're not upserting), we should generate one
                if (book.Id == null)
                {
                    book.Id = Guid.NewGuid();
                }
                author.Books.Add(book);
            }
        }

        public void UpdateBookForAuthor(Book book)
        {
            // no code in this implementation
        }

        public void DeleteBook(Book book)
        {
            _context.Books.Remove(book);
        }

        public async Task<File> GetFileAsync(int id)
        {
            return await _context.Files.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<File>> GetAllFilesAsync()
        {
            return await _context.Files.ToListAsync();
        }

        public async void AddFileAsync(File file)
        {
            await _context.Files.AddAsync(file);
        }
    }
}
