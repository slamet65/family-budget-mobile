namespace FamilyBudget.Mobile.Services.Api.Dtos;

public record SyncBootstrapDto(long Cursor);
public record SyncChangeDto(long Sequence, string EntityType, int EntityId, string Operation, DateTimeOffset ChangedAt);
public record SyncChangesDto(List<SyncChangeDto> Changes, long NextCursor, bool HasMore);
