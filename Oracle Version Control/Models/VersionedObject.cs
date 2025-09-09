using System.Data;

namespace Oracle_Version_Control.Models;

public class VersionedObject
{
    public string Objeto { get; set; } = string.Empty;
    public string ObjectType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string Checkout { get; set; } = string.Empty;
    public string Checkin { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
    public string IsControlled { get; set; } = "Y";
    public int DisplayIndex { get; set; } = 0;

    public string StatusDisplay => Status == "Y" ? "Check-out" : 
                                   Status == "Não controlado" ? "Não controlado" : "Livre";
    
    public Color StatusColor => Status == "Y" ? Colors.Red : 
                               Status == "Não controlado" ? Colors.Gold : Colors.Green;
    
    public bool HasComment => !string.IsNullOrEmpty(Comments);

    public VersionedObject() { }

    public VersionedObject(DataRow row)
    {
        Objeto = row["Objeto"]?.ToString() ?? string.Empty;
        ObjectType = row["ObjectType"]?.ToString() ?? string.Empty;
        Status = row["Status"]?.ToString() ?? string.Empty;
        Usuario = row["Usuario"]?.ToString() ?? string.Empty;
        Checkout = row["Checkout"]?.ToString() ?? string.Empty;
        Checkin = row["Checkin"]?.ToString() ?? string.Empty;
        Comments = row["Comments"]?.ToString() ?? string.Empty;
        IsControlled = row["IsControlled"]?.ToString() ?? "Y";
    }
}