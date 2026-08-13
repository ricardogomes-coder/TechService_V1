using MySqlConnector;
using TechService.Api.Data;
using TechService.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Serviços usados pelo Swagger/OpenAPI.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Uma única factory reutilizada para criar ligações ao MySQL.
builder.Services.AddSingleton<MySqlConnectionFactory>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Endpoint mantido da Versão 0.
app.MapGet("/", () => Results.Ok(new
{
    mensagem = "Olá! Bem-vindo à API TechService - Versão 1",
    versao = "V1",
    estado = "API ligada ao MySQL",
    endpoint_disponivel = "GET /api/clientes"
}))
.WithName("EstadoDaApi")
.WithSummary("Verificar o estado da API")
.Produces(StatusCodes.Status200OK);

// Versão 1: listar clientes ativos da tabela clientes.
app.MapGet("/api/clientes", async (MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT
            id_cliente,
            nome,
            email,
            telefone,
            status,
            created_at,
            updated_at,
            deleted_at
        FROM clientes
        WHERE status = 1
        ORDER BY nome;
        """;

    var clientes = new List<Cliente>();

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();

    var ordinalIdCliente = reader.GetOrdinal("id_cliente");
    var ordinalNome = reader.GetOrdinal("nome");
    var ordinalEmail = reader.GetOrdinal("email");
    var ordinalTelefone = reader.GetOrdinal("telefone");
    var ordinalStatus = reader.GetOrdinal("status");
    var ordinalCreatedAt = reader.GetOrdinal("created_at");
    var ordinalUpdatedAt = reader.GetOrdinal("updated_at");
    var ordinalDeletedAt = reader.GetOrdinal("deleted_at");

    while (await reader.ReadAsync())
    {
        clientes.Add(new Cliente
        {
            IdCliente = reader.GetInt32(ordinalIdCliente),
            Nome = reader.GetString(ordinalNome),
            Email = reader.GetString(ordinalEmail),
            Telefone = reader.IsDBNull(ordinalTelefone)
                ? null
                : reader.GetString(ordinalTelefone),
            Status = reader.GetInt32(ordinalStatus),
            CreatedAt = reader.GetDateTime(ordinalCreatedAt),
            UpdatedAt = reader.IsDBNull(ordinalUpdatedAt)
                ? null
                : reader.GetDateTime(ordinalUpdatedAt),
            DeletedAt = reader.IsDBNull(ordinalDeletedAt)
                ? null
                : reader.GetDateTime(ordinalDeletedAt)
        });
    }

    return Results.Ok(clientes);
})
.WithName("ListarClientes")
.WithSummary("Listar clientes ativos")
.WithDescription("Devolve os clientes da tabela clientes cujo status é igual a 1.")
.Produces<List<Cliente>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status500InternalServerError);


app.MapGet("/api/clientes/{id_cliente:int}", async (int id_cliente, MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT
            id_cliente,
            nome,
            email,
            telefone,
            status,
            created_at,
            updated_at,
            deleted_at
        FROM clientes
        WHERE id_cliente = @id_cliente
        AND status = 1;
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id_cliente", id_cliente);

    await using var reader = await command.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
    {
        return Results.NotFound(new
        {
            mensagem = "Cliente não encontrado."
        });
    }

    var cliente = new Cliente
    {
        IdCliente = reader.GetInt32(reader.GetOrdinal("id_cliente")),
        Nome = reader.GetString(reader.GetOrdinal("nome")),
        Email = reader.GetString(reader.GetOrdinal("email")),
        Telefone = reader.IsDBNull(reader.GetOrdinal("telefone"))
            ? null
            : reader.GetString(reader.GetOrdinal("telefone")),
        Status = reader.GetInt32(reader.GetOrdinal("status")),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
        UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at"))
            ? null
            : reader.GetDateTime(reader.GetOrdinal("updated_at")),
        DeletedAt = reader.IsDBNull(reader.GetOrdinal("deleted_at"))
            ? null
            : reader.GetDateTime(reader.GetOrdinal("deleted_at"))
    };

    return Results.Ok(cliente);
})
.WithName("BuscarClientePorId")
.WithSummary("Buscar cliente por ID")
.WithDescription("Devolve um cliente ativo da tabela clientes pelo seu id_cliente.")
.Produces<Cliente>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status500InternalServerError);


// Este endpoint permite alterar os dados de um cliente. 
// PUT /api/clientes/5 
// O ID do cliente é recebido através da URL. 
// Os restantes dados são recebidos no corpo da requisição 
// em formato JSON.
app.MapPut("/api/clientes/{id}", async (int id, Cliente cliente, MySqlConnectionFactory factory) =>
{
    // SQL utilizado para atualizar o cliente.
    // Apenas nome, telefone e email são alterados.
const string sql = """
UPDATE clientes
SET
nome = @nome,
telefone = @telefone,
email = @email
WHERE id_cliente = @id;
""";

// Cria a ligação ao MySQL.
await using var connection = factory.CreateConnection();
// Abre a ligação.
await connection.OpenAsync();

// Cria o comando SQL.
await using var command = new MySqlCommand(sql, connection);

// Adiciona o ID recebido na URL.
command.Parameters.AddWithValue("@id", id);
// Adiciona o novo nome.
command.Parameters.AddWithValue("@nome", cliente.Nome);

// Adiciona o telefone.
// Se o telefone for null no objeto Cliente, 
// enviamos DBNull.Value para o MySQL.
command.Parameters.AddWithValue("@telefone", (object?)cliente.Telefone ?? DBNull.Value);
command.Parameters.AddWithValue("@email", cliente.Email);

var rows = await command.ExecuteNonQueryAsync();

if (rows == 0)
{
    return Results.NotFound(new
    {
        mensagem = "Cliente não encontrado."
    });
}

return Results.Ok(new
{
    mensagem = "Cliente atualizado com sucesso!"
});

})
.WithName("AtualizarCliente")
.WithSummary("Atualizar cliente")
.WithDescription("Atualiza o nome, telefone e email de um cliente pelo seu id_cliente.")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status500InternalServerError);

// Este endpoint NÃO elimina fisicamente o cliente da base de dados. 
// o registo continua guardado na base de dados,  mas deixa de ser considerado um cliente ativo.
 
// // DELETE /api/clientes/5 
app.MapDelete("/api/clientes/{id:int}", async (int id, MySqlConnectionFactory factory) => 
{   
    // Atualiza o cliente em vez de o eliminar. 
    // // status = 0 -> cliente fica inativo // 
    // deleted_at = NOW() -> guarda a data/hora da desativação 
    const string sql = """ 
        UPDATE clientes 
        SET status = 0,
        deleted_at = NOW() WHERE id_cliente = @id 
        AND status = 1; 
        """; 
        
        // Cria a ligação à base de dados.
        await using var connection = factory.CreateConnection();

        // Abre a ligação ao MySQL. 
        await connection.OpenAsync(); 
        
        // Cria o comando SQL. 
        await using var command = new MySqlCommand(sql, connection); 
        
        // Adiciona o ID recebido através da URL. 
        command.Parameters.AddWithValue("@id", id); 

        // Executa o UPDATE. O valor de rows indica quantos registos foram alterados. 
        var rows = await command.ExecuteNonQueryAsync();

        // Se nenhum registo foi alterado, significa que: // - o cliente não existe; OU // - o cliente já estava inativo. 
        if (rows == 0) 
        { 
            return Results.NotFound(new { mensagem = "Cliente não encontrado ou já está inativo." }); 
            
        } 
        // O cliente foi desativado com sucesso. 
        // O registo continua na tabela clientes. 
        return Results.Ok(new 
        { 
            mensagem = "Cliente desativado com sucesso!", 
            id_cliente = id, 
            status = 0 
            }); 
}) 

.WithName("EliminarCliente")
.WithSummary("Desativar cliente") 
.WithDescription( "Desativa um cliente sem o eliminar fisicamente da base de dados. " 
                + "O status passa para 0 e a data de eliminação é registada em deleted_at." ) 
.Produces(StatusCodes.Status200OK) 
.Produces(StatusCodes.Status404NotFound) 
.Produces(StatusCodes.Status500InternalServerError);

// Este endpoint permite criar um novo cliente. 
// // POST /api/clientes 
//  Os dados do cliente são recebidos no corpo da requisição 
// // em formato JSON. 
app.MapPost("/api/clientes", async (Cliente cliente, MySqlConnectionFactory factory) =>
{ 
    const string sql = """ 
    INSERT INTO clientes ( nome, email, telefone, status, created_at ) 
    VALUES ( @nome, @email, @telefone, 1, NOW() ); 
    """;
    
    // Cria a ligação à base de dados.
    await using var connection = factory.CreateConnection(); 
    
    // Abre a ligação ao MySQL. 
    await connection.OpenAsync(); 

    // Cria o comando SQL. 
    await using var command = new MySqlCommand(sql, connection); 
    
    // Adiciona os dados recebidos. 
    command.Parameters.AddWithValue("@nome", cliente.Nome); 
    command.Parameters.AddWithValue("@email", cliente.Email); 
    command.Parameters.AddWithValue( "@telefone", (object?)cliente.Telefone ?? DBNull.Value ); 
    
    // Executa o INSERT. 
    await command.ExecuteNonQueryAsync(); 
    
    // Obtém o ID do cliente criado. 
    var idCliente = (int)command.LastInsertedId; 
    
    return Results.Created( $"/api/clientes/{idCliente}", 
        new 
        { 
            mensagem = "Cliente criado com sucesso!", 
            id_cliente = idCliente, 
            nome = cliente.Nome, 
            email = cliente.Email, 
            telefone = cliente.Telefone, 
            status = 1 
            } 
    );
    
}) 

.WithName("CriarCliente") 
.WithSummary("Criar cliente") 
.WithDescription("Cria um novo cliente na tabela clientes.") 
.Produces(StatusCodes.Status201Created) 
.Produces(StatusCodes.Status400BadRequest) 
.Produces(StatusCodes.Status500InternalServerError);

// ============================================================================
// EQUIPAMENTOS
// ============================================================================

// GET /api/equipamentos
// Lista todos os equipamentos ativos.
app.MapGet("/api/equipamentos", async (MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT
            id_equipamento,
            id_cliente,
            tipo,
            marca,
            modelo,
            numero_serie,
            observacoes,
            status,
            created_at,
            updated_at,
            deleted_at
        FROM equipamentos
        WHERE status = 1
        ORDER BY id_equipamento;
        """;

    var equipamentos = new List<Equipamento>();

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();

    var ordinalIdEquipamento = reader.GetOrdinal("id_equipamento");
    var ordinalIdCliente = reader.GetOrdinal("id_cliente");
    var ordinalTipo = reader.GetOrdinal("tipo");
    var ordinalMarca = reader.GetOrdinal("marca");
    var ordinalModelo = reader.GetOrdinal("modelo");
    var ordinalNumeroSerie = reader.GetOrdinal("numero_serie");
    var ordinalObservacoes = reader.GetOrdinal("observacoes");
    var ordinalStatus = reader.GetOrdinal("status");
    var ordinalCreatedAt = reader.GetOrdinal("created_at");
    var ordinalUpdatedAt = reader.GetOrdinal("updated_at");
    var ordinalDeletedAt = reader.GetOrdinal("deleted_at");

    while (await reader.ReadAsync())
    {
        equipamentos.Add(new Equipamento
        {
            IdEquipamento = reader.GetInt32(ordinalIdEquipamento),
            IdCliente = reader.GetInt32(ordinalIdCliente),
            Tipo = reader.GetString(ordinalTipo),
            Marca = reader.GetString(ordinalMarca),
            Modelo = reader.GetString(ordinalModelo),
            NumeroSerie = reader.GetString(ordinalNumeroSerie),

            Observacoes = reader.IsDBNull(ordinalObservacoes)
                ? null
                : reader.GetString(ordinalObservacoes),

            Status = reader.GetInt32(ordinalStatus),

            CreatedAt = reader.GetDateTime(ordinalCreatedAt),

            UpdatedAt = reader.IsDBNull(ordinalUpdatedAt)
                ? null
                : reader.GetDateTime(ordinalUpdatedAt),

            DeletedAt = reader.IsDBNull(ordinalDeletedAt)
                ? null
                : reader.GetDateTime(ordinalDeletedAt)
        });
    }

    return Results.Ok(equipamentos);
})
.WithName("ListarEquipamentos")
.WithSummary("Listar equipamentos ativos")
.WithDescription(
    "Devolve os equipamentos da tabela equipamentos cujo status é igual a 1."
)
.Produces<List<Equipamento>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status500InternalServerError);


