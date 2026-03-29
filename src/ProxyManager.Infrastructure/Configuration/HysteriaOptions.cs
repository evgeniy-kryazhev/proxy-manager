namespace ProxyManager.Infrastructure.Configuration;

public sealed class HysteriaOptions
{
    public const string SectionName = "Hysteria";

    public string ConfigPath { get; set; } = "/opt/hysteria/config.yaml";

    public string WorkingDirectory { get; set; } = "/opt/hysteria";

    public string ComposeFilePath { get; set; } = "/opt/hysteria/docker-compose.yml";

    public string Host { get; set; } = "excraft.ru";

    public int Port { get; set; } = 443;

    public string Sni { get; set; } = "excraft.ru";
}
