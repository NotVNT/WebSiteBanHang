using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebSiteBanHang.Models;
using WebSiteBanHang.Repositories;

namespace WebSiteBanHang.Controllers.Api
{
    [Route("api/products")]
    [ApiController]
    [Produces("application/json")]
    public class ProductApiController : ControllerBase
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public ProductApiController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        /// <summary>
        /// Lấy danh sách tất cả sản phẩm
        /// </summary>
        /// <returns>Danh sách sản phẩm</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            var products = await _productRepository.GetAllAsync();
            return Ok(products);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một sản phẩm theo ID
        /// </summary>
        /// <param name="id">ID của sản phẩm</param>
        /// <returns>Thông tin sản phẩm</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            
            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        /// <summary>
        /// Tạo mới sản phẩm
        /// </summary>
        /// <param name="product">Thông tin sản phẩm cần tạo</param>
        /// <returns>Sản phẩm đã được tạo</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Product>> CreateProduct(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Kiểm tra CategoryId có tồn tại không
            if (product.CategoryId != 0)
            {
                var category = await _categoryRepository.GetByIdAsync(product.CategoryId);
                if (category == null)
                {
                    return BadRequest("Danh mục không tồn tại");
                }
            }

            await _productRepository.AddAsync(product);
            
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        /// <summary>
        /// Cập nhật thông tin sản phẩm
        /// </summary>
        /// <param name="id">ID của sản phẩm</param>
        /// <param name="product">Thông tin sản phẩm cập nhật</param>
        /// <returns>Không có nội dung trả về</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProduct(int id, Product product)
        {
            if (id != product.Id)
            {
                return BadRequest("ID trong route không khớp với ID trong dữ liệu");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Kiểm tra sản phẩm có tồn tại không
            var existingProduct = await _productRepository.GetByIdAsync(id);
            if (existingProduct == null)
            {
                return NotFound();
            }

            // Kiểm tra CategoryId có tồn tại không
            if (product.CategoryId != 0)
            {
                var category = await _categoryRepository.GetByIdAsync(product.CategoryId);
                if (category == null)
                {
                    return BadRequest("Danh mục không tồn tại");
                }
            }

            try
            {
                await _productRepository.UpdateAsync(product);
                return NoContent();
            }
            catch (Exception ex)
            {
                // Log lỗi
                return StatusCode(500, "Đã xảy ra lỗi khi cập nhật sản phẩm: " + ex.Message);
            }
        }

        /// <summary>
        /// Xóa sản phẩm
        /// </summary>
        /// <param name="id">ID của sản phẩm cần xóa</param>
        /// <returns>Không có nội dung trả về</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            await _productRepository.DeleteAsync(id);

            return NoContent();
        }
    }
} 