// ============================================================================
// GET EQUIPAMENTO POR ID
// ============================================================================

// GET /api/equipamentos/5
app.MapGet(
    "/api/equipamentos/{id_equipamento:int}",
    async (int id_equipamento, MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT
            id_equipamento,
            id_cliente,
            tipo,
            marca,
            modelo,
            numero_serie,
            observacoes,
            status,
            created_at,
            updated_at,
            deleted_at
        FROM equipamentos
        WHERE id_equipamento = @id_equipamento
        AND status = 1;
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);

    command.Parameters.AddWithValue(
        "@id_equipamento",
        id_equipamento
    );

    await using var reader = await command.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
    {
        return Results.NotFound(new
        {
            mensagem = "Equipamento não encontrado."
        });
    }

    var equipamento = new Equipamento
    {
        IdEquipamento = reader.GetInt32(
            reader.GetOrdinal("id_equipamento")
        ),

        IdCliente = reader.GetInt32(
            reader.GetOrdinal("id_cliente")
        ),

        Tipo = reader.GetString(
            reader.GetOrdinal("tipo")
        ),

        Marca = reader.GetString(
            reader.GetOrdinal("marca")
        ),

        Modelo = reader.GetString(
            reader.GetOrdinal("modelo")
        ),

        NumeroSerie = reader.GetString(
            reader.GetOrdinal("numero_serie")
        ),

        Observacoes = reader.IsDBNull(
            reader.GetOrdinal("observacoes")
        )
            ? null
            : reader.GetString(
                reader.GetOrdinal("observacoes")
            ),

        Status = reader.GetInt32(
            reader.GetOrdinal("status")
        ),

        CreatedAt = reader.GetDateTime(
            reader.GetOrdinal("created_at")
        ),

        UpdatedAt = reader.IsDBNull(
            reader.GetOrdinal("updated_at")
        )
            ? null
            : reader.GetDateTime(
                reader.GetOrdinal("updated_at")
            ),

        DeletedAt = reader.IsDBNull(
            reader.GetOrdinal("deleted_at")
        )
            ? null
            : reader.GetDateTime(
                reader.GetOrdinal("deleted_at")
            )
    };

    return Results.Ok(equipamento);
})
.WithName("BuscarEquipamentoPorId")
.WithSummary("Buscar equipamento por ID")
.WithDescription(
    "Devolve um equipamento ativo através do seu id_equipamento."
)
.Produces<Equipamento>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status500InternalServerError);


