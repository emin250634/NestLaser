namespace NestLaserDesktop.Models;

public class CompanyProfile
{
    public string CompanyName { get; set; } = "NestLaser";
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string LogoPath { get; set; } = string.Empty;

    public CompanyProfile Clone() => new()
    {
        CompanyName = CompanyName,
        Address = Address,
        Phone = Phone,
        Email = Email,
        Website = Website,
        LogoPath = LogoPath
    };
}
