using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CapstoneProjectPUSH_API.Data;
using CapstoneProjectPUSH_API.Dtos;
using CapstoneProjectPUSH_API.Models;
using Microsoft.AspNetCore.Authorization;
using CapstoneProjectPUSH_API.Services;

namespace CapstoneProjectPUSH_API.Controllers
{
    // Articles are read-only and open to everyone
    [ApiController]
    [Route("api/[controller]")]
    public class ArticlesController : PushControllerBase
    {
        private readonly PushDbContext _db;
        private readonly ImageStorage _imageStorage;

        public ArticlesController(PushDbContext db, ImageStorage imageStorage)
        {
            _db = db;
            _imageStorage = imageStorage;
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

        // POST api/articles
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddArticle(ArticleRequest request)
        {
            // Validate a copy first
            Article article = new Article();
            CopyRequestToArticle(request, article);

            // Validation
            string? error = await ValidateAsync(article, 0);

            if (error != null)
            {
                return BadRequest(new MessageResponse(false, error));
            }

            // Add article
            _db.Articles.Add(article);
            await _db.SaveChangesAsync();

            return Ok(new CreatedResponse { Success = true, Message = article.Title + " was added.", Id = article.Id });
        }

        // PUT api/articles/id
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateArticle(int id, ArticleRequest request)
        {
            // Database check
            Article? existing = await _db.Articles.FindAsync(id);

            if (existing == null)
            {
                return NotFound(new MessageResponse(false, "Article could not be found."));
            }

            // Validate a copy first, so a failed edit doesn't change the tracked article
            Article updated = new Article();
            CopyRequestToArticle(request, updated);

            // Validation
            string? error = await ValidateAsync(updated, id);

            if (error != null)
            {
                return BadRequest(new MessageResponse(false, error));
            }

            // Update article
            existing.Title = updated.Title;
            existing.Topic = updated.Topic;
            existing.Content = updated.Content;
            existing.Citation = updated.Citation;
            existing.CitationUrl = updated.CitationUrl;
            existing.DateTimePublished = updated.DateTimePublished;

            await _db.SaveChangesAsync();

            return Ok(new MessageResponse(true, existing.Title + " was updated."));
        }

        // DELETE api/articles/id
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteArticle(int id)
        {
            // Database check
            Article? article = await _db.Articles.FindAsync(id);

            if (article == null)
            {
                return NotFound(new MessageResponse(false, "Article could not be found."));
            }

            _db.Articles.Remove(article);
            await _db.SaveChangesAsync();

            // Delete image
            _imageStorage.Delete(article.ImageUrl);

            return Ok(new MessageResponse(true, "Article deleted."));
        }

        // Helpers

        // Creates a placeholder of the request article
        private static void CopyRequestToArticle(ArticleRequest request, Article article)
        {
            article.Title = Clean(request.Title);
            article.Topic = Clean(request.Topic);
            article.Content = Clean(request.Content);
            article.Citation = Clean(request.Citation);
            article.CitationUrl = Clean(request.CitationUrl);
            article.DateTimePublished = request.DateTimePublished;
        }

        // Validate article details (ignoreArticleId is 0 when adding)
        private async Task<string?> ValidateAsync(Article article, int ignoreArticleId)
        {
            if (article.Title.Length < 2 || article.Title.Length > 100)
            {
                return "Title must be between 2 and 100 characters.";
            }

            if (article.Topic.Length == 0 || article.Topic.Length > 50)
            {
                return "Topic is required (50 characters max).";
            }

            if (article.Content.Length == 0 || article.Content.Length > 5000)
            {
                return "Content is required (5000 characters max).";
            }

            if (article.Citation.Length == 0 || article.Citation.Length > 300)
            {
                return "Citation is required (300 characters max).";
            }

            if (article.CitationUrl.Length > 0)
            {
                if (article.CitationUrl.Length > 300 || !IsValidLink(article.CitationUrl))
                {
                    return "Source link must be a valid web address (300 characters max).";
                }
            }

            if (article.DateTimePublished == DateTime.MinValue)
            {
                return "Please choose a published date.";
            }

            if (article.DateTimePublished > DateTime.UtcNow)
            {
                return "Published date can't be in the future.";
            }

            bool titleTaken = await _db.Articles.AnyAsync(a => a.Id != ignoreArticleId && a.Title.ToLower() == article.Title.ToLower());

            if (titleTaken)
            {
                return "An article called \"" + article.Title + "\" already exists.";
            }

            return null;
        }

        // Only allows full http or https links
        private static bool IsValidLink(string link)
        {
            Uri? uri;

            if (!Uri.TryCreate(link, UriKind.Absolute, out uri))
            {
                return false;
            }

            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static string Clean(string? value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value.Trim();
        }
    }
}