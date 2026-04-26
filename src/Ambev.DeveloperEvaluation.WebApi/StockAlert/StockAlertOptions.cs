namespace Ambev.DeveloperEvaluation.WebApi.StockAlert;

/// <summary>
/// Configurações do serviço de alerta de estoque via Mailjet.
/// Populado a partir da seção "StockAlert" do appsettings / variáveis de ambiente.
/// </summary>
public class StockAlertOptions
{
    public const string SectionName = "StockAlert";

    /// <summary>API Key do Mailjet (obrigatório para envio de e-mail).</summary>
    public string MailjetApiKey { get; set; } = string.Empty;

    /// <summary>Secret Key do Mailjet (obrigatório para envio de e-mail).</summary>
    public string MailjetSecretKey { get; set; } = string.Empty;

    /// <summary>E-mail do remetente verificado no Mailjet.</summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>Nome do remetente exibido no cliente de e-mail.</summary>
    public string FromName { get; set; } = "Ambev Dev Evaluation - Estoque";

    /// <summary>E-mail do administrador que receberá os alertas.</summary>
    public string AdminEmail { get; set; } = string.Empty;

    /// <summary>Intervalo em minutos entre cada verificação de estoque. Padrão: 60 minutos.</summary>
    public int CheckIntervalMinutes { get; set; } = 60;
}