// ============================================================================
// PUT - ATUALIZAR EQUIPAMENTO
// ============================================================================

// PUT /api/equipamentos/5
app.MapPut(
    "/api/equipamentos/{id}",
    async (
        int id,
        Equipamento equipamento,
        MySqlConnectionFactory factory) =>
{
    const string sql = """
        UPDATE equipamentos
        SET
            id_cliente = @id_cliente,
            tipo = @tipo,
            marca = @marca,
            modelo = @modelo,
            numero_serie = @numero_serie,
            observacoes = @observacoes,
            updated_at = NOW()
        WHERE id_equipamento = @id
        AND status = 1;
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);

    command.Parameters.AddWithValue(
        "@id",
        id
    );

    command.Parameters.AddWithValue(
        "@id_cliente",
        equipamento.IdCliente
    );

    command.Parameters.AddWithValue(
        "@tipo",
        equipamento.Tipo
    );

    command.Parameters.AddWithValue(
        "@marca",
        equipamento.Marca
    );

    command.Parameters.AddWithValue(
        "@modelo",
        equipamento.Modelo
    );

    command.Parameters.AddWithValue(
        "@numero_serie",
        equipamento.NumeroSerie
    );

    command.Parameters.AddWithValue(
        "@observacoes",
        (object?)equipamento.Observacoes ?? DBNull.Value
    );

    var rows = await command.ExecuteNonQueryAsync();

    if (rows == 0)
    {
        return Results.NotFound(new
        {
            mensagem = "Equipamento não encontrado ou está inativo."
        });
    }

    return Results.Ok(new
    {
        mensagem = "Equipamento atualizado com sucesso!",
        id_equipamento = id
    });
})
.WithName("AtualizarEquipamento")
.WithSummary("Atualizar equipamento")
.WithDescription(
    "Atualiza os dados de um equipamento pelo seu id_equipamento."
)
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status500InternalServerError);


// ============================================================================
// DELETE - DESATIVAR EQUIPAMENTO
// ============================================================================

// DELETE /api/equipamentos/5
app.MapDelete(
    "/api/equipamentos/{id:int}",
    async (int id, MySqlConnectionFactory factory) =>
{
    const string sql = """
        UPDATE equipamentos
        SET
            status = 0,
            deleted_at = NOW()
        WHERE id_equipamento = @id
        AND status = 1;
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);

    command.Parameters.AddWithValue(
        "@id",
        id
    );

    var rows = await command.ExecuteNonQueryAsync();

    if (rows == 0)
    {
        return Results.NotFound(new
        {
            mensagem = "Equipamento não encontrado ou já está inativo."
        });
    }

    return Results.Ok(new
    {
        mensagem = "Equipamento desativado com sucesso!",
        id_equipamento = id,
        status = 0
    });
})
.WithName("EliminarEquipamento")
.WithSummary("Desativar equipamento")
.WithDescription(
    "Desativa um equipamento sem o eliminar fisicamente da base de dados. " +
    "O status passa para 0 e a data de eliminação é registada em deleted_at."
)
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status500InternalServerError);


