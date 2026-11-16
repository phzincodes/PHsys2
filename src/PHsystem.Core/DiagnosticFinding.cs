namespace PHsystem.Core;

public enum FindingSeverity { Information, Warning, Critical }
public sealed record DiagnosticFinding(string Title, string Description, string Evidence, string Impact,
    FindingSeverity Severity, double Confidence, string Source, DateTimeOffset ObservedAt,
    string PossibleAction, string Risk, bool Reversible, bool RequiresAdministrator);
