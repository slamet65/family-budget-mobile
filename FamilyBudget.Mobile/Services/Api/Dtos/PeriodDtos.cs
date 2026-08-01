namespace FamilyBudget.Mobile.Services.Api.Dtos;

public record PeriodDto(int Id, string? Name, DateTimeOffset StartDate, string Status, DateTimeOffset? ClosedAt, DateTimeOffset CreatedAt)
{
    public bool IsOpen => Status == "open";
}

public record CreatePeriodRequest(DateTimeOffset StartDate, string? Name);
