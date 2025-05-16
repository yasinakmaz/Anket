using System.ComponentModel.DataAnnotations;

namespace Anket.Models;
public class AnketRecord
{
    [Key]
    public int Id { get; set; } // Auto-increment birincil anahtar

    [Required]
    public int AnketID { get; set; }

    [Required]
    public DateTime Date { get; set; }

    public int OzelDurum { get; set; }

    [Required]
    public string DeviceID { get; set; } = string.Empty;

    public bool IsProcessed { get; set; } = false;

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
    public string CreateDate { get; init; } = string.Empty;
    public string DeviceID { get; init; } = string.Empty;
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