using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebSiteBanHang.Models;
using WebSiteBanHang.Models.ViewModels;

namespace WebSiteBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ContentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Content
        public async Task<IActionResult> Index(string searchTerm = "", string contentType = "")
        {
            var query = _context.Contents.AsQueryable();
            
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c => c.Title.Contains(searchTerm) || c.Body.Contains(searchTerm));
            }
            
            if (!string.IsNullOrEmpty(contentType))
            {
                query = query.Where(c => c.ContentType == contentType);
            }
            
            var contents = await query
                .OrderBy(c => c.ContentType)
                .ThenBy(c => c.DisplayOrder)
                .ToListAsync();
                
            var viewModel = new ContentListViewModel
            {
                Contents = contents,
                SearchTerm = searchTerm,
                ContentType = contentType
            };
            
            return View(viewModel);
        }

        // GET: Admin/Content/Create
        public IActionResult Create()
        {
            var viewModel = new ContentViewModel
            {
                Content = new Content()
            };
            return View(viewModel);
        }

        // POST: Admin/Content/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContentViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                viewModel.Content.CreatedAt = DateTime.Now;
                viewModel.Content.UpdatedAt = DateTime.Now;
                _context.Add(viewModel.Content);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }

        // GET: Admin/Content/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var content = await _context.Contents.FindAsync(id);
            if (content == null)
            {
                return NotFound();
            }
            
            var viewModel = new ContentViewModel
            {
                Content = content
            };
            
            return View(viewModel);
        }

        // POST: Admin/Content/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ContentViewModel viewModel)
        {
            if (id != viewModel.Content.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    viewModel.Content.UpdatedAt = DateTime.Now;
                    _context.Update(viewModel.Content);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContentExists(viewModel.Content.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }

        // GET: Admin/Content/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var content = await _context.Contents
                .FirstOrDefaultAsync(m => m.Id == id);
            if (content == null)
            {
                return NotFound();
            }

            return View(content);
        }

        // POST: Admin/Content/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var content = await _context.Contents.FindAsync(id);
            _context.Contents.Remove(content);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContentExists(int id)
        {
            return _context.Contents.Any(e => e.Id == id);
        }
    }
} 