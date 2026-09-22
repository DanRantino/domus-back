using Domus.Domain.Tasks;

namespace Domus.Infrastructure.DevelopmentSeed;

public sealed record SeedHouseTask(
    string HouseName,
    string Title,
    string? Description,
    string Status,
    string CreatedByEmail,
    string? AssigneeEmail,
    TimeSpan? DueFromNow,
    TimeSpan? CompletedFromNow);

public static class SeedHouseTasks
{
    public static IReadOnlyList<SeedHouseTask> GetTasks() =>
    [
        new(
            "Casa da Família",
            "Comprar ração",
            "Ração do cachorro no mercado da esquina",
            HouseTaskStatuses.Pending,
            "dev1@domus.local",
            "dev2@domus.local",
            TimeSpan.Zero,
            null),
        new(
            "Casa da Família",
            "Tirar o lixo",
            "Separar reciclável e orgânico",
            HouseTaskStatuses.Pending,
            "dev1@domus.local",
            "dev3@domus.local",
            TimeSpan.Zero,
            null),
        new(
            "Casa da Família",
            "Limpar a cozinha",
            "Pia, fogão e bancada",
            HouseTaskStatuses.Pending,
            "dev1@domus.local",
            null,
            TimeSpan.FromDays(1),
            null),
        new(
            "Casa da Família",
            "Pagar internet",
            "Fatura do mês no app do banco",
            HouseTaskStatuses.Pending,
            "dev1@domus.local",
            "dev1@domus.local",
            TimeSpan.FromDays(3),
            null),
        new(
            "Casa da Família",
            "Comprar produtos de limpeza",
            "Detergente, desinfetante e pano de chão",
            HouseTaskStatuses.Pending,
            "dev1@domus.local",
            "dev4@domus.local",
            null,
            null),
        new(
            "Casa da Família",
            "Trocar roupa de cama",
            "Lençóis do quarto principal",
            HouseTaskStatuses.Completed,
            "dev1@domus.local",
            "dev2@domus.local",
            null,
            TimeSpan.FromHours(-2)),
        new(
            "Casa do Admin",
            "Revisar contas",
            "Conferir vencimentos da semana",
            HouseTaskStatuses.Pending,
            "dev1@domus.local",
            "dev1@domus.local",
            TimeSpan.Zero,
            null),
        new(
            "Casa do Admin",
            "Organizar documentos",
            "Pastas de contratos e garantias",
            HouseTaskStatuses.Completed,
            "dev1@domus.local",
            "dev1@domus.local",
            null,
            TimeSpan.FromHours(-4)),
    ];
}
