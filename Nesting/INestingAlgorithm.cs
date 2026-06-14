using System.Collections.Generic;
using NestLaserDesktop.Models;

namespace NestLaserDesktop.Nesting;

public interface INestingAlgorithm
{
    string AlgorithmName { get; }
    string AlgorithmVersion { get; }
    NestResult Nest(List<PartModel> parts, PlateModel plate, NestSettings settings);
    bool IsExperimental { get; }
}