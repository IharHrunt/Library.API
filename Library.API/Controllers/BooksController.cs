using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Library.API.Controllers
{
    //[Produces("application/json")]    
    [Route("api/authors/{authorId}/books")]
    public class BooksController : Controller
    {
        private ILibraryRepository _libraryRepository;
        private ILogger<BooksController> _logger;

        public BooksController(ILibraryRepository libraryRepository,
            ILogger<BooksController> logger)
        {
            _logger = logger;
            _libraryRepository = libraryRepository;
        }

        [HttpGet()]
        public async Task<IActionResult> GetBooksAuthor(Guid authorId)
        {

            if (!(await _libraryRepository.AuthorExistsAsync(authorId)))
            {
                return NotFound();
            }
            var booksFromRepository = await _libraryRepository.GetBooksForAuthorAsync(authorId);

            var booksForAuthor = AutoMapper.Mapper.Map<IEnumerable<BookDto>>(booksFromRepository);
            return Ok(booksForAuthor);
        }

        [HttpGet("{Id}", Name = "GetBookForAuthor")]
        public async Task<IActionResult> GetBookForAuthor(Guid authorId, Guid Id)
        {
            if (!(await _libraryRepository.AuthorExistsAsync(authorId)))
            {
                return NotFound();
            }

            var booksForAuthorFromRepository = await _libraryRepository.GetBookForAuthorAsync(authorId, Id);
            if (booksForAuthorFromRepository == null)
            {
                return NotFound();
            }

            var bookForAuthor = AutoMapper.Mapper.Map<BookDto>(booksForAuthorFromRepository);
            return Ok(bookForAuthor);
        }

        [HttpPost()]
        public async Task<IActionResult> CreateBookForAuthor(Guid authorId,
          [FromBody] BookForCreationDto book)
        {
            if (book == null)
            {
                return BadRequest();
            }

            if (book.Description == book.Title)
            {
                ModelState.AddModelError(nameof(BookForCreationDto),
                    "The provided description should be different from the title.");
            }

            if (!ModelState.IsValid)
            {
                return new UnprocessableEntityObjectResult(ModelState);
            }

            if (!(await _libraryRepository.AuthorExistsAsync(authorId)))
            {
                return NotFound();
            }

            var bookEntity = AutoMapper.Mapper.Map<Book>(book);
            _libraryRepository.AddBookForAuthor(authorId, bookEntity);

            if (!(await _libraryRepository.SaveAsync()))
            {
                throw new Exception($"Creating a book for author {authorId} failed on save.");
            }

            var bookToReturn = AutoMapper.Mapper.Map<BookDto>(bookEntity);

            return CreatedAtRoute("GetBookForAuthor", new { authorId = authorId, Id = bookToReturn.Id }, bookToReturn);
        }

        [HttpDelete("{Id}")]
        public async Task<IActionResult> DeleteBookForAuthor(Guid authorId, Guid Id)
        {
            if (!(await _libraryRepository.AuthorExistsAsync(authorId)))
            {
                return NotFound();
            }

            var bookForAuthorFromRepository = await _libraryRepository.GetBookForAuthorAsync(authorId, Id);
            if (bookForAuthorFromRepository == null)
            {
                return NotFound();
            }

            _libraryRepository.DeleteBook(bookForAuthorFromRepository);

            if (!(await _libraryRepository.SaveAsync()))
            {
                throw new Exception($"Deleting a book for author {authorId} failed on save.");
            }

            _logger.LogInformation(100, $"!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!Book {Id} for author {authorId} was deleted.!!!!!!!!!!!!!!!!!!!!");

            return NoContent();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBookForAuthor(Guid authorId, Guid id,
            [FromBody] BookForUpdateDto book)
        {
            if (book == null)
            {
                return BadRequest();
            }

            if (book.Description == book.Title)
            {
                ModelState.AddModelError(nameof(BookForUpdateDto),
                    "The provided description should be different from the title.");
            }

            if (!ModelState.IsValid)
            {
                return new UnprocessableEntityObjectResult(ModelState);
            }

            if (!(await _libraryRepository.AuthorExistsAsync(authorId)))
            {
                return NotFound();
            }

            var bookForAuthorFromRepository = await _libraryRepository.GetBookForAuthorAsync(authorId, id);
            if (bookForAuthorFromRepository == null)
            {
                //return NotFound();
                var bookToAdd = AutoMapper.Mapper.Map<Book>(book);
                bookToAdd.Id = id;
                _libraryRepository.AddBookForAuthor(authorId, bookToAdd);

                if (!(await _libraryRepository.SaveAsync()))
                {
                    throw new Exception($"Upserting a book {id} for author {authorId} failed on save.");
                }
                var bookToReturn = AutoMapper.Mapper.Map<BookDto>(bookToAdd);

                return CreatedAtRoute("GetBookForAuthor", new { authorId = authorId, Id = bookToReturn.Id }, bookToReturn);
            }

            AutoMapper.Mapper.Map(book, bookForAuthorFromRepository);

            _libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepository);

            if (!(await _libraryRepository.SaveAsync()))
            {
                throw new Exception($"Updating a book {id} for author {authorId} failed on save.");
            }
            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> PartiallyUpdateBookForAuthor(Guid authorId, Guid id,
           [FromBody] JsonPatchDocument<BookForUpdateDto> patchDoc)
        {
            if (patchDoc == null)
            {
                return BadRequest();
            }

            if (!(await _libraryRepository.AuthorExistsAsync(authorId)))
            {
                return NotFound();
            }

            var bookForAuthorFromRepo = await _libraryRepository.GetBookForAuthorAsync(authorId, id);

            if (bookForAuthorFromRepo == null)
            {
                var bookDto = new BookForUpdateDto();
                patchDoc.ApplyTo(bookDto, ModelState);

                if (bookDto.Description == bookDto.Title)
                {
                    ModelState.AddModelError(nameof(BookForUpdateDto),
                        "The provided description should be different from the title.");
                }

                TryValidateModel(bookDto);

                if (!ModelState.IsValid)
                {
                    return new UnprocessableEntityObjectResult(ModelState);
                }

                var bookToAdd = AutoMapper.Mapper.Map<Book>(bookDto);
                bookToAdd.Id = id;

                _libraryRepository.AddBookForAuthor(authorId, bookToAdd);

                if (!(await _libraryRepository.SaveAsync()))
                {
                    throw new Exception($"Upserting book {id} for author {authorId} failed on save.");
                }

                var bookToReturn = AutoMapper.Mapper.Map<BookDto>(bookToAdd);
                return CreatedAtRoute("GetBookForAuthor",
                    new { authorId = authorId, id = bookToReturn.Id },
                    bookToReturn);
            }

            var bookToPatch = AutoMapper.Mapper.Map<BookForUpdateDto>(bookForAuthorFromRepo);

            //patchDoc.ApplyTo(bookToPatch, ModelState);

            patchDoc.ApplyTo(bookToPatch);

            if (bookToPatch.Description == bookToPatch.Title)
            {
                ModelState.AddModelError(nameof(BookForUpdateDto),
                    "The provided description should be different from the title.");
            }

            TryValidateModel(bookToPatch);

            if (!ModelState.IsValid)
            {
                return new UnprocessableEntityObjectResult(ModelState);
            }

            AutoMapper.Mapper.Map(bookToPatch, bookForAuthorFromRepo);

            _libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);

            if (!(await _libraryRepository.SaveAsync()))
            {
                throw new Exception($"Patching book {id} for author {authorId} failed on save.");
            }

            return NoContent();
        }

    }
}