using Library.API.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Library.API.Services
{
    public interface ILibraryRepository
    {
        Task<bool> SaveAsync();
        Task<IEnumerable<Author>> GetAuthorsAsync();
        Task<Author> GetAuthorAsync(Guid authorId);        
        Task<bool> AuthorExistsAsync(Guid authorId);
        void AddAuthorAsync(Author author);
        void DeleteAuthor(Author author);
        void UpdateAuthor(Author author);
        Task<IEnumerable<Book>> GetBooksForAuthorAsync(Guid authorId);
        Task<Book> GetBookForAuthorAsync(Guid authorId, Guid bookId);
        void AddBookForAuthor(Guid authorId, Book book);
        void UpdateBookForAuthor(Book book);
        void DeleteBook(Book book);

        
    }
}

