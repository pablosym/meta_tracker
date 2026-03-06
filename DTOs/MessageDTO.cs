namespace Tracker.DTOs;

public class MessageDTO
{
    public Estatus Status { get; set; }
    public string Value { get; set; } = string.Empty;
    public int TagId { get; set; }
    public object? TagObj { get; set; }

    public bool IsOk => Status == Estatus.OK;

    public enum Estatus
    {
        OK,
        ERROR,
        WARNING,
        INFO,
        REDIRECT,
        REDIRECT_BLANK,
        NO_MODAL,
        NO_SESSION,
        PERMISSION,
        MIXED
    }

    // 🔧 Fabric methods
    public static MessageDTO Ok(string value, object? tagObj = null, int tagId = 0)
        => Create(Estatus.OK, value, tagObj, tagId);

    public static MessageDTO Error(string value, object? tagObj = null, int tagId = 0)
        => Create(Estatus.ERROR, value, tagObj, tagId);

    public static MessageDTO Warning(string value, object? tagObj = null, int tagId = 0)
        => Create(Estatus.WARNING, value, tagObj, tagId);

    public static MessageDTO Info(string value, object? tagObj = null, int tagId = 0)
        => Create(Estatus.INFO, value, tagObj, tagId);

    public static MessageDTO Redirect(string url)
        => Create(Estatus.REDIRECT, url);

    // 
    private static MessageDTO Create(Estatus status, string value, object? tagObj = null, int tagId = 0)
    {
        if (status == Estatus.ERROR)
            Helpers.Error.WriteLog(value); // o delegar este comportamiento externamente

        return new MessageDTO
        {
            Status = status,
            Value = value,
            TagId = tagId,
            TagObj = tagObj
        };
    }
}
