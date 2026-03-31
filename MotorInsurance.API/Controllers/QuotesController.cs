using Microsoft.AspNetCore.Mvc;
using MotorInsurance.API.DTOs.Quote;
using MotorInsurance.API.Services.Quote;

namespace MotorInsurance.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuotesController : ControllerBase
    {
        private readonly IQuoteService _service;

        public QuotesController(IQuoteService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateQuoteDto dto)
        {
            try
            {
                var quote = await _service.CreateAsync(dto);
                return Ok(quote);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

       
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var result = await _service.ApproveQuoteAsync(id);

                if (!result)
                    return NotFound("Quote not found");

                return Ok("Quote approved and policy created");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}