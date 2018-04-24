using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Library.API.Entities;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers
{   
    [Route("api/Authors")]    
    public class AuthorsController : Controller
    {
        private ILibraryRepository _libraryRepository;
               
        public AuthorsController(ILibraryRepository libraryRepository)
        {
            _libraryRepository = libraryRepository;
        }      

        [HttpGet]
        public async Task<IActionResult> GetAuthors()
        {            
            var authorsFromRepo = await _libraryRepository.GetAuthorsAsync();
            var authors = AutoMapper.Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);
            return Ok(authors);
        }

        [HttpGet("{id}", Name = "GetAuthor")]
        public async Task<IActionResult> GetAuthorAsync(Guid id)
        {
            var authorFromRepo = await _libraryRepository.GetAuthorAsync(id);
            if (authorFromRepo == null)
            {
                return NotFound();
            }
            var author = AutoMapper.Mapper.Map<AuthorDto>(authorFromRepo);
            return Ok(author);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAuthor([FromBody] AuthorForCreationDto author)
        {
            if (author == null)
            {
                return BadRequest("Error");
            }
            var authorEntity = AutoMapper.Mapper.Map<Author>(author);
            _libraryRepository.AddAuthorAsync(authorEntity);
            if (! (await _libraryRepository.SaveAsync()))
            {
                throw new Exception("Creating an author failed on save.");
                // return StatusCode(500, "A problem happened with handling your request.");
            }
            var authorToReturn = AutoMapper.Mapper.Map<AuthorDto>(authorEntity);
            return CreatedAtRoute("GetAuthor",
                new { id = authorToReturn.Id },
                authorToReturn);
        }
        
        [HttpPost("{id}")]
        public async Task<IActionResult> BlockAuthorCreation(Guid id)
        {
            if(await _libraryRepository.AuthorExistsAsync(id))
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }
            return NotFound();
        }

        [HttpDelete("{id}")]        
        public async Task<IActionResult> DeleteAuthor(Guid id)
        {
            var authorFromRepo = await _libraryRepository.GetAuthorAsync(id);
            if (authorFromRepo == null)
            {
                return NotFound();
            }
            _libraryRepository.DeleteAuthor(authorFromRepo);            
            if (!(await _libraryRepository.SaveAsync()))
            {
                throw new Exception($"Deleting author {id} failed on save.");
            }
            return NoContent();
        }
    }
}


