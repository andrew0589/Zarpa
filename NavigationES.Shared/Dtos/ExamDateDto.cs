namespace NavigationES.Shared.Dtos
{
    // The user's own exam date (PUT /api/account/exam-date body and GET answer).
    // Null clears it.
    public record ExamDateDto(DateOnly? Date);
}
