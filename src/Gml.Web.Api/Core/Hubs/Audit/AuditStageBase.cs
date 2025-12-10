namespace Gml.Web.Api.Core.Hubs.Audit;

/// <summary>
/// Базовый этап аудита
/// </summary>
/// <param name="name">Наименование "Что проверяется"</param>
/// <param name="description">Описание этапа</param>
/// <param name="subStages">Что проверяется поэтапно</param>
public abstract class AuditStageBase(string name, string description, List<string> subStages)
{
    public string Name { get; } = name;
    public string Description { get; } = description;
    public List<string> SubStages { get; } = subStages;
    public AuditResult Result { get; } = new();
    public string? Host { get; set; }

    public abstract Task Evaluate();


    protected void AddError(string message)
    {
        Result.Messages.Add(new AuditMessage(AuditMessageType.Error, message));
    }

    protected void AddSuccess(string message)
    {
        Result.Messages.Add(new AuditMessage(AuditMessageType.Success, message));
    }

    protected void AddDefault(string message)
    {
        Result.Messages.Add(new AuditMessage(AuditMessageType.Default, message));
    }

    protected void AddWarning(string message)
    {
        Result.Messages.Add(new AuditMessage(AuditMessageType.Warning, message));
    }
}
