namespace Vigia.Application.Plugins;

/// <summary>
/// Value type of a config field, as exposed to UI, CLI and Terraform.
/// </summary>
public enum ConfigFieldType
{
    String,
    Integer,
    Number,
    Boolean,

    /// <summary>Duration string such as <c>30s</c>.</summary>
    Duration,

    /// <summary>List of values; see <see cref="ConfigField.ItemType"/>.</summary>
    List,

    /// <summary>String to string map, for example HTTP headers.</summary>
    Map,
}
