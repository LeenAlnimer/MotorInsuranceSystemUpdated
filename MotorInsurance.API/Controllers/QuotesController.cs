using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorInsurance.API.DTOs.Quote;
using MotorInsurance.API.Services.Quote;

namespace MotorInsurance.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class QuotesController : ControllerBase
    {
        private readonly IQuoteService _service;

        public QuotesController(IQuoteService service)
        {
            _service = service;
        }

        // CREATE
        [HttpPost]
        public async Task<IActionResult> Create(CreateQuoteDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return Ok(result);
        }

        // GET ALL
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        // GET BY ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // APPROVE
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            var result = await _service.ApproveQuoteAsync(id);

            if (!result)
                return NotFound("Quote not found");

            return Ok(new { message = "Quote approved & policy created" });
        }

        // REJECT
        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(int id)
        {
            var result = await _service.RejectQuoteAsync(id);

            if (!result)
                return NotFound();

            return Ok(new { message = "Quote rejected" });
        }

        // DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);

            if (!result)
                return NotFound();

            return Ok(new { message = "Quote deleted" });
        }
    }
}