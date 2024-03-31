namespace HashCrack.Components.Model;

public record CrackTask(Guid Id, List<string> HashSources, Status Status = Status.InProgress);