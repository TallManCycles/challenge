using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

public class GarminActivity
{
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    public string SummaryId { get; set; } = string.Empty;
    
    public string? ActivityId { get; set; }
    
    [Required]
    public GarminActivityType ActivityType { get; set; }
    
    public string? ActivityName { get; set; }
    
    [Required]
    public DateTime StartTime { get; set; }
    
    public int StartTimeOffsetInSeconds { get; set; }
    
    [Required]
    public int DurationInSeconds { get; set; }
    
    public double? DistanceInMeters { get; set; }
    
    public double? TotalElevationGainInMeters { get; set; }
    
    public double? TotalElevationLossInMeters { get; set; }
    
    public int? ActiveKilocalories { get; set; }
    
    public string? DeviceName { get; set; }
    
    public bool IsManual { get; set; }
    
    public bool IsWebUpload { get; set; }
    
    [Required]
    [Column(TypeName = "jsonb")]
    public string ResponseData { get; set; } = string.Empty;
    
    [Required]
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ProcessedAt { get; set; }
    
    public bool IsProcessed { get; set; } = false;
    
    public string? ProcessingError { get; set; }

    public User User { get; set; } = null!;
}

public enum GarminActivityType
{
    RUNNING,
    INDOOR_RUNNING,
    OBSTACLE_RUN,
    STREET_RUNNING,
    TRACK_RUNNING,
    TRAIL_RUNNING,
    TREADMILL_RUNNING,
    ULTRA_RUN,
    VIRTUAL_RUN,
    CYCLING,
    BMX,
    CYCLOCROSS,
    DOWNHILL_BIKING,
    E_BIKE_FITNESS,
    E_BIKE_MOUNTAIN,
    E_ENDURO_MTB,
    ENDURO_MTB,
    GRAVEL_CYCLING,
    INDOOR_CYCLING,
    MOUNTAIN_BIKING,
    RECUMBENT_CYCLING,
    ROAD_BIKING,
    TRACK_CYCLING,
    VIRTUAL_RIDE,
    HANDCYCLING,
    INDOOR_HANDCYCLING,
    FITNESS_EQUIPMENT,
    BOULDERING,
    ELLIPTICAL,
    INDOOR_CARDIO,
    HIIT,
    INDOOR_CLIMBING,
    INDOOR_ROWING,
    MOBILITY,
    PILATES,
    STAIR_CLIMBING,
    STRENGTH_TRAINING,
    YOGA,
    MEDITATION,
    SWIMMING,
    LAP_SWIMMING,
    OPEN_WATER_SWIMMING,
    WALKING,
    CASUAL_WALKING,
    SPEED_WALKING,
    HIKING,
    RUCKING,
    WINTER_SPORTS,
    BACKCOUNTRY_SNOWBOARDING,
    BACKCOUNTRY_SKIING,
    CROSS_COUNTRY_SKIING_WS,
    RESORT_SKIING,
    SNOWBOARDING_WS,
    RESORT_SKIING_SNOWBOARDING_WS,
    SKATE_SKIING_WS,
    SKATING_WS,
    SNOW_SHOE_WS,
    SNOWMOBILING_WS,
    WATER_SPORTS,
    BOATING_V2,
    BOATING,
    FISHING_V2,
    FISHING,
    KAYAKING_V2,
    KAYAKING,
    KITEBOARDING_V2,
    KITEBOARDING,
    OFFSHORE_GRINDING_V2,
    OFFSHORE_GRINDING,
    ONSHORE_GRINDING_V2,
    ONSHORE_GRINDING,
    PADDLING_V2,
    PADDLING,
    ROWING_V2,
    ROWING,
    SAILING_V2,
    SAILING,
    SNORKELING,
    STAND_UP_PADDLEBOARDING_V2,
    STAND_UP_PADDLEBOARDING,
    SURFING_V2,
    SURFING,
    WAKEBOARDING_V2,
    WAKEBOARDING,
    WATERSKIING,
    WHITEWATER_RAFTING_V2,
    WHITEWATER_RAFTING,
    WINDSURFING_V2,
    WINDSURFING,
    TRANSITION_V2,
    BIKE_TO_RUN_TRANSITION_V2,
    BIKE_TO_RUN_TRANSITION,
    RUN_TO_BIKE_TRANSITION_V2,
    RUN_TO_BIKE_TRANSITION,
    SWIM_TO_BIKE_TRANSITION_V2,
    SWIM_TO_BIKE_TRANSITION,
    TEAM_SPORTS,
    AMERICAN_FOOTBALL,
    BASEBALL,
    BASKETBALL,
    CRICKET,
    FIELD_HOCKEY,
    ICE_HOCKEY,
    LACROSSE,
    RUGBY,
    SOCCER,
    SOFTBALL,
    ULTIMATE_DISC,
    VOLLEYBALL,
    RACKET_SPORTS,
    BADMINTON,
    PADDELBALL,
    PICKLEBALL,
    PLATFORM_TENNIS,
    RACQUETBALL,
    SQUASH,
    TABLE_TENNIS,
    TENNIS,
    TENNIS_V2,
    OTHER,
    BOXING,
    BREATHWORK,
    DANCE,
    DISC_GOLF,
    FLOOR_CLIMBING,
    GOLF,
    INLINE_SKATING,
    JUMP_ROPE,
    MIXED_MARTIAL_ARTS,
    MOUNTAINEERING,
    ROCK_CLIMBING,
    STOP_WATCH,
    PARA_SPORTS,
    WHEELCHAIR_PUSH_RUN,
    WHEELCHAIR_PUSH_WALK
}