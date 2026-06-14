using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NestLaserDesktop.Models;

public class LaserOperation : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString("N")[..8];
    private string _name = "Operasyon";
    private string _layerId = string.Empty;
    private string _layerName = string.Empty;
    private string _color = "#569CD6";
    private OperationType _type = OperationType.CutOuter;
    private double _power = 80;
    private double _speed = 20;
    private SpeedUnit _speedUnit = SpeedUnit.MmPerSecond;
    private int _passCount = 1;
    private int _priority;
    private bool _enabled = true;

    public string Id { get => _id; set { _id = value; OnPropertyChanged(); } }
    public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
    public string LayerId { get => _layerId; set { _layerId = value; OnPropertyChanged(); } }
    public string LayerName { get => _layerName; set { _layerName = value; OnPropertyChanged(); } }
    public string Color { get => _color; set { _color = value; OnPropertyChanged(); } }
    public OperationType Type { get => _type; set { _type = value; OnPropertyChanged(); } }
    public double Power { get => _power; set { _power = value; OnPropertyChanged(); } }
    public double Speed { get => _speed; set { _speed = value; OnPropertyChanged(); } }
    public SpeedUnit SpeedUnit { get => _speedUnit; set { _speedUnit = value; OnPropertyChanged(); } }
    public int PassCount { get => _passCount; set { _passCount = value; OnPropertyChanged(); } }
    public int Priority { get => _priority; set { _priority = value; OnPropertyChanged(); } }
    public bool Enabled { get => _enabled; set { _enabled = value; OnPropertyChanged(); } }

    public event PropertyChangedEventHandler? PropertyChanged;

    public LaserOperation Clone() => new()
    {
        Id = Id,
        Name = Name,
        LayerId = LayerId,
        LayerName = LayerName,
        Color = Color,
        Type = Type,
        Power = Power,
        Speed = Speed,
        SpeedUnit = SpeedUnit,
        PassCount = PassCount,
        Priority = Priority,
        Enabled = Enabled
    };

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
