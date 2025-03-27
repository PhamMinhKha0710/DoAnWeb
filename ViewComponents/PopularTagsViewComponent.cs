using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnWeb.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnWeb.ViewComponents
{
    public class PopularTagsViewComponent : ViewComponent
    {
        private readonly DevCommunityContext _context;

        public PopularTagsViewComponent(DevCommunityContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int count = 10)
        {
            // Get tags with their usage count in questions
            var popularTags = await _context.Tags
                .Include(t => t.QuestionTags)
                .Select(t => new
                {
                    Name = t.TagName,
                    Count = t.QuestionTags.Count
                })
                .OrderByDescending(t => t.Count)
                .Take(count)
                .ToListAsync();

            // Convert to tuple list for the view
            var results = popularTags.Select(t => (t.Name, t.Count)).ToList();
            
            return View(results);
        }
    }
} 