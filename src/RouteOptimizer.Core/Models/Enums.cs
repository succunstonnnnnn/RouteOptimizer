namespace RouteOptimizer.Core.Models;

public enum ServiceType
{
    Interior,
    Exterior,
    Floral
}

public enum SkillLevel
{
    Junior = 0,
    Medior = 1,
    Senior = 2
}

public enum PhysicalDemand
{
    Light = 0,
    Medium = 1,
    Hard = 2
}

public enum VisitFrequency
{
    Weekly = 1,
    BiWeekly = 2,
    ThreeWeekly = 3,
    FourWeeks = 4
}

public enum LocationType
{
    TechnicianHome,
    ServiceSite,
    ParkingHub
}

public enum TransportType
{
    Car,
    PublicTransport,
    Either
}

public enum PermitDifficulty
{
    Easy,
    Medium,
    Hard
}

public enum WorkLocation
{
    Home,
    Office,
    Either
}
