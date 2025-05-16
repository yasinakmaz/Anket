using System.ComponentModel.DataAnnotations;

namespace Anket.Models;
public record AnketRecord
{
    [Key]
    [Column("AnketID")]
    public int AnketID { get; init; } 
    
    [Column("Date")]
    public DateTime Date { get; init; }
    
    [Column("OzelDurum")]
    public int OzelDurum { get; init; }
    
    [Column("DeviceID")]
    public string DeviceID { get; init; } = string.Empty; 
    
    [Column("IsProcessed")]
    public bool IsProcessed { get; init; } = false; 
    
    public AnketRecord() { }
    
    public AnketRecord(int anketID, DateTime date, int ozelDurum, string deviceID, bool isProcessed = false)
    {
        AnketID = anketID;
        Date = date;
        OzelDurum = ozelDurum;
        DeviceID = deviceID;
        IsProcessed = isProcessed;
    }
}

public record FirebaseAnketModel
{
    public string MemnunInd { get; init; } = "0"; 
    public string CreateDate { get; init; }
    public string DeviceID { get; init; } = "";
    public bool IsProcessed { get; init; } = false;

    public static FirebaseAnketModel FromAnketRecord(AnketRecord record)
    {
        string memnunInd = record.AnketID switch
        {
            1001 => "0",  // Mutlu
            1002 => "1",  // Nötr
            1003 => "2",  // Üzgün
            _ => "1"      // Varsayılan olarak Nötr
        };

        return new FirebaseAnketModel
        {
            MemnunInd = memnunInd,
            CreateDate = record.Date.ToString("yyyy-MM-ddTHH:mm:ss"),
            DeviceID = record.DeviceID,
            IsProcessed = record.IsProcessed
        };
    }
} 