namespace NestLaserDesktop.Utilities;

public static class AppConstants
{
    public const string AppName = "NestLaser Desktop";
    public const string AppVersion = "0.1.0";

    public const double DefaultPlateWidth = 1000;
    public const double DefaultPlateHeight = 2000;
    public const double DefaultMargin = 5;
    public const double DefaultGap = 2;
    public const double DefaultMaterialThickness = 0;

    public const int CircleSegments = 36;
    public const double MinPartSize = 1;
    public const double MaxPartSize = 10000;

    public const string DxfFilter = "DXF Dosyası (*.dxf)|*.dxf|Tüm Dosyalar (*.*)|*.*";
    public const string DxfSaveFilter = "DXF Dosyası (*.dxf)|*.dxf";

    public static class Colors
    {
        public const string PlateBackground = "#2D2D30";
        public const string PlateBorder = "#569CD6";
        public const string PartDefault = "#4EC9B0";
        public const string Accent = "#569CD6";
        public const string Warning = "#F44747";
        public const string Success = "#4EC9B0";
    }
}
