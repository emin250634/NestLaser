using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NestLaserDesktop.Models;

public class LayerModel : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString("N")[..8];
    private string _name = "Cut";
    private LayerType _type = LayerType.Cut;
    private string _color = "#4EC9B0";
    private bool _isVisible = true;
    private bool _isLocked;
    private double _power = 80;
    private double _speed = 20;
    private int _passCount = 1;
    private int _order;

    public string Id { get => _id; set { _id = value; OnPropertyChanged(); } }
    public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
    public LayerType Type { get => _type; set { _type = value; OnPropertyChanged(); } }
    public string Color { get => _color; set { _color = value; OnPropertyChanged(); } }
    public bool IsVisible { get => _isVisible; set { _isVisible = value; OnPropertyChanged(); } }
    public bool IsLocked { get => _isLocked; set { _isLocked = value; OnPropertyChanged(); } }
    public double Power { get => _power; set { _power = value; OnPropertyChanged(); } }
    public double Speed { get => _speed; set { _speed = value; OnPropertyChanged(); } }
    public int PassCount { get => _passCount; set { _passCount = value; OnPropertyChanged(); } }
    public int Order { get => _order; set { _order = value; OnPropertyChanged(); } }

    public event PropertyChangedEventHandler? PropertyChanged;

    public LayerModel Clone() => new()
    {
        Id = Id,
        Name = Name,
        Type = Type,
        Color = Color,
        IsVisible = IsVisible,
        IsLocked = IsLocked,
        Power = Power,
        Speed = Speed,
        PassCount = PassCount,
        Order = Order
    };

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
