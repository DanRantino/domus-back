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
    private const string Pending = HouseTaskStatuses.Pending;
    private const string Completed = HouseTaskStatuses.Completed;

    public static IReadOnlyList<SeedHouseTask> GetTasks() =>
    [
        Family("Pagar a internet", "Fatura do mês no app do banco", Pending, "dev1@domus.local", TimeSpan.Zero, null),
        Family("Revisar as contas da casa", "Conferir vencimentos da semana", Pending, "dev1@domus.local", TimeSpan.FromDays(3), null),
        Family("Agendar a manutenção do ar", "Chamar o técnico do quarto principal", Pending, "dev1@domus.local", TimeSpan.FromDays(-1), null),
        Family("Comprar lâmpadas", "Duas LED para o corredor", Pending, "dev1@domus.local", null, null),
        Family("Guardar os contratos", "Pastas de garantia e seguro", Completed, "dev1@domus.local", null, TimeSpan.FromHours(-2)),

        Family("Comprar ração", "Ração do cachorro no mercado da esquina", Pending, "dev2@domus.local", TimeSpan.Zero, null),
        Family("Passear com o cachorro", "Volta no quarteirão depois do almoço", Pending, "dev2@domus.local", TimeSpan.FromDays(1), null),
        Family("Lavar a roupa", "Roupa clara da semana", Pending, "dev2@domus.local", TimeSpan.FromDays(-1), null),
        Family("Trocar o filtro da cafeteira", "Filtro novo está no armário", Pending, "dev2@domus.local", null, null),
        Family("Trocar a roupa de cama", "Lençóis do quarto principal", Completed, "dev2@domus.local", null, TimeSpan.FromHours(-3)),

        Family("Tirar o lixo", "Separar reciclável e orgânico", Pending, "dev3@domus.local", TimeSpan.Zero, null),
        Family("Regar as plantas", "Vasinhos da varanda", Pending, "dev3@domus.local", TimeSpan.FromDays(2), null),
        Family("Limpar o banheiro", "Pia, box e chão", Pending, "dev3@domus.local", TimeSpan.FromDays(-1), null),
        Family("Separar os recicláveis", "Papel e plástico da semana", Pending, "dev3@domus.local", null, null),
        Family("Lavar o carro", "Por fora, na garagem", Completed, "dev3@domus.local", null, TimeSpan.FromHours(-4)),

        Family("Comprar produtos de limpeza", "Detergente, desinfetante e pano de chão", Pending, "dev4@domus.local", TimeSpan.Zero, null),
        Family("Limpar a cozinha", "Pia, fogão e bancada", Pending, "dev4@domus.local", TimeSpan.FromDays(1), null),
        Family("Aspirar a sala", "Sofá, tapete e cantos", Pending, "dev4@domus.local", TimeSpan.FromDays(-1), null),
        Family("Guardar as compras", "Despensa e geladeira", Pending, "dev4@domus.local", null, null),
        Family("Lavar as louças", "Louça do jantar", Completed, "dev4@domus.local", null, TimeSpan.FromHours(-5)),

        Admin("Revisar as contas", "Conferir vencimentos da semana", Pending, "dev1@domus.local", TimeSpan.Zero, null),
        Admin("Pagar o condomínio", "Boleto do mês no app do banco", Pending, "dev1@domus.local", TimeSpan.FromDays(5), null),
        Admin("Trocar a senha do wifi", "Anotar a senha nova na geladeira", Pending, "dev1@domus.local", TimeSpan.FromDays(-1), null),
        Admin("Arquivar notas fiscais", "Notas de eletrodomésticos", Pending, "dev1@domus.local", null, null),
        Admin("Organizar os documentos", "Pastas de contratos e garantias", Completed, "dev1@domus.local", null, TimeSpan.FromHours(-4)),
    ];

    private static SeedHouseTask Family(
        string title,
        string description,
        string status,
        string email,
        TimeSpan? dueFromNow,
        TimeSpan? completedFromNow) =>
        new("Casa da Família", title, description, status, email, email, dueFromNow, completedFromNow);

    private static SeedHouseTask Admin(
        string title,
        string description,
        string status,
        string email,
        TimeSpan? dueFromNow,
        TimeSpan? completedFromNow) =>
        new("Casa do Admin", title, description, status, email, email, dueFromNow, completedFromNow);
}
