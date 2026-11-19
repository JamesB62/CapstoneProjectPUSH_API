using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CapstoneProjectPUSH_API.Data;
using CapstoneProjectPUSH_API.Dtos;
using CapstoneProjectPUSH_API.Models;

namespace CapstoneProjectPUSH_API.Controllers
{
    // Articles are read-only and open to everyone
    [ApiController]
    [Route("api/[controller]")]
    public class ArticlesController : PushControllerBase
    {
        private readonly PushDbContext _db;

        public ArticlesController(PushDbContext db)
        {
            _db = db;
        }

        // GET api/articles
        [HttpGet]
        public async Task<IActionResult> GetArticles()
        {
            List<Article> articles = await _db.Articles
                .OrderByDescending(a => a.DateTimePublished)
                .ToListAsync();

            return Ok(articles);
        }

        // GET api/articles/count
        [HttpGet("count")]
        public async Task<IActionResult> GetArticleCount()
        {
            int count = await _db.Articles.CountAsync();
            return Ok(count);
        }

        // GET api/articles/id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetArticle(int id)
        {
            Article? article = await _db.Articles.FindAsync(id);

            if (article == null)
            {
                return NotFound(new MessageResponse(false, "Article could not be found."));
            }
            else
            {
                return Ok(article);
            }
        }
    }
}
