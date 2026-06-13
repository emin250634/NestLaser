namespace NestLaserDesktop.Services;

public class WorkflowProgress
{
    public WorkflowProgress(string message, double? percent = null)
    {
        Message = message;
        Percent = percent;
    }

    public string Message { get; }
    public double? Percent { get; }
}
