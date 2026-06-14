namespace NestLaserDesktop.Models;

public class DxfExportOptions
{
    public bool ExportHiddenLayers { get; set; }
    public bool ExportReferenceLayer { get; set; }
    public bool SelectedOnly { get; set; }
    public bool ExportPlateBorders { get; set; }
    public bool ExportUnplacedParts { get; set; }
    public bool UseNestResult { get; set; } = true;
    public bool IncludeOperationOrder { get; set; } = true;
}
