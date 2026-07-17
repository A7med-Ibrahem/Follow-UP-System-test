namespace SmartFollowUp.API.DTOs
{
    public class CreateNoteRequestDto
    {
        public long CaseId { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    public class NoteResponseDto
    {
        public long Id { get; set; }
        public long CaseId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}