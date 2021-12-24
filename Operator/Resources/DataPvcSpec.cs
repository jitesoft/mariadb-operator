namespace Jitesoft.MariaDBOperator.crd;

public class DataPvcSpec
{
    public string Size { get; set; } = "10G";
    public string? StorageType { get; set; } = null;
}
