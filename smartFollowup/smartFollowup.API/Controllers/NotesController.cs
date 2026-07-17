using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartFollowUp.API.DTOs;
using SmartFollowUp.API.Services;
using System.Security.Claims;

namespace SmartFollowUp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotesController : ControllerBase
    {
        private readonly NoteService _noteService;

        public NotesController(NoteService noteService)
        {
            _noteService = noteService;
        }

        // POST api/notes
        [HttpPost]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> AddNote(CreateNoteRequestDto request)
        {
            var doctorId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _noteService.AddNoteAsync(request, doctorId);
            if (result == null)
                return BadRequest(new { message = "Case not found or not assigned to you" });

            return Ok(result);
        }

        // GET api/notes/case/{caseId}
        [HttpGet("case/{caseId}")]
        [Authorize(Roles = "Doctor,Patient")]
        public async Task<IActionResult> GetCaseNotes(long caseId)
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _noteService.GetCaseNotesAsync(caseId, userId);

            if (result == null)
                return NotFound(new { message = "Case not found or not accessible" });

            return Ok(result);
        }
    }
}