// ============================================================================
// POST - CRIAR EQUIPAMENTO
// ============================================================================

// POST /api/equipamentos
app.MapPost(
    "/api/equipamentos",
    async (
        Equipamento equipamento,
        MySqlConnectionFactory factory) =>
{
    const string sql = """
        INSERT INTO equipamentos
        (
            id_cliente,
            tipo,
            marca,
            modelo,
            numero_serie,
            observacoes,
            status,
            created_at
        )
        VALUES
        (
            @id_cliente,
            @tipo,
            @marca,
            @modelo,
            @numero_serie,
            @observacoes,
            1,
            NOW()
        );
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();

    await using var command = new MySqlCommand(sql, connection);

    command.Parameters.AddWithValue(
        "@id_cliente",
        equipamento.IdCliente
    );

    command.Parameters.AddWithValue(
        "@tipo",
        equipamento.Tipo
    );

    command.Parameters.AddWithValue(
        "@marca",
        equipamento.Marca
    );

    command.Parameters.AddWithValue(
        "@modelo",
        equipamento.Modelo
    );

    command.Parameters.AddWithValue(
        "@numero_serie",
        equipamento.NumeroSerie
    );

    command.Parameters.AddWithValue(
        "@observacoes",
        (object?)equipamento.Observacoes ?? DBNull.Value
    );

    await command.ExecuteNonQueryAsync();

    var idEquipamento = (int)command.LastInsertedId;

    return Results.Created(
        $"/api/equipamentos/{idEquipamento}",
        new
        {
            mensagem = "Equipamento criado com sucesso!",
            id_equipamento = idEquipamento,
            id_cliente = equipamento.IdCliente,
            tipo = equipamento.Tipo,
            marca = equipamento.Marca,
            modelo = equipamento.Modelo,
            numero_serie = equipamento.NumeroSerie,
            observacoes = equipamento.Observacoes,
            status = 1
        }
    );
})
.WithName("CriarEquipamento")
.WithSummary("Criar equipamento")
.WithDescription(
    "Cria um novo equipamento na tabela equipamentos."
)
.Produces(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status500InternalServerError);

app.Run